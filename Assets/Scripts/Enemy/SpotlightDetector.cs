using UnityEngine;

public class SpotlightDetector : MonoBehaviour
{
    [Header("감지 설정")]
    public float detectionRange = 10f;
    public float detectionAngle = 30f;
    public int rayCount = 10;
    public LayerMask detectionMask; // Player만 감지하는 레이어

    [Header("디버그")]
    public bool debugRays = true;

    [SerializeField] private EnemyAIController enemy;
    private PlayerController player;

    private void Update()
    {
        DetectPlayer();
    }

    private void DetectPlayer()
    {
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        float halfAngle = detectionAngle;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / (rayCount - 1));
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * forward;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, detectionRange, detectionMask))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    player = hit.transform.GetComponent<PlayerController>();
                    
                    if (player.crouching || player.crawling) continue;

                    Debug.Log("플레이어 감지!");
                    enemy.OnPlayerDetected(player);
                    break;
                }
                else
                {
                    continue;
                }
            }

            if (debugRays)
                Debug.DrawRay(origin, dir * detectionRange, Color.yellow);
        }
    }
}