using UnityEngine;

public class UnderwaterEffectController : MonoBehaviour
{
    public GameObject underwaterVolumeObject;
    private PlayerController player;

    private void Start()
    {
        player = FindObjectOfType<PlayerController>();
    }

    private void Update()
    {
        if (player == null || underwaterVolumeObject == null) return;

        underwaterVolumeObject.SetActive(player.IsSubmerged());
    }
}