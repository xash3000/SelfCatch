using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool gameRunning = true;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
    }
}
