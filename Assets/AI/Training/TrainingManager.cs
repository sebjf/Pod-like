using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public struct TrainingAgentTransform
{
    public Vector3 position;
    public Quaternion rotation;
}

[Serializable]
public class TrainingState
{
    public string scenekey;
    public string carkey;
    public List<TrainingAgentTransform> agents;

    public TrainingState()
    {
        agents = new List<TrainingAgentTransform>();
    }
}

public class TrainingManager : MonoBehaviour
{
    private Dictionary<string, string> circuits;
    private Dictionary<string, GameObject> cars;

    private ConcurrentQueue<TrainingRequest> trainingRequests;

    private TrainingState state;
    private ProfileAgentManager manager;

    public string directory = @"Support\TrainingData";
    private List<float> interpolationCoefficients;

    public IEnumerable<string> Circuits => circuits.Keys;
    public IEnumerable<string> Cars => cars.Keys;

    // remote instance management

    private List<TrainingInstance> instances;
    private List<TrainingInstance> available;

    public bool isRemoteClient;
    private bool runServer;
    private TrainingInstance client;

    public event Action OnTrainingRequestComplete;

    public GameObject GetCarPrefab(string key)
    {
        return cars[key];
    }
    
    public int Remaining {
        get
        {
            return trainingRequests.Count;
        }
    }

    public string RemoteInstances { 
        get 
        { 
            lock(instances)
            {
                return instances.Count.ToString();
            }
        }
    }

    [Serializable]
    public class TrainingRequest
    {
        public string circuit;
        public string car;
    }

    private void Awake()
    {
        circuits = new Dictionary<string, string>();
        cars = new Dictionary<string, GameObject>();
        trainingRequests = new ConcurrentQueue<TrainingRequest>();
        instances = new List<TrainingInstance>();
        available = new List<TrainingInstance>();
        state = new TrainingState();

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            circuits.Add(Path.GetFileNameWithoutExtension(path), path);
        }

        foreach (var item in GetComponent<Catalogue>().aiCars)
        {
            cars.Add(item.name, item);
        }

        interpolationCoefficients = new List<float>();
        interpolationCoefficients.Add(0.5f);
        interpolationCoefficients.Add(0.15f);

