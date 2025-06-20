using System.Collections.Generic;
using UnityEngine;

public class PlayerRespawnManager : MonoBehaviour
{
    public static PlayerRespawnManager Instance { get; private set; }

    private Vector3 currentRespawnPoint;
    private Transform lastCheckpoint;

    [Header("리스폰 대상 그룹")]
    public Transform respawnObjectGroup;

    // 위치 + 회전 데이터를 저장하는 구조체
    private struct TransformData
    {
        public Vector3 position;
        public Quaternion rotation;

        public TransformData(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }
    }

    private Dictionary<Transform, TransformData> savedObjectTransforms = new();

    private void Awake()
    {
        Instance = this;
    }

    public void SetRespawnPoint(Vector3 point, Transform checkpointTransform)
    {
        if (lastCheckpoint == checkpointTransform)
            return;

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
        savedObjectTransforms.Clear();

        foreach (Transform child in respawnObjectGroup)
        {
            savedObjectTransforms[child] = new TransformData(child.position, child.rotation);
        }
    }

    private void RestoreStructurePositions()
    {
        foreach (var kvp in savedObjectTransforms)
        {
            kvp.Key.position = kvp.Value.position;
            kvp.Key.rotation = kvp.Value.rotation;
        }
    }
}