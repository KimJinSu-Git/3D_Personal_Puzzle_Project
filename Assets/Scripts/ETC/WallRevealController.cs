using UnityEngine;

public class WallRevealController : MonoBehaviour
{
    [SerializeField] private GameObject wallToHide;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            wallToHide.SetActive(false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            wallToHide.SetActive(true);
        }
    }
}