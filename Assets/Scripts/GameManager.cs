using System;
using Unity.Cinemachine;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool gameRunning = false;
    public bool timerRunning = false;
    private float _elapsedTime = 0f;

    [SerializeField] private GameObject player;
    [SerializeField] private GameObject chaser;
    [SerializeField] private CameraFollowPlayer cam;

    public event Action gameWon;
    public event Action gameLost;
    public string FormattedTime
    {
        get
        {
            int minutes      = Mathf.FloorToInt(_elapsedTime / 60f);
            int seconds      = Mathf.FloorToInt(_elapsedTime % 60f);
            int milliseconds = Mathf.FloorToInt((_elapsedTime * 100f) % 100f);
            return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
        }
    }

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
    }
    
    private void Update()
    {
        if(timerRunning)
            _elapsedTime += Time.deltaTime;
    }

    public void StartTimer()
    {
        timerRunning = true;
        gameRunning = true;
    }
    
    public void StopTimer()
    {
        timerRunning = false;
    }
    
    public void InitRewind()
    {
        cam.player = chaser.transform;
        chaser.SetActive(true);
    }

    public void CatchPlayer()
    {
        Debug.Log($"You win: " + FormattedTime);
        gameRunning = false;
        gameWon?.Invoke();
    }

    public void PlayerEscaped()
    {
        Debug.Log($"You lost: Player Escaped");
        gameRunning = false;
        gameLost?.Invoke();
        cam.player = player.transform;
    }
}
