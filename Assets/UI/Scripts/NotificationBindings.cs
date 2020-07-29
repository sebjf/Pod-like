using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class NotificationBindings : MonoBehaviour
{
    public RaceManager manager;

    public Text messages;

    // Start is called before the first frame update
    void Start()
    {
        messages.text = "";

        manager.OnPenalty.AddListener((competitor) =>
        {
            StartCoroutine(PostPenalty(competitor));
        });
    }

    public IEnumerator PostPenalty(Competitor competitor)
    {
        if (manager.player == competitor)
        {
            messages.text = "Penalty - " + competitor.penalties.Last().ToString() + "!";
            yield return new WaitForSeconds(4f);
        }
        messages.text = "";
    }
}
