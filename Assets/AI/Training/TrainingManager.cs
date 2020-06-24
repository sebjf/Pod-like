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

public class TrainingManager : MonoBehaviour
{
    private Dictionary<string, string> circuits;
    private Dictionary<string, GameObject> cars;

    public string directory = @"Support\TrainingData";
    private List<float> interpolationCoefficients;

    public bool isRemoteClient;

    public IEnumerable<string> Circuits => circuits.Keys;
    public IEnumerable<string> Cars => cars.Keys;

    private ConcurrentQueue<TrainingRequest> trainingRequests;

    private List<TrainingInstance> instances;

    public event Action OnTrainingRequestComplete;
    public event Action OnStateUpdate;

    public GameObject GetCarPrefab(string key)
    {
        return cars[key];
    }
    
    public int Remaining
    {
        get
        {
            return trainingRequests.Count;
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
            new TrainingInstance(new TcpClient("127.0.0.1", 8000), this);
        }
        else
        {
            StartCoroutine(TrainingWorkerCoroutine());
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

            var manager = scene.GetRootGameObjects().Select(x => x.GetComponentInChildren<ProfileAgentManager>()).Where(x => x != null).FirstOrDefault();
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
    }


    public async void StartListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8000);
        listener.Start(100);
        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            var instance = new TrainingInstance(client, this);
            lock(instances)
            {
                instances.Add(instance);
            }
        }
    }

    public class TrainingInstance
    {
        private NetworkStream stream;
        private TrainingManager manager;

        public TrainingInstance(TcpClient client, TrainingManager manager)
        {
            this.manager = manager;
            manager.OnTrainingRequestComplete += PostComplete;
            manager.OnStateUpdate += SendStateUpdate;
            stream = client.GetStream();
            Task.Run(ReadWorker);
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

                // message -> push request
                //manager.trainingRequests.Enqueue()


            }
        }

        public void PostComplete()
        {
            // send complete
        }

        public void SendStateUpdate()
        {

        }
    }

}
