using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAIController : MonoBehaviour
{
    public enum GuardState { Idle, Chasing, Catching, Returning }
    private GuardState currentState = GuardState.Idle;

    [Header("추격 설정")]
    public float chaseSpeed = 3.6f;
    public float returnSpeed = 1f;
    public float maxChaseDistance = 30f;
    public float catchDistance = 1f;

    [Header("애니메이션 및 잡기 처리")]
    public Animator animator;
    public Transform catchHandTransform;

    [Header("장애물 처리")]
    public float vaultCheckDistance = 1.2f;
    public string vaultableTag = "Obstacle";
    public float vaultResumeDelay = 1.0f;
    
    [Header("좁은 통로 감지 설정")]
    public Transform headTransform;
    public float obstacleCheckDistance = 0.3f;
    public LayerMask obstacleLayer;

    private bool obstacleDetected = false;
    private bool isBumping = false;
    private float bumpTimer = 0f;
    private bool isFailed = false;
    
    // 구성요소
    private NavMeshAgent agent;
    private PlayerController targetPlayer;
    private CapsuleCollider collider;
    private Rigidbody rb;
    private AnimatorStateInfo info;
    
    // 값 저장할 변수
    private Vector3 startPosition;
    private Vector3 startCollider;
    private Vector3 targetCollider;
    
    // 가만히 있을 때의 상태 변수
    [SerializeField] private float idleRotationSpeed = 0.2f; // 회전 속도
    private float idleRotationMin = 70f;
    private float idleRotationMax = 110f;
    private float idleRotationTimer = 0f;

    private float catchTimer = 0f;
    private bool hasTriggeredDeath = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        collider = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        rb.isKinematic = true;
        startCollider = collider.center;
        targetCollider = new Vector3(0, 1.27f, 0);
    }

    private void Update()
    {
        info = animator.GetCurrentAnimatorStateInfo(0);
        
        switch (currentState)
        {
            case GuardState.Idle:
                PatrolOrIdle();
                break;

            case GuardState.Chasing:
                ChasePlayer();
                CheckForObstacleVault();
                break;

            case GuardState.Catching:
                HandleCatching();
                break;

            case GuardState.Returning:
                ReturnToStart();
                CheckForObstacleVault();
                break;
        }
    }

    public void OnPlayerDetected(PlayerController player)
    {
        if (currentState == GuardState.Catching || isFailed) return;

        targetPlayer = player;
        agent.speed = chaseSpeed;
        ChangeState(GuardState.Chasing);
        Debug.Log("Guard: 플레이어 감지, 추격 시작");
    }

    private void ChangeState(GuardState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case GuardState.Idle:
                rb.isKinematic = true;
                agent.speed = 0f;
                animator.Play("Idle");
                break;
            case GuardState.Chasing:
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                animator.Play("Run");
                break;
            case GuardState.Catching:
                rb.isKinematic = false;
                collider.center = targetCollider;
                agent.isStopped = true;
                animator.Play("Plunge");
                if (targetPlayer != null && !targetPlayer.caughtDie)
                {
                    targetPlayer.transform.SetParent(catchHandTransform);
                    targetPlayer.stateMachine.ChangeState(targetPlayer.caughtState);
                }
                catchTimer = 0f;
                break;
            case GuardState.Returning:
                collider.center = startCollider;
                agent.speed = returnSpeed;
                agent.isStopped = false;
                targetPlayer = null;
                animator.Play("Walk");
                break;
        }
    }

    private void ChasePlayer()
    {
        if (targetPlayer == null)
        {
            ChangeState(GuardState.Returning);
            return;
        }
        
        // 장애물 앞에 막혔는지 체크
        if (!isBumping && CheckForObstacleHead())
        {
            StartBumpReaction();
            return;
        }

        if (isBumping)
        {
            bumpTimer += Time.deltaTime;
            if (bumpTimer >= 1f)
            {
                isBumping = false;
                ChangeState(GuardState.Returning);
            }
            return;
        }

        if (info.IsName("Obstacle_Vault") && info.normalizedTime >= 0.9f)
        {
            animator.SetTrigger("Obstacle_Run");
        }

        agent.SetDestination(targetPlayer.transform.position);

        float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (distance <= catchDistance)
        {
            ChangeState(GuardState.Catching);
        }
        else if (distance > maxChaseDistance)
        {
            ChangeState(GuardState.Returning);
        }
    }

    private void HandleCatching()
    {
        if (info.IsName("Plunge"))
        {
            float yRot = transform.rotation.eulerAngles.y; // 월드 기준 회전 값
            float angleToForward = Mathf.Abs(Mathf.DeltaAngle(yRot, 0f));
            float angleToBackward = Mathf.Abs(Mathf.DeltaAngle(yRot, 180f));

            if (angleToForward < angleToBackward)
            {
                transform.localRotation = Quaternion.Euler(0f, -30f, 0f); // 전방
            }
            else
            {
                transform.localRotation = Quaternion.Euler(0f, 120f, 0f); // 후방
            }
            
            var vector3 = transform.position;
            vector3.x = 0;
            transform.position = vector3;
            if (!hasTriggeredDeath && info.normalizedTime >= 0.3f && targetPlayer != null)
            {
                hasTriggeredDeath = true;
                targetPlayer.transform.SetParent(null);
                targetPlayer.stateMachine.ChangeState(targetPlayer.deathState);
            }

            if (info.normalizedTime >= 0.7f)
            {
                hasTriggeredDeath = false;
                ChangeState(GuardState.Returning);
            }
        }
    }

    private void ReturnToStart()
    {
        agent.SetDestination(startPosition);
        isFailed = false;
        if (info.IsName("Obstacle_Vault") && info.normalizedTime >= 0.9f)
        {
            animator.SetTrigger("Obstacle_Walk");
        }
        if (Vector3.Distance(transform.position, startPosition) < 0.5f)
        {
            ChangeState(GuardState.Idle);
        }
    }

    private void PatrolOrIdle()
    {
        idleRotationTimer += Time.deltaTime * idleRotationSpeed;
        float t = Mathf.PingPong(idleRotationTimer, 1f);

        float targetY = Mathf.Lerp(idleRotationMin, idleRotationMax, t);

        Vector3 currentEuler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, targetY, 0f);
    }
    
    private bool CheckForObstacleHead()
    {
        Debug.DrawRay(headTransform.position, headTransform.up * obstacleCheckDistance, Color.red);
        if (Physics.Raycast(headTransform.position, headTransform.up, out RaycastHit hit, obstacleCheckDistance, obstacleLayer))
        {
            if (hit.collider.CompareTag("Obstacle_Head"))
            {
                return true;
            }
        }
        return false;
    }
    
    private void StartBumpReaction()
    {
        isFailed = true;
        isBumping = true;
        bumpTimer = 0f;

        agent.isStopped = true;
        animator.Play("Obstacle_Bump");
    }

    private void CheckForObstacleVault()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out RaycastHit hit, vaultCheckDistance))
        {
            if (hit.collider.CompareTag(vaultableTag))
            {
                agent.speed = 4f;
                animator.Play("Obstacle_Vault");
                collider.enabled = false;
                rb.isKinematic = true;
                Invoke(nameof(ResumeAfterVault), vaultResumeDelay);
                return;
            }
        }

        if (!info.IsName("Obstacle_Vault"))
        {
            agent.speed = chaseSpeed;
            collider.enabled = true;
            rb.isKinematic = false;
        }
    }

    private void ResumeAfterVault()
    {
        agent.isStopped = false;
    }

    private void ResetEnemy()
    {
        ChangeState(GuardState.Idle);
    }

    private void OnEnable()
    {
        GameResetEvent.OnPlayerReset += ResetEnemy;
    }

    private void OnDisable()
    {
        GameResetEvent.OnPlayerReset -= ResetEnemy;
    }
}