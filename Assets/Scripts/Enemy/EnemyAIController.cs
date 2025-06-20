using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAIController : MonoBehaviour
{
    public enum GuardState { Idle, Chasing, Catching, Returning }
    private GuardState currentState = GuardState.Idle;

    private Vector3 startPosition;
    private NavMeshAgent agent;
    private PlayerController targetPlayer;
    private CapsuleCollider collider;
    private Rigidbody rb;
    private AnimatorStateInfo info;
    private Vector3 startCollider;
    private Vector3 targetCollider;

    [Header("추격 설정")]
    public float chaseSpeed = 4f;
    public float returnSpeed = 1f;
    public float maxChaseDistance = 15f;
    public float catchDistance = 1.5f;

    [Header("애니메이션 및 잡기 처리")]
    public Animator animator;
    public Transform catchHandTransform;

    [Header("장애물 처리")]
    public float vaultCheckDistance = 1.2f;
    public string vaultableTag = "Obstacle";
    public float vaultResumeDelay = 1.0f; // vault 애니메이션 길이만큼

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
        Debug.Log(rb.velocity);
        
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
        if (currentState == GuardState.Catching) return;

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
        // 추후 순찰 경로 구현 가능
    }

    private void CheckForObstacleVault()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out RaycastHit hit, vaultCheckDistance))
        {
            if (hit.collider.CompareTag(vaultableTag))
            {
                animator.Play("Obstacle_Vault");
                collider.enabled = false;
                rb.isKinematic = true;
                Invoke(nameof(ResumeAfterVault), vaultResumeDelay);
                return;
            }
        }

        if (!info.IsName("Obstacle_Vault"))
        {
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