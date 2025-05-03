using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private TextMeshProUGUI winText;
    private void Start()
    {
        GameManager.Instance.gameWon += () =>
        {
            winPanel.SetActive(true);
            winText.text = "You Win\n\nTime: " + GameManager.Instance.FormattedTime;
        };
        GameManager.Instance.gameLost += () => losePanel.SetActive(true);
    }

    public void StartGame()
    {
        GameManager.Instance.StartTimer();
        infoPanel.SetActive(false);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
