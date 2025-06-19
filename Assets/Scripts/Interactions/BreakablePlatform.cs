using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class BreakablePlatform : MonoBehaviour
{
    public float fallDelay = 0.5f;

    [SerializeField] private bool isTriggered = false;
    private float fallTimer = 0f;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    private void OnEnable()
    {
        GameResetEvent.OnPlayerReset += ResetPlatform;
    }

    private void OnDisable()
    {
        GameResetEvent.OnPlayerReset -= ResetPlatform;
    }

    private void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Player") ||
            other.gameObject.layer == LayerMask.NameToLayer("Pushable"))
        {
            Debug.Log("무너진다");
            isTriggered = true;
        }
    }

    private void Update()
    {
        if (isTriggered)
        {
            fallTimer += Time.deltaTime;
            if (fallTimer >= fallDelay)
            {
                rb.useGravity = true;
                rb.isKinematic = false;
            }
        }
    }

    public void ResetPlatform()
    {
        isTriggered = false;
        fallTimer = 0f;

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = originalPosition;
        transform.rotation = originalRotation;
    }
}