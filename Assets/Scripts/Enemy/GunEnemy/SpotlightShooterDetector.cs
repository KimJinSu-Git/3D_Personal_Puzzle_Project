using UnityEngine;

public class SpotlightShooterDetector : MonoBehaviour
{
    [Header("감지 설정")]
    public float detectionRange = 20f;
    public float detectionAngle = 30f;
    public int rayCount = 10;
    public LayerMask detectionMask;

    [Header("디버그")]
    public bool debugRays = true;

    [SerializeField] private PatrolShooter shooter;
    private PlayerController player;

    void Update()
    {
        DetectPlayer();
    }

    void DetectPlayer()
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

                    // Player가 수면 위 상태가 아닐 경우 무시
                    if (!(player.stateMachine.CurrentState is PlayerSwimSurfaceState))
                        continue;

                    // 사격 명령 전달
                    SoundManager.Instance.PlayEnemySFX("HandGun_Detect");
                    shooter.OnPlayerDetected(player);
                    break;
                }
            }

            if (debugRays)
                Debug.DrawRay(origin, dir * detectionRange, Color.red);
        }
    }
}