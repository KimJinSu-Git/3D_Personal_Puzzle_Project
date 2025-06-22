using UnityEngine;
using Cinemachine;

public class BreakableTile : MonoBehaviour
{
    private Rigidbody rb;
    private bool hasBroken = false;

    [SerializeField] private GameObject dustEffectPrefab;

    private static bool hasShaken = false;
    private static CinemachineImpulseSource sharedImpulseSource;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        if (sharedImpulseSource == null)
        {
            GameObject obj = GameObject.Find("GlobalImpulseManager");
            if (obj != null)
                sharedImpulseSource = obj.GetComponent<CinemachineImpulseSource>();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasBroken) return;

        if (collision.gameObject.CompareTag("FallingBox"))
        {
            hasBroken = true;
            rb.isKinematic = false;
            rb.useGravity = true;

            if (dustEffectPrefab != null)
            {
                GameObject dust = Instantiate(dustEffectPrefab, transform.position, Quaternion.identity);
                Destroy(dust, 3f);
            }

            if (!hasShaken && sharedImpulseSource != null)
            {
                hasShaken = true;
                sharedImpulseSource.GenerateImpulse();
            }
        }
    }
}