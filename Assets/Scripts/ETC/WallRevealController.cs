using UnityEngine;
using System.Collections;

public class WallRevealController : MonoBehaviour
{
    [SerializeField] private GameObject wallToHide;

    private Coroutine restoreCoroutine;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (restoreCoroutine != null)
            {
                StopCoroutine(restoreCoroutine);
                restoreCoroutine = null;
            }

            wallToHide.SetActive(false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            restoreCoroutine = StartCoroutine(RestoreWallAfterDelay(2f));
        }
    }

    private IEnumerator RestoreWallAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        wallToHide.SetActive(true);
        restoreCoroutine = null;
    }
}