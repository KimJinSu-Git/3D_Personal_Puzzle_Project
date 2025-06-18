using UnityEngine;

public class RespawnCheckpoint : MonoBehaviour
{
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            PlayerRespawnManager.Instance.SetRespawnPoint(transform.position, transform);
            triggered = true;
        }
    }
}