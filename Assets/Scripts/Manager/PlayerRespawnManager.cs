using System.Collections.Generic;
using UnityEngine;

public class PlayerRespawnManager : MonoBehaviour
{
    public static PlayerRespawnManager Instance { get; private set; }

    private Vector3 currentRespawnPoint;
    private bool hasSaved = false;
    private Transform lastCheckpoint;

    private Dictionary<Transform, Vector3> savedObjectPositions = new();

    [Header("리스폰 대상 그룹")]
    public Transform respawnObjectGroup;

    private void Awake()
    {
        Instance = this;
    }

    public void SetRespawnPoint(Vector3 point, Transform checkpointTransform)
    {
        if (lastCheckpoint == checkpointTransform)
        {
            return;
        }
        

        currentRespawnPoint = point;
        lastCheckpoint = checkpointTransform;

        SaveStructurePositions();
    }

    public Vector3 GetRespawnPoint()
    {
        return currentRespawnPoint;
    }

    public void RespawnPlayer(GameObject player)
    {
        player.transform.position = currentRespawnPoint;
        
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        RestoreStructurePositions();
    }

    private void SaveStructurePositions()
    {
        savedObjectPositions.Clear();
        foreach (Transform child in respawnObjectGroup)
        {
            savedObjectPositions[child] = child.position;
        }
    }

    private void RestoreStructurePositions()
    {
        foreach (var kvp in savedObjectPositions)
        {
            kvp.Key.position = kvp.Value;
        }
    }
}