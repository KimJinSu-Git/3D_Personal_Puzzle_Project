using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum BreathState
{
    None,    
    Slow,     
    Fast      
}
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    private BreathState currentBreathState = BreathState.None;
    private Coroutine breathCooldownCoroutine;
    
    [Header("움직임 설정")]
    public float walkSpeed = 2f;
    public float runSpeed = 3f;
    public float jumpForce = 3f;
    public float swimSpeed = 2.5f;

    [Header("구성 요소")]
    public Rigidbody rb;
    public Animator animator;
    public CapsuleCollider capsule;
    public Transform visualRoot;
    public Quaternion originalVisualRotation;
    public GameObject skirtPos;
    
    [Header("파티클 시스템")]
    public GameObject drowningParticle;

    [Header("플레이어 감지 체크 요소")] 
    public bool crouching = false;
    public bool crawling = false;
    public bool caughtDie = false;
    public bool isDie = false;

    public bool isGrounded;
    public bool allowWaterImpact = false;
    public bool isFacingRight = true;
    public Vector3 lastFallVelocity;
    [HideInInspector] public Transform currentLadder;

    public PlayerStateMachine stateMachine;

    private Coroutine colliderLerpRoutine;
    private float pushCheckDistance = 0.3f;
    private float yRotation;
    
    private bool isInCutscene = false;
    
    public bool isInWater = false;
    public float? waterSurfaceY = null;
    public float underwaterTime = 0f;
    public float maxUnderwaterTime = 10f;

    [SerializeField] private GameObject pauseUI;
    
    /// <summary>
    /// 상태 종류들
    /// </summary>
    public PlayerIdleState idleState;
    public PlayerMoveState moveState;
    public PlayerJumpState jumpState;
    public PlayerFallState fallState;
    public PlayerDeathState deathState;
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
    
    public PlayerWaterImpactState  waterImpactState;
    public PlayerSwimSurfaceState swimSurfaceState;
    public PlayerSwimTurnState swimTurnState;
    public PlayerUnderwaterSwimState  underwaterSwimState;
    public PlayerUnderwaterTurnState underwaterTurnState;
    public PlayerDrowningState drowningState;
    
    public PlayerCaughtState caughtState;
    
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
        deathState = new PlayerDeathState(this, stateMachine);
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

        waterImpactState = new PlayerWaterImpactState(this, stateMachine);
        swimSurfaceState = new PlayerSwimSurfaceState(this, stateMachine);
        swimTurnState = new PlayerSwimTurnState(this, stateMachine);
        underwaterSwimState = new PlayerUnderwaterSwimState(this, stateMachine);
        underwaterTurnState = new PlayerUnderwaterTurnState(this, stateMachine);
        drowningState = new PlayerDrowningState(this, stateMachine);

        caughtState = new PlayerCaughtState(this, stateMachine);
    }

    private void Start()
    {
        stateMachine.Initialize(idleState);
        SoundManager.Instance.PlayBGM("Background Ambient");
        SoundManager.Instance.PlayBreath("Player_Breath_Slow");
        currentBreathState = BreathState.Slow;
    }

    private void Update()
    {
        if (isInCutscene || pauseUI.activeSelf) return;
        
        stateMachine.Update();
        Debug.Log(stateMachine.CurrentState);
        isFacingCheck();

        if (Input.GetKeyDown(KeyCode.F))
        {
            stateMachine.ChangeState(deathState);
        }

        if (underwaterTime >= 5f && underwaterTime <= 10f)
        {
            SoundManager.Instance.PlayBreath("Player_UnderWater_Breath");
            if (drowningParticle != null)
                drowningParticle.SetActive(true);
        }
        else if (underwaterTime >= 10f)
        {
            SoundManager.Instance.PlayBreath("Player_UnderWater_Death");
        }
    }

    private void LateUpdate()
    {
        if (pauseUI.activeSelf)
        {
            SoundManager.Instance.PauseAllExceptBGM();
            Time.timeScale = 0;
        }
        else
        {
            SoundManager.Instance.ResumePausedSFX();
            Time.timeScale = 1;
        }
    }

    private void FixedUpdate()
    {
        if (isInCutscene)
        {
            rb.velocity = transform.forward * 2f;
        }
    }
    
    public void SetBreathState(BreathState newState)
    {
        if (currentBreathState == newState)
            return;

        currentBreathState = newState;

        switch (newState)
        {
            case BreathState.None:
                SoundManager.Instance.StopBreath();
                break;
            case BreathState.Slow:
                SoundManager.Instance.PlayBreath("Player_Breath_Slow", true);
                break;
            case BreathState.Fast:
                SoundManager.Instance.PlayBreath("Player_Breath_Fast", true);
                break;
        }
    }

    /// <summary>
    /// 달리기를 멈췄을 때 일정 시간 뒤 Slow 상태로 천천히 숨 쉬기 전환
    /// </summary>
    public void StartBreathCooldown(float delay = 1f)
    {
        if (breathCooldownCoroutine != null)
            StopCoroutine(breathCooldownCoroutine);
    
        breathCooldownCoroutine = StartCoroutine(BreathCooldownCoroutine(delay));
    }

    private IEnumerator BreathCooldownCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (currentBreathState == BreathState.Fast)
            SetBreathState(BreathState.Slow);
    }
    
    public void EnterCutsceneMode()
    {
        animator.CrossFade("Idle_Walk_Run", 0.1f);
        isInCutscene = true;
        SoundManager.Instance.StopBreath();
        rb.velocity = Vector3.zero;
    }

    private void isFacingCheck()
    {
        yRotation = transform.eulerAngles.y;
        isFacingRight = Mathf.Approximately(yRotation, 0f);
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

    public void SetDeathCollider(float duration = 0.25f)
    {
        LerpCollider(new Vector3(0f, 0.436f, 0f), 0.8733f, 2, duration);
    }
    
    public bool IsHeadBlocked()
    {
        Vector3 headCenter = transform.position + Vector3.up * 0.9f; 
        float radius = 0.2f;
        float checkHeight = 0.3f;
    
        Vector3 topPoint = headCenter + Vector3.up * checkHeight;

        return Physics.CheckCapsule(headCenter, topPoint, radius, LayerMask.GetMask("Default"));
    }
    
    private void OnCollisionStay(Collision other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground") || 
            other.contacts[0].normal.y > 0.5f ||
            other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground") || 
            other.gameObject.layer == LayerMask.NameToLayer("Pushable") ||
            other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            isGrounded = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            isInWater = false;
            waterSurfaceY = null;
            underwaterTime = 0f;
            
            if (drowningParticle != null)
                drowningParticle.SetActive(false);
            
            SoundManager.Instance.PlayBGM("Background Ambient");
            SoundManager.Instance.PlayBreath("Player_Breath_Slow");
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

        if (other.gameObject.layer == LayerMask.NameToLayer("Water") && !isInWater)
        {
            isInWater = true;

            Collider waterCollider = other.GetComponent<Collider>();
            if (waterCollider != null)
            {
                float surfaceY = waterCollider.bounds.max.y;
                if (!waterSurfaceY.HasValue || Mathf.Abs(waterSurfaceY.Value - surfaceY) > 0.1f)
                    waterSurfaceY = surfaceY;
            }

            if (allowWaterImpact && lastFallVelocity.y > 6f)
            {
                stateMachine.ChangeState(waterImpactState);
            }
            else
            {
                stateMachine.ChangeState(swimSurfaceState);
            }
        }
    }
    
    public bool CheckPushableObject()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = transform.forward;
        
        return Physics.Raycast(origin, dir, pushCheckDistance, LayerMask.GetMask("Pushable"));
    }
    
    /// <summary>
    /// 사다리 관련 함수들
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    ///  물 관련 함수들
    /// </summary>
    /// <returns></returns>
    public bool IsSubmerged()
    {
        return waterSurfaceY.HasValue && transform.position.y < waterSurfaceY.Value - 0.3f;
    }
    public bool IsInWater()
    {
        return isInWater && waterSurfaceY.HasValue;
    }
    
    /// <summary>
    /// 콜라이더 보간 함수와 코루틴
    /// </summary>
    /// <param name="targetCenter"></param>
    /// <param name="targetHeight"></param>
    /// <param name="targetDirection"></param>
    /// <param name="duration"></param>
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
    
    public bool CheckLeverInFront(out LeverController lever)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 direction = transform.forward;

        Debug.DrawRay(origin, direction * 0.4f, Color.red, 1f);

        Ray ray = new Ray(origin, direction);
        if (Physics.Raycast(ray, out RaycastHit hit, 0.4f, LayerMask.GetMask("Lever")))
        {
            lever = hit.collider.GetComponent<LeverController>();
            return lever != null;
        }

        lever = null;
        return false;
    }
}
