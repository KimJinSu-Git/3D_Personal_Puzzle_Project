using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SavePoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerRespawnManager.Instance.SetRespawnPoint(transform.position);
            Debug.Log(transform.position);
        }
    }
}