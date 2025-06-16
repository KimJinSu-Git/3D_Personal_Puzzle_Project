using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRespawnManager : MonoBehaviour
{
    public static PlayerRespawnManager Instance { get; private set; }

    private Vector3 currentRespawnPoint;

    private void Awake()
    {
        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public void SetRespawnPoint(Vector3 point)
    {
        currentRespawnPoint = point;
    }

    public Vector3 GetRespawnPoint()
    {
        return currentRespawnPoint;
    }
}