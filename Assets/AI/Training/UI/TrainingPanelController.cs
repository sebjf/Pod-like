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
    public Text InstancsText;
    public Button TrainProfileHereButton;
    public GameObject ToggleButtonPrefab;
    public TrainingManager TrainingManager;

    private List<string> circuits;
    private List<string> cars;

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

        Refresh(false);

        TrainProfileHereButton.onClick.AddListener(() => TrainingManager.AddTrainingInstances(circuits, cars));
    }

    private void Refresh(bool ignored)
    {
        circuits = CircuitsPanel.GetComponentsInChildren<TrainingEntityListItem>().Where(x => x.selected).Select(x => x.item).ToList();
        cars = CarsPanel.GetComponentsInChildren<TrainingEntityListItem>().Where(x => x.selected).Select(x => x.item).ToList();
        var instances = circuits.Count * cars.Count;
        SummaryText.text = string.Format("Selected {0}", instances);
    }


    private void Update()
    {
        ProgressText.text = string.Format("Remaining {0}", TrainingManager.Remaining);
        InstancsText.text = string.Format("Instances {0}", TrainingManager.RemoteInstances);
    }

}
