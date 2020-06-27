using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TrainingPanelController : MonoBehaviour
{
    public LayoutGroup CircuitsPanel;
    public LayoutGroup CarsPanel;
    public Text SummaryText;
    public Text ProgressText;
    public Text InstancesText;
    public GameObject InstancesContainer;
    public Button TrainProfileHereButton;
    public GameObject ToggleButtonPrefab;
    public GameObject InstanceButtonPrefab;
    public TrainingManager TrainingManager;

    private List<string> circuits;
    private List<string> cars;
    private List<string> agents;

    private List<GameObject> instancesButtons;

    private void Awake()
    {
        foreach (Transform transform in CircuitsPanel.transform)
        {
            Destroy(transform.gameObject);
        }

        foreach (Transform transform in CarsPanel.transform)
        {
            Destroy(transform.gameObject);
        }

        foreach (Transform transform in InstancesContainer.transform)
        {
            Destroy(transform.gameObject);
        }

        instancesButtons = new List<GameObject>();
    }

    private void Start()
    {
        foreach (var item in TrainingManager.Circuits)
        {
            var widget = GameObject.Instantiate(ToggleButtonPrefab, CircuitsPanel.transform);
            widget.GetComponentInChildren<Text>().text = item;
            widget.GetComponentInChildren<TrainingEntityListItem>().item = item;
            widget.GetComponentInChildren<Toggle>().onValueChanged.AddListener(Refresh);
        }

        foreach (var item in TrainingManager.Cars)
        {
            var widget = GameObject.Instantiate(ToggleButtonPrefab, CarsPanel.transform);
            widget.GetComponentInChildren<Text>().text = item;
            widget.GetComponentInChildren<TrainingEntityListItem>().item = item;
            widget.GetComponentInChildren<Toggle>().onValueChanged.AddListener(Refresh);
        }

        foreach (var item in GetComponentsInChildren<AgentToggle>())
        {
            item.GetComponent<Toggle>().onValueChanged.AddListener(Refresh);
        }

        Refresh(false);

        TrainProfileHereButton.onClick.AddListener(() => TrainingManager.AddTrainingRequests(circuits, cars, agents));
    }

    private void Refresh(bool ignored)
    {
        circuits = CircuitsPanel.GetComponentsInChildren<TrainingEntityListItem>().Where(x => x.selected).Select(x => x.item).ToList();
        cars = CarsPanel.GetComponentsInChildren<TrainingEntityListItem>().Where(x => x.selected).Select(x => x.item).ToList();
        agents = GetComponentsInChildren<AgentToggle>().Where(a => a.Checked).Select(a => a.agent).ToList();
        var instances = circuits.Count * cars.Count;
        SummaryText.text = string.Format("Selected {0}", instances);
    }


    private void Update()
    {
        ProgressText.text = string.Format("Remaining {0}", TrainingManager.Remaining);

        var server = TrainingManager.gameObject.GetComponent<TrainingServer>();
        if(server)
        {
            InstancesText.text = string.Format("Instances {0}", server.RemoteInstances.ToString());

            while(instancesButtons.Count > server.RemoteInstances)
            {
                Destroy(instancesButtons.Last());
                instancesButtons.Remove(instancesButtons.Last());
            }

            while(instancesButtons.Count < server.RemoteInstances)
            {
                var widget = GameObject.Instantiate(InstanceButtonPrefab, InstancesContainer.transform);
                instancesButtons.Add(widget);
                widget.GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    var index = instancesButtons.IndexOf(widget);
                    server.WatchInstance = index;
                });
            }
        }
        else
        {
            InstancesText.text = "";
        }
    }

}
