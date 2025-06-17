using UnityEngine;

public class UnderwaterEffectController : MonoBehaviour
{
    public GameObject underwaterVolumeObject;  // 수중 효과용 Volume 오브젝트
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