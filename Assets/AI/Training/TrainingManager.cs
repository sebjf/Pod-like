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
using Unity.Collections.LowLevel.Unsafe;
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

[Serializable]
public class TrainingReport
{
    public string filename;
    public string json;
}

[Serializable]
public class TrainingRequest
{
    public string circuit;
    public string car;
}

[Serializable]
public class TrainingMessage
{
    public string type;
    public string payload;
}

public class TrainingManager : MonoBehaviour
{
    private Dictionary<string, string> circuits;
    private Dictionary<string, GameObject> cars;

    public IEnumerable<string> Circuits => circuits.Keys;
    public IEnumerable<string> Cars => cars.Keys;

    public enum Mode
    {
        Local,
        Server,
        Client
    }

    public Mode mode;

    [HideInInspector]
    [NonSerialized]
    public ConcurrentQueue<TrainingRequest> requests;
    
    public string directory = @"Support\TrainingData";
    
    public int Remaining {
        get
        {
            return requests.Count;
        }
    }

    private void Awake()
    {
        circuits = new Dictionary<string, string>();
        cars = new Dictionary<string, GameObject>();
        requests = new ConcurrentQueue<TrainingRequest>();

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            circuits.Add(Path.GetFileNameWithoutExtension(path), path);
        }

        foreach (var item in GetComponent<Catalogue>().aiCars)
        {
            cars.Add(item.name, item);
        }
    }

    public void AddTrainingRequests(IEnumerable<string> circuits, IEnumerable<string> cars)
    {
        foreach (var circuit in circuits)
        {
            foreach (var car in cars)
            {
                requests.Enqueue(new TrainingRequest() { car = car, circuit = circuit });
            }
        }
    }

    public void SaveTrainingData(string filename, string content)
    {
        var file = Path.Combine(directory, filename);
        using (FileStream stream = new FileStream(filename, FileMode.Create))
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(content);
            }
        }
    }

    public GameObject Car(string key)
    {
        return cars[key];
    }

    public string Circuit(string key)
    {
        return circuits[key];
    }

    private void Start()
    {
        switch (mode)
        {
            case Mode.Local:
                {
                    var worker = gameObject.AddComponent<TrainingWorker>();
                    worker.OnTrainingRequestComplete += OnLocalTrainingRequestComplete;
                }
                break;
            case Mode.Server:
                {
                    var server = gameObject.AddComponent<TrainingServer>();
                }
                break;
            case Mode.Client:
                {
                    var client = gameObject.AddComponent<TrainingClient>();
                }
                break;
        }
    }

    private void OnLocalTrainingRequestComplete(ProfileAgentManager obj)
    {
        SaveTrainingData(obj.filename, obj.ExportJson());
    }
}

public class TrainingManagerEndpoint
{
    private NetworkStream stream;

    public TrainingManagerEndpoint(TcpClient client)
    {
        stream = client.GetStream();
        Task.Run(ReadWorker);
    }

    public event Action<TrainingManagerEndpoint, TrainingMessage> OnMessage;

    public void Close()
    {
        stream.Close();
    }

    public async void ReadWorker()
    {
        byte[] header = new byte[4];
        while (true)
        {
            await stream.ReadAsync(header, 0, 4);
            var length = BitConverter.ToInt32(header, 0);
            var buffer = new byte[length];
            await stream.ReadAsync(buffer, 0, length);

            var message = JsonUtility.FromJson<TrainingMessage>(Encoding.UTF8.GetString(buffer));
            OnMessage?.Invoke(this, message);
        }
    }

    public void SendMessage(TrainingMessage message)
    {
        var data = Encoding.UTF8.GetBytes(JsonUtility.ToJson(message));
        var header = BitConverter.GetBytes(data.Length);
        stream.Write(header, 0, 4);
        stream.Write(data, 0, data.Length);
    }

    public void SendRequest(TrainingRequest request)
    {
        TrainingMessage message = new TrainingMessage();
        message.type = "trainingrequest";
        message.payload = JsonUtility.ToJson(request);
        SendMessage(message);
    }

    public void RequestStateUpdate()
    {
        TrainingMessage message = new TrainingMessage();
        message.type = "staterequest";
        message.payload = "";
        SendMessage(message);
    }

    public void SendStateUpdate(TrainingState state)
    {
        TrainingMessage message = new TrainingMessage();
        message.type = "state";
        message.payload = JsonUtility.ToJson(state);
        SendMessage(message);
    }

    public void PostComplete()
    {
        TrainingMessage message = new TrainingMessage();
        message.type = "complete";
        message.payload = "";
        SendMessage(message);
    }

    public void PostComplete(string filename, string json)
    {
        TrainingMessage message = new TrainingMessage();
        message.type = "complete";
        TrainingReport report = new TrainingReport();
        report.filename = filename;
        report.json = json;
        message.payload = JsonUtility.ToJson(report);
        SendMessage(message);
    }
}