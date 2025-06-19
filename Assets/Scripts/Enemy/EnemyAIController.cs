using UnityEngine;
using UnityEngine.AI;

public class EnemyAIController : MonoBehaviour
{
    public enum GuardState { Idle, Chasing, Catching, Returning }
    private GuardState currentState = GuardState.Idle;

    private Vector3 startPosition;
    private NavMeshAgent agent;
    private PlayerController targetPlayer;

    [Header("추격 설정")]
    public float chaseSpeed = 4f;
    public float returnSpeed = 2f;
    public float maxChaseDistance = 15f;
    public float catchDistance = 1.5f;

    [Header("애니메이션 및 잡기 처리")]
    public Animator animator;
    public Transform catchHandTransform;

    private float catchTimer = 0f;
    private float throwDelay = 1.2f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        startPosition = transform.position;
    }

    private void Update()
    {
        switch (currentState)
        {
            case GuardState.Idle:
                PatrolOrIdle();
                break;

            case GuardState.Chasing:
                ChasePlayer();
                break;

            case GuardState.Catching:
                HandleCatching();
                break;

            case GuardState.Returning:
                ReturnToStart();
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

        if (newState == GuardState.Returning)
        {
            agent.speed = returnSpeed;
            targetPlayer = null;
            Debug.Log("Guard: 복귀 시작");
        }
        else if (newState == GuardState.Idle)
        {
            agent.speed = 0f;
            Debug.Log("Guard: 대기 상태");
        }
        else if (newState == GuardState.Catching)
        {
            agent.isStopped = true;
            animator.SetTrigger("Catch_Start");

            if (targetPlayer != null)
            {
                targetPlayer.transform.SetParent(catchHandTransform);
                targetPlayer.transform.localPosition = Vector3.zero;
                targetPlayer.rb.isKinematic = true;
                targetPlayer.stateMachine.ChangeState(targetPlayer.idleState);
            }

            catchTimer = 0f;
        }
    }

    private void ChasePlayer()
    {
        if (targetPlayer == null)
        {
            ChangeState(GuardState.Returning);
            return;
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
        catchTimer += Time.deltaTime;

        if (catchTimer >= throwDelay)
        {
            animator.SetTrigger("Catch_Throw");
        }

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (info.IsName("Catch_Throw") && info.normalizedTime >= 0.95f)
        {
            if (targetPlayer != null)
            {
                targetPlayer.transform.SetParent(null);
                targetPlayer.rb.isKinematic = false;
                targetPlayer.rb.AddForce(Vector3.back * 5f + Vector3.up * 3f, ForceMode.Impulse);
                targetPlayer.stateMachine.ChangeState(targetPlayer.deathState);
            }

            agent.isStopped = false;
            ChangeState(GuardState.Returning);
        }
    }

    private void ReturnToStart()
    {
        agent.SetDestination(startPosition);
        if (Vector3.Distance(transform.position, startPosition) < 0.5f)
        {
            ChangeState(GuardState.Idle);
        }
    }

    private void PatrolOrIdle()
    {
        // 순찰 구현 가능
    }
}
