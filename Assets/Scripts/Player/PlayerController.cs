using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("움직임 설정")]
    public float walkSpeed = 2f;
    public float runSpeed = 3f;
    public float jumpForce = 3f;

    [Header("구성 요소")]
    public Rigidbody rb;
    public Animator animator;
    public CapsuleCollider capsule;
    public Transform visualRoot;
    public Quaternion originalVisualRotation;

    [HideInInspector] public bool isGrounded;
    [HideInInspector] public Transform currentLadder;

    public PlayerStateMachine stateMachine;

    private Coroutine colliderLerpRoutine;
    private float pushCheckDistance = 0.2f;
    
    /// <summary>
    /// 상태 종류들
    /// </summary>
    public PlayerIdleState idleState;
    public PlayerMoveState moveState;
    public PlayerJumpState jumpState;
    public PlayerFallState fallState;
    public PlayerTurnState turnState;
    
    public PlayerCrouchBlendState crouchBlendState;
    public PlayerCrouchTurnState crouchTurnState;
    public PlayerCrouchToggleState crouchEnterState;
    public PlayerCrouchToggleState crouchExitState;
    
    public PlayerCrawlTransitionState crawlTransitionState;
    public PlayerCrawlBlendState crawlBlendState;
    public PlayerCrawlExitState crawlExitState;
    
    public PlayerPushEnterState pushEnterState;
    public PlayerPushBlendState pushBlendState;
    public PlayerPushExitState pushExitState;
    
    public PlayerLadderEnterUpState ladderEnterUpState;
    public PlayerLadderEnterDownState ladderEnterDownState;
    public PlayerLadderClimbState ladderClimbState;
    public PlayerLadderExitTopState ladderExitTopState;
    public PlayerLadderExitBottomState ladderExitBottomState;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        capsule = GetComponent<CapsuleCollider>();
        
        stateMachine = new PlayerStateMachine();
        
        idleState = new PlayerIdleState(this, stateMachine);
        moveState = new PlayerMoveState(this, stateMachine);
        jumpState = new PlayerJumpState(this, stateMachine);
        fallState = new PlayerFallState(this, stateMachine);
        turnState = new PlayerTurnState(this, stateMachine);
        
        crouchEnterState = new PlayerCrouchToggleState(this, stateMachine, true);
        crouchExitState = new PlayerCrouchToggleState(this, stateMachine, false);
        crouchBlendState = new PlayerCrouchBlendState(this, stateMachine);
        crouchTurnState = new PlayerCrouchTurnState(this, stateMachine);
        
        crawlTransitionState = new PlayerCrawlTransitionState(this, stateMachine);
        crawlBlendState = new PlayerCrawlBlendState(this, stateMachine);
        crawlExitState = new PlayerCrawlExitState(this, stateMachine);
        
        pushEnterState = new PlayerPushEnterState(this, stateMachine);
        pushBlendState = new PlayerPushBlendState(this, stateMachine);
        pushExitState = new PlayerPushExitState(this, stateMachine);
        
        ladderEnterUpState = new PlayerLadderEnterUpState(this, stateMachine);
        ladderEnterDownState = new PlayerLadderEnterDownState(this, stateMachine);
        ladderClimbState = new PlayerLadderClimbState(this, stateMachine);
        ladderExitTopState = new PlayerLadderExitTopState(this, stateMachine);
        ladderExitBottomState = new PlayerLadderExitBottomState(this, stateMachine);
    }

    private void Start()
    {
        stateMachine.Initialize(idleState);
    }

    private void Update()
    {
        stateMachine.Update();
        Debug.Log(isGrounded);
    }
    
    public void SetStandingCollider(float duration = 0.25f)
    {
        LerpCollider(new Vector3(0f, 0.436f, 0f), 0.8733f, 1, duration);
    }

    public void SetCrouchCollider(float duration = 0.25f)
    {
        LerpCollider(new Vector3(0f, 0.3f, 0f), 0.6f, 1, duration);
    }

    public void SetCrawlingCollider(float duration = 0.25f)
    {
        LerpCollider(new Vector3(0f, 0.15f, 0f), 0.50f, 2, duration);
    }
    
    public bool IsHeadBlocked()
    {
        Vector3 headCenter = transform.position + Vector3.up * 0.9f; 
        float radius = 0.2f;
        float checkHeight = 0.3f;
    
        Vector3 topPoint = headCenter + Vector3.up * checkHeight;

        // 머리 위에 충돌이 있으면 true
        return Physics.CheckCapsule(headCenter, topPoint, radius, LayerMask.GetMask("Default"));
    }
    
    private void OnCollisionStay(Collision other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Pushable"))
        {
            isGrounded = false;
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("LadderTop") && stateMachine.CurrentState == ladderClimbState)
        {
            float inputY = Input.GetAxisRaw("Vertical");

            if (inputY > 0.1f)
            { 
                stateMachine.ChangeState(ladderExitTopState);
            }
        }
    }
    
    public bool CheckPushableObject()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = transform.forward;
        
        return Physics.Raycast(origin, dir, pushCheckDistance, LayerMask.GetMask("Pushable"));
    }
    
    public bool CheckLadderBelowFront()
    {
        Vector3 frontOffset = transform.forward * 0.25f;
        Vector3 origin = transform.position + frontOffset + Vector3.up * 0.3f;
        float distance = 1.0f;
        
        Debug.DrawRay(origin, Vector3.down * distance, Color.red, 2f);

        return Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, LayerMask.GetMask("Ladder"));
    }
    
    public Transform GetLadderBelowFront()
    {
        Vector3 frontOffset = transform.forward * 0.25f;
        Vector3 origin = transform.position + frontOffset + Vector3.up * 0.3f;
        float distance = 1.0f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, LayerMask.GetMask("Ladder")))
        {
            return hit.transform;
        }

        return null;
    }
    
    public bool IsFacingSameDirectionAsLadder(Transform ladder)
    {
        Vector3 playerForward = transform.forward;
        Vector3 ladderForward = ladder.forward;

        float dot = Vector3.Dot(playerForward.normalized, ladderForward.normalized);

        return dot > 0.8f;
    }
    
    public bool CheckLadderInFront()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = transform.forward;

        if (Physics.Raycast(origin, dir, pushCheckDistance, LayerMask.GetMask("Ladder")))
        {
            return true;
        }
        return false;
    }
    
    public Transform GetLadderInFront()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, 0.5f, LayerMask.GetMask("Ladder")))
        {
            return hit.transform;
        }

        return null;
    }

    public bool CheckLadderBottom()
    {
        return Physics.Raycast(transform.position + Vector3.down * 0.1f, Vector3.down, 0.05f, LayerMask.GetMask("Ground"));
    }
    
    public void LerpCollider(Vector3 targetCenter, float targetHeight, int targetDirection, float duration = 0.25f)
    {
        if (colliderLerpRoutine != null)
            StopCoroutine(colliderLerpRoutine);

        colliderLerpRoutine = StartCoroutine(LerpColliderCoroutine(targetCenter, targetHeight, targetDirection, duration));
    }

    private IEnumerator LerpColliderCoroutine(Vector3 targetCenter, float targetHeight, int targetDirection, float duration)
    {
        Vector3 startCenter = capsule.center;
        float startHeight = capsule.height;
        
        float bottomY = startCenter.y - startHeight * 0.5f;

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            float height = Mathf.Lerp(startHeight, targetHeight, t);
            float centerY = bottomY + height * 0.5f;

            capsule.height = height;
            capsule.center = new Vector3(targetCenter.x, centerY, targetCenter.z);

            yield return null;
        }
        
        capsule.direction = targetDirection;
        capsule.height = targetHeight;
        capsule.center = targetCenter;
    }
}
