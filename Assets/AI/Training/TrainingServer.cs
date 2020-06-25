using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(TrainingManager))]
[RequireComponent(typeof(TrainingVisualiser))]
public class TrainingServer : MonoBehaviour
{
    private TrainingManager manager;
    private TrainingVisualiser visualiser;

    private void Awake()
    {
        manager = GetComponent<TrainingManager>();
        visualiser = GetComponent<TrainingVisualiser>();
        instances = new List<TrainingManagerEndpoint>();
        available = new List<TrainingManagerEndpoint>();
    }

    private void Start()
    {
        StartCoroutine(RemoteWorkerCoroutine());
        StartCoroutine(RemoteVisualisationWorkerCoroutine());
        runServer = true;
        Task.Run(StartServer);
    }

    public string RemoteInstances
    {
        get
        {
            lock (instances)
            {
                return instances.Count.ToString();
            }
        }
    }

    private List<TrainingManagerEndpoint> instances;
    private List<TrainingManagerEndpoint> available;

    private bool runServer;

    public async void StartServer()
    {
        TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8000);
        listener.Start(100);
        while (runServer)
        {
            var client = await listener.AcceptTcpClientAsync();
            var instance = new TrainingManagerEndpoint(client);
            instance.OnMessage += OnMessage;
            lock (instances)
            {
                instances.Add(instance);
            }
        }
        listener.Stop();
    }

    private void OnMessage(TrainingManagerEndpoint instance, TrainingMessage message)
    {
        if (message.type == "complete")
        {
            if(message.payload.Length > 0)
            {
                var report = JsonUtility.FromJson<TrainingReport>(message.payload);
                manager.SaveTrainingData(report.filename, report.json);
            }

            lock (available)
            {
                available.Add(instance);
            }
        }

        if (message.type == "state")
        {
            lock (visualiser.state)
            {
                var state = JsonUtility.FromJson<TrainingState>(message.payload);

                visualiser.state.scenekey = state.scenekey;
                visualiser.state.carkey = state.carkey;
                visualiser.state.agents = state.agents;

                if (visualiser.state.scenekey == "") visualiser.state.scenekey = null;
                if (visualiser.state.carkey == "") visualiser.state.carkey = null;
            }
        }
    }

    private IEnumerator RemoteWorkerCoroutine()
    {
        while (true)
        {
            TrainingRequest request;
            while (!manager.requests.TryDequeue(out request))
            {
                yield return null;
            }

            while (true)
            {
                TrainingManagerEndpoint instance;

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
                    lock (instances)
                    {
                        instances.Remove(instance); // client gone away
                    }
                }
            }
        }
    }

    private IEnumerator RemoteVisualisationWorkerCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            if (instances.Count > 0)
            {
                var instance = instances.First();
                try
                {
                    instance.RequestStateUpdate();
                }
                catch (IOException)
                {
                    lock (instances)
                    {
                        instances.Remove(instance);
                    }
                }
            }
        }
    }

    private void OnApplicationQuit()
    {
        runServer = false;
    }
}

