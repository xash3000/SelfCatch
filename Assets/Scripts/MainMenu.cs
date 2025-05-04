using System;
using TMPro;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;

    private void Update()
    {
        timerText.text = GameManager.Instance.FormattedTime;
        if (GameManager.Instance.timerRunning)
        {
            timerText.color = Color.black;
        }
        else
        {
            timerText.color = Color.red;
        }
    }
}