        OnTrainingRequestComplete += TrainingManager_OnTrainingRequestComplete;
    }

    private void TrainingManager_OnTrainingRequestComplete()
    {
        Debug.Log("Training Request Complete");
    }

    private void Start()
    {
        if (isRemoteClient)
        {
            client = new TrainingInstance(new TcpClient("127.0.0.1", 8000), this); // this creates a TrainingInstance pointing to this class
            StartCoroutine(TrainingWorkerCoroutine());
        }
        else
        {
            StartCoroutine(RemoteWorkerCoroutine());
            runServer = true;
            Task.Run(StartServer);
        }
    }

    public void AddTrainingInstances(IEnumerable<string> circuits, IEnumerable<string> cars)
    {
        foreach (var circuit in circuits)
        {
            foreach (var car in cars)
            {
                trainingRequests.Enqueue(new TrainingRequest() { car = car, circuit = circuit });
            }
        }
    }

    public IEnumerator TrainingWorkerCoroutine()
    {
        while (true)
        {
            TrainingRequest request;
            while(!trainingRequests.TryDequeue(out request))
            {
                yield return null;
            }

            // dereference to the actual objects that will be passed around
            var path = circuits[request.circuit];
            var prefab = cars[request.car];

            // load the scene and create the agent trainer
            var asyncoperation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);

            while (!asyncoperation.isDone)
            {
                yield return null;
            }



            var scene = SceneManager.GetSceneByPath(path);
            var root = scene.GetRootGameObjects().Select(x => x.GetComponentInChildren<Track>()).Where(x => x != null).First();

            manager = scene.GetRootGameObjects().Select(x => x.GetComponentInChildren<ProfileAgentManager>()).Where(x => x != null).FirstOrDefault();
            if (manager == null)
            {
                manager = root.gameObject.AddComponent<ProfileAgentManager>();
                // set up manager further here...
            }
            manager.AgentPrefab = prefab;
            manager.directory = directory;

            // set up interpolated paths...
            var geometry = root.GetComponentInChildren<TrackGeometry>();
            var existing = geometry.GetComponentsInChildren<InterpolatedPath>().Select(x => x.coefficient).ToList();
            foreach (var item in interpolationCoefficients)
            {
                if (existing.Contains(item))
                {
                    continue;
                }
                var interpolated = geometry.gameObject.AddComponent<InterpolatedPath>();
                interpolated.coefficient = item;
                interpolated.Initialise();
            }

            var training = true;

            // setup the callbacks
            manager.OnComplete.RemoveAllListeners();
            manager.OnComplete.AddListener((x) =>
            {
                x.Export();
                SceneManager.UnloadSceneAsync(path).completed += (asyncresult) =>
                {
                    training = false;
                };
            });

            // and let it go...

            while (training)
            {
                if(isRemoteClient)
                {
                    lock (state)
                    {
                        state.scenekey = request.circuit;
                        state.carkey = request.car;
                        state.agents.Clear();
                        state.agents.AddRange(manager.Agents.Select(t => new TrainingAgentTransform() { position = t.position, rotation = t.rotation }));
                    }
                }

                yield return null;
            }

            if (OnTrainingRequestComplete != null)
            {
                OnTrainingRequestComplete.Invoke();
            }
        }
    }

    [Serializable]
    public class Message
    {
        public string type;
        public string payload;
    }

    private IEnumerator RemoteWorkerCoroutine()
    {
        while(true)
        {
            TrainingRequest request;
            while (!trainingRequests.TryDequeue(out request))
            {
                yield return null;
            }

            while (true)
            {
                TrainingInstance instance;

                while (true)
                {
                    lock (available)
                    {
                        if (available.Count > 0)
                        {
                            instance = available.First();
                            available.Remove(instance);
                            break;
                        }
                        else
                        {
                            yield return null;
                        }
                    }
                }

                try
                {
                    instance.SendRequest(request);
                    break; // next request
                }
                catch (IOException)
                {
                    lock(instances)
                    {
                        instances.Remove(instance); // client gone away
                    }
                }
            }
        }
    }

    private void OnApplicationQuit()
    {
        runServer = false;
        if(client != null)
        {
            client.Close();
        }
    }


    public async void StartServer()
    {
        TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8000);
        listener.Start(100);
        while (runServer)
        {
            var client = await listener.AcceptTcpClientAsync();
            var instance = new TrainingInstance(client, this);
            lock(instances)
            {
                instances.Add(instance);
            }
        }
        listener.Stop();
    }

    public class TrainingInstance
    {
        private NetworkStream stream;
        private TrainingManager manager;

        public TrainingInstance(TcpClient client, TrainingManager manager)
        {
            this.manager = manager;
            manager.OnTrainingRequestComplete += PostComplete;
            stream = client.GetStream();
            Task.Run(ReadWorker);
            PostComplete();
        }

        public void Close()
        {
            stream.Close();
        }

        public async void ReadWorker()
        {
            byte[] header = new byte[4];
            while(true)
            {
                await stream.ReadAsync(header,0,4);
                var length = BitConverter.ToInt32(header, 0);
                var buffer = new byte[length];
                await stream.ReadAsync(buffer, 0, length);

                var message = JsonUtility.FromJson<Message>(Encoding.UTF8.GetString(buffer));

                if(message.type == "complete")
                {
                    lock(manager.available)
                    {
                        manager.available.Add(this);
                    }
                }

                if(message.type == "trainingrequest")
                {
                    var request = JsonUtility.FromJson<TrainingRequest>(message.payload);
                    manager.trainingRequests.Enqueue(request);
                }

                if(message.type == "staterequest")
                {
                    lock (manager.state)
                    {
                        SendStateUpdate(manager.state);
                    }
                }

                if(message.type == "state")
                {
                    var state = JsonUtility.FromJson<TrainingState>(message.payload);
                    var visualiser = manager.GetComponent<TrainingVisualiser>();
                    visualiser.UpdateTrainingState(state);
                }
            }
        }

        private void SendMessage(Message message)
        {
            var data = Encoding.UTF8.GetBytes(JsonUtility.ToJson(message));
            var header = BitConverter.GetBytes(data.Length);
            stream.Write(header, 0, 4);
            stream.Write(data, 0, data.Length);
        }

        public void SendRequest(TrainingRequest request)
        {
            Message message = new Message();
            message.type = "trainingrequest";
            message.payload = JsonUtility.ToJson(request);
            SendMessage(message);
        }

        public void PostComplete()
        {
            Message message = new Message();
            message.type = "complete";
            message.payload = "";
            SendMessage(message);
        }

        public void RequestStateUpdate()
        {
            Message message = new Message();
            message.type = "staterequest";
            message.payload = "";
            SendMessage(message);
        }

        public void SendStateUpdate(TrainingState state)
        {
            Message message = new Message();
            message.type = "state";
            message.payload = JsonUtility.ToJson(state);
        }
    }

}
