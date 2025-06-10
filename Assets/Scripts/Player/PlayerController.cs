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
    [SerializeField] private CapsuleCollider capsule;

    [HideInInspector] public bool isGrounded;

    public PlayerStateMachine stateMachine;

    /// <summary>
    /// 상태 종류들
    /// </summary>
    public PlayerIdleState idleState;
    public PlayerMoveState moveState;
    public PlayerJumpState jumpState;
    public PlayerTurnState turnState;
    public PlayerCrouchBlendState crouchBlendState;
    public PlayerCrouchTurnState crouchTurnState;
    public PlayerCrouchToggleState crouchEnterState;
    public PlayerCrouchToggleState crouchExitState;
    public PlayerCrawlTransitionState crawlTransitionState;
    public PlayerCrawlBlendState crawlBlendState;
    public PlayerCrawlExitState crawlExitState;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        capsule = GetComponent<CapsuleCollider>();
        
        stateMachine = new PlayerStateMachine();
        idleState = new PlayerIdleState(this, stateMachine);
        moveState = new PlayerMoveState(this, stateMachine);
        jumpState = new PlayerJumpState(this, stateMachine);
        turnState = new PlayerTurnState(this, stateMachine);
        
        crouchEnterState = new PlayerCrouchToggleState(this, stateMachine, true);
        crouchExitState = new PlayerCrouchToggleState(this, stateMachine, false);
        crouchBlendState = new PlayerCrouchBlendState(this, stateMachine);
        crouchTurnState = new PlayerCrouchTurnState(this, stateMachine);
        crawlTransitionState = new PlayerCrawlTransitionState(this, stateMachine);
        crawlBlendState = new PlayerCrawlBlendState(this, stateMachine);
        crawlExitState = new PlayerCrawlExitState(this, stateMachine);
    }

    private void Start()
    {
        stateMachine.Initialize(idleState);
    }

    private void Update()
    {
        stateMachine.Update();
    }
    
    public void SetStandingCollider()
    {
        capsule.center = new Vector3(0f, 0.436f, 0f);
        capsule.height = 0.8733f;
        capsule.direction = 1;
    }

    public void SetCrouchCollider()
    {
        capsule.center = new Vector3(0f, 0.3f, 0f);
        capsule.height = 0.6f;
        capsule.direction = 1;
    }

    public void SetCrawlingCollider()
    {
        capsule.center = new Vector3(0f, 0.15f, 0f);
        capsule.height = 0.72f;
        capsule.direction = 2;
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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}
