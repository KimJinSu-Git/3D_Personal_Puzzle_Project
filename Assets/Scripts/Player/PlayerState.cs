using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerBaseState
{
    protected PlayerController player;
    protected PlayerStateMachine stateMachine;

    protected PlayerBaseState(PlayerController player, PlayerStateMachine stateMachine)
    {
        this.player = player;
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}

public class PlayerIdleState : PlayerBaseState
{
    private static readonly int Speed = Animator.StringToHash("Speed");
    public PlayerIdleState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.SetStandingCollider();
        
        player.rb.velocity = new Vector3(0, player.rb.velocity.y, 0);
        player.GetComponent<Animator>().SetFloat(Speed, 0f);
    }

    public override void Update()
    {
        float inputZ = Input.GetAxisRaw("Horizontal");

        float currentSpeed = player.animator.GetFloat(Speed);
        float targetSpeed = 0f;
        float newSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);
        player.animator.SetFloat(Speed, newSpeed);

        if (Mathf.Abs(inputZ) > 0.1f)
        {
            stateMachine.ChangeState(player.moveState);
            return;
        }

        if (Input.GetButtonDown("Jump") && player.isGrounded)
        {
            stateMachine.ChangeState(player.jumpState);
            return;
        }
        
        if (!player.isGrounded && player.rb.velocity.y < -2f)
        {
            stateMachine.ChangeState(player.fallState);
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            stateMachine.ChangeState(player.crouchEnterState);
            return;
        }
        
        if (player.CheckLadderInFront() && Input.GetKeyDown(KeyCode.E))
        {
            player.currentLadder = player.GetLadderInFront();
            stateMachine.ChangeState(player.ladderEnterUpState);
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.E) && player.CheckLadderBelowFront())
        {
            Transform ladder = player.GetLadderBelowFront();

            if (ladder != null && player.IsFacingSameDirectionAsLadder(ladder))
            {
                player.currentLadder = ladder;
                stateMachine.ChangeState(player.ladderEnterDownState);
                return;
            }
        }
        
        if (Input.GetKeyDown(KeyCode.E) && player.CheckPushableObject())
        {
            stateMachine.ChangeState(player.pushEnterState);
            return;
        }
    }
}

public class PlayerMoveState : PlayerBaseState
{
    private static readonly int Speed = Animator.StringToHash("Speed");

    public PlayerMoveState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.SetStandingCollider();
        
        player.animator.Play("Idle_Walk_Run");
        player.animator.SetFloat(Speed, 1f);
    }

    public override void Update()
    {
        float inputZ = Input.GetAxisRaw("Horizontal");
        bool isMoving = Mathf.Abs(inputZ) > 0.1f;
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        float yRotation = player.transform.eulerAngles.y;
        bool isFacingRight = Mathf.Approximately(yRotation, 0f);
        bool isFacingLeft = Mathf.Approximately(yRotation, 180f);

        bool inputRight = inputZ > 0f;
        bool inputLeft = inputZ < 0f;

        if ((isFacingRight && inputLeft) || (isFacingLeft && inputRight))
        {
            player.turnState.SetTurnData(inputZ, isRunning);
            stateMachine.ChangeState(player.turnState);
            return;
        }

        float moveSpeed = isRunning ? player.runSpeed : player.walkSpeed;
        player.rb.velocity = new Vector3(0, player.rb.velocity.y, inputZ * moveSpeed);

        float targetAnimSpeed = isMoving ? (isRunning ? 1f : 0.5f) : 0f;
        float currentSpeed = player.animator.GetFloat(Speed);
        float newSpeed = Mathf.Lerp(currentSpeed, targetAnimSpeed, Time.deltaTime * 10f);
        player.animator.SetFloat(Speed, newSpeed);

        if (!isMoving)
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        if (Input.GetButtonDown("Jump") && player.isGrounded)
        {
            stateMachine.ChangeState(player.jumpState);
            return;
        }
        
        if (!player.isGrounded && player.rb.velocity.y < -2f)
        {
            stateMachine.ChangeState(player.fallState);
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            stateMachine.ChangeState(player.crouchEnterState);
            return;
        }
        
        if (player.CheckLadderInFront() && Input.GetKeyDown(KeyCode.E))
        {
            player.currentLadder = player.GetLadderInFront();
            stateMachine.ChangeState(player.ladderEnterUpState);
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.E) && player.CheckLadderBelowFront())
        {
            Transform ladder = player.GetLadderBelowFront();

            if (ladder != null && player.IsFacingSameDirectionAsLadder(ladder))
            {
                player.currentLadder = ladder;
                stateMachine.ChangeState(player.ladderEnterDownState);
                return;
            }
        }
        
        if (Input.GetKeyDown(KeyCode.E) && player.CheckPushableObject())
        {
            stateMachine.ChangeState(player.pushEnterState);
            return;
        }
    }
}

public class PlayerTurnState : PlayerBaseState
{
    private static readonly int IsTurning = Animator.StringToHash("isTurning");
    private static readonly int RunTurnTrigger = Animator.StringToHash("run_Turn");

    private float targetDirection;
    private bool isRunning;
    private bool turnCheck;
    private Quaternion fromRotation;
    private Quaternion toRotation;
    private float turnDuration = 0.3f;
    private float elapsedTime = 0f;

    public PlayerTurnState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public void SetTurnData(float inputDirection, bool isRunning)
    {
        this.targetDirection = inputDirection;
        this.isRunning = isRunning;
    }

    public override void Enter()
    {
        player.animator.SetBool(IsTurning, true);
        player.rb.velocity = Vector3.zero;
        elapsedTime = 0f;

        fromRotation = player.transform.rotation;
        turnCheck = false;

        if (targetDirection > 0)
            toRotation = Quaternion.Euler(0f, 0f, 0f); 
        else
            toRotation = Quaternion.Euler(0f, 180f, 0f); 

        if (isRunning)
            player.animator.SetTrigger(RunTurnTrigger);
    }

    public override void Update()
    {
        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / turnDuration);

        if (!isRunning)
        {
            player.transform.rotation = Quaternion.Slerp(fromRotation, toRotation, t);

            if (t >= 1f)
            {
                player.animator.SetBool(IsTurning, false);
                stateMachine.ChangeState(player.moveState);
            }
        }
        else
        {
            AnimatorStateInfo stateInfo = player.animator.GetCurrentAnimatorStateInfo(0);
            
            if (!turnCheck && stateInfo.normalizedTime >= 0.15f)
            {
                if (targetDirection > 0f)
                    player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);     
                else if (targetDirection < 0f)
                    player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);    

                turnCheck = true;
            }
            
            if (stateInfo.IsName("Run_Hardturn_180") && stateInfo.normalizedTime >= 0.50f)
            {
                player.transform.rotation = toRotation;
                player.animator.SetBool(IsTurning, false);
                stateMachine.ChangeState(player.moveState);
            }
        }
    }

    public override void Exit()
    {
        player.animator.SetBool(IsTurning, false);
    }
}

public class PlayerJumpState : PlayerBaseState
{
    private bool hasJumped = false;

    public PlayerJumpState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.isGrounded = false;

        float moveInput = Mathf.Abs(Input.GetAxisRaw("Horizontal"));
        string jumpAnim = moveInput > 0.1f ? "Jump_Fwd" : "Jump_in_Place";

        player.animator.Play(jumpAnim);

        player.rb.velocity = new Vector3(player.rb.velocity.x, 0f, player.rb.velocity.z);
        player.rb.AddForce(Vector3.up * player.jumpForce, ForceMode.Impulse);
    }

    public override void Update()
    {
        if (player.rb.velocity.y < -2f && !player.isGrounded)
        {
            stateMachine.ChangeState(player.fallState);
            return;
        }

        if (player.isGrounded)
        {
            stateMachine.ChangeState(player.idleState);
            player.animator.Play("Idle_Walk_Run");
            return;
        }
    }

    public override void Exit()
    {
        
    }
}

public class PlayerFallState : PlayerBaseState
{
    private bool playedExitAnim = false;
    private float groundCheckDistance = 0.25f;
    private float exitAnimDuration = 0.4f;
    private float exitAnimTimer = 0f;

    public PlayerFallState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.animator.Play("Jump_in_Place_Loop");
        playedExitAnim = false;
        exitAnimTimer = 0f;
    }

    public override void Update()
    {
        if (!playedExitAnim && IsNearGround())
        {
            player.animator.Play("Jump_Exit");
            playedExitAnim = true;
        }

        if (playedExitAnim)
        {
            exitAnimTimer += Time.deltaTime;
            if (exitAnimTimer >= exitAnimDuration)
            {
                if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f)
                    stateMachine.ChangeState(player.moveState);
                else
                    stateMachine.ChangeState(player.idleState);
            }
        }
    }

    public override void Exit()
    {
        player.animator.Play("Idle_Walk_Run");
    }
    
    private bool IsNearGround()
    {
        Vector3 origin = player.transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckDistance,
                LayerMask.GetMask("Ground", "Pushable")))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);

            if (angle < 30f)
            {
                return true;
            }
        }

        return false;
    }
}

public class PlayerCrouchToggleState : PlayerBaseState
{
    private static readonly int CrouchSettleTrigger = Animator.StringToHash("Crouch_Settle");
    private static readonly int IsCrouching = Animator.StringToHash("isCrouching");

    private bool goingDown;

    public PlayerCrouchToggleState(PlayerController player, PlayerStateMachine stateMachine, bool goingDown) : base(player, stateMachine)
    {
        this.goingDown = goingDown;
    }

    public override void Enter()
    {
        player.rb.velocity = Vector3.zero;
        player.animator.SetBool(IsCrouching, goingDown);
        player.animator.SetTrigger(CrouchSettleTrigger);
        
        if (goingDown)
            player.SetCrouchCollider(0.5f);
        else
            player.SetStandingCollider(1.2f);
    }

    public override void Update()
    {
        AnimatorStateInfo stateInfo = player.animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("Crouch_Settle") && stateInfo.normalizedTime >= 0.80f)
        {
            stateMachine.ChangeState(player.crouchBlendState);
        }
        
        if (stateInfo.IsName("Crouch_Settle_Reverse") && stateInfo.normalizedTime >= 0.80f)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }
}

public class PlayerCrouchBlendState : PlayerBaseState
{
    private static readonly int CrouchSpeed = Animator.StringToHash("CrouchSpeed");

    public PlayerCrouchBlendState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.SetCrouchCollider();
    }

    public override void Update()
    {
        float inputZ = Input.GetAxisRaw("Horizontal");
        bool isMoving = Mathf.Abs(inputZ) > 0.1f;

        float currentYRotation = player.transform.eulerAngles.y;
        bool isFacingRight = Mathf.DeltaAngle(currentYRotation, 0f) < 90f;
        bool isFacingLeft = Mathf.DeltaAngle(currentYRotation, 180f) < 90f;

        bool inputRight = inputZ > 0f;
        bool inputLeft = inputZ < 0f;

        if ((isFacingRight && inputLeft) || (isFacingLeft && inputRight))
        {
            player.crouchTurnState.SetTurnData(inputZ);
            stateMachine.ChangeState(player.crouchTurnState);
            return;
        }

        float speedValue = isMoving ? 1f : 0f;
        float currentSpeed = player.animator.GetFloat(CrouchSpeed);
        float newSpeed = Mathf.Lerp(currentSpeed, speedValue, Time.deltaTime * 10f);
        player.animator.SetFloat(CrouchSpeed, newSpeed);

        float moveSpeed = player.walkSpeed * 0.5f;
        player.rb.velocity = new Vector3(0f, player.rb.velocity.y, inputZ * moveSpeed);

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (!player.IsHeadBlocked())
            {
                stateMachine.ChangeState(player.crouchExitState);
            }
            else
            {
                Debug.Log("머리 위에 장애물이 있슴다 ~");
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            stateMachine.ChangeState(player.crawlTransitionState);
            return;
        }
    }
}

public class PlayerCrouchTurnState : PlayerBaseState
{
    private float targetDirection;
    private float turnDuration = 0.4f;
    private float elapsedTime;
    private Quaternion fromRotation;
    private Quaternion toRotation;

    public PlayerCrouchTurnState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public void SetTurnData(float inputDirection)
    {
        targetDirection = inputDirection;
    }

    public override void Enter()
    {
        elapsedTime = 0f;
        player.rb.velocity = Vector3.zero;

        fromRotation = player.transform.rotation;
        toRotation = targetDirection > 0f ? Quaternion.Euler(0f, 0f, 0f) : Quaternion.Euler(0f, 180f, 0f);
    }

    public override void Update()
    {
        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / turnDuration);

        player.transform.rotation = Quaternion.Slerp(fromRotation, toRotation, t);

        if (t >= 1f)
        {
            stateMachine.ChangeState(player.crouchBlendState);
        }
    }
}

public class PlayerCrawlTransitionState : PlayerBaseState
{
    private static readonly int ToCrawling = Animator.StringToHash("ToCrawling");

    public PlayerCrawlTransitionState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        player.rb.velocity = Vector3.zero;
        player.animator.SetTrigger(ToCrawling);
        
        player.SetCrawlingCollider(0.5f);
    }

    public override void Update()
    {
        AnimatorStateInfo info = player.animator.GetCurrentAnimatorStateInfo(0);
        if (info.IsName("Crawling"))
        {
            stateMachine.ChangeState(player.crawlBlendState);
        }
    }
}

public class PlayerCrawlBlendState : PlayerBaseState
{
    private static readonly int CrawlSpeed = Animator.StringToHash("CrawlSpeed");
    private static readonly int ToCrouch = Animator.StringToHash("ToCrouch");

    public PlayerCrawlBlendState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        player.SetCrawlingCollider();
        player.rb.velocity = Vector3.zero;
    }

    public override void Update()
    {
        float inputZ = Input.GetAxisRaw("Horizontal");
        float speed = Mathf.Clamp(inputZ, -1f, 1f);

        float yRotation = player.transform.eulerAngles.y;
        if (Mathf.Approximately(yRotation, 180f))
            speed *= -1f;
        
        float current = player.animator.GetFloat(CrawlSpeed);
        float newSpeed = Mathf.Lerp(current, speed, Time.deltaTime * 10f);
        player.animator.SetFloat(CrawlSpeed, newSpeed);

        float crawlSpeed = player.walkSpeed * 0.3f;
        player.rb.velocity = new Vector3(0, player.rb.velocity.y, inputZ * crawlSpeed);

        if (Input.GetKeyDown(KeyCode.X))
        {
            if (!player.IsHeadBlocked())
            {
                player.rb.velocity = Vector3.zero;
                player.animator.SetTrigger(ToCrouch);
                stateMachine.ChangeState(player.crawlExitState);
            }
            else
            {
                Debug.Log("머리 위에 장애물이 있슴다 ~");
            }
            
        }
    }
}

public class PlayerCrawlExitState : PlayerBaseState
{
    public PlayerCrawlExitState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        player.rb.velocity = Vector3.zero;
        
        player.SetCrouchCollider(0.5f);
    }

    public override void Update()
    {
        AnimatorStateInfo info = player.animator.GetCurrentAnimatorStateInfo(0);
        if (info.IsName("Crouch"))
        {
            stateMachine.ChangeState(player.crouchBlendState);
        }
    }
}

public class PlayerPushEnterState : PlayerBaseState
{
    private static readonly int PushEnterTrigger = Animator.StringToHash("Push_Enter");

    public PlayerPushEnterState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.rb.velocity = Vector3.zero;
        player.animator.SetTrigger(PushEnterTrigger);
    }

    public override void Update()
    {
        AnimatorStateInfo info = player.animator.GetCurrentAnimatorStateInfo(0);
        if (info.IsName("Push_Enter") && info.normalizedTime >= 0.9f)
        {
            stateMachine.ChangeState(player.pushBlendState);
        }
    }
}

public class PlayerPushBlendState : PlayerBaseState
{
    private static readonly int PushSpeed = Animator.StringToHash("Push_Speed");

    private PushableBox pushableBoxTarget;

    public PlayerPushBlendState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.rb.velocity = Vector3.zero;

        if (Physics.Raycast(player.transform.position + Vector3.up * 0.5f, player.transform.forward, out RaycastHit hit, 0.5f, LayerMask.GetMask("Pushable")))
        {
            Debug.Log("PushableBox 찾음");
            pushableBoxTarget = hit.collider.GetComponent<PushableBox>();
            Debug.Log(pushableBoxTarget);
        }
    }

    public override void Update()
    {
        AnimatorStateInfo info = player.animator.GetCurrentAnimatorStateInfo(0);

        float inputZ = Input.GetAxisRaw("Horizontal");
        float moveSpeed = player.walkSpeed * 0.5f;

        player.rb.velocity = new Vector3(0f, player.rb.velocity.y, inputZ * moveSpeed);

        float current = player.animator.GetFloat(PushSpeed);
        float target = Mathf.Abs(inputZ) > 0.1f ? 1f : 0f;
        float lerped = Mathf.Lerp(current, target, Time.deltaTime * 10f);
        player.animator.SetFloat(PushSpeed, lerped);

        if (info.IsName("Push") && pushableBoxTarget != null)
        {
            Vector3 toBox = (pushableBoxTarget.transform.position - player.transform.position).normalized;
            float dot = Vector3.Dot(player.transform.forward, toBox);

            if (dot > 0.5f && Mathf.Abs(inputZ) > 0.1f)
            {
                Debug.Log("미는 중");
                Vector3 localMove = new Vector3(0f, 0f, inputZ * moveSpeed * Time.deltaTime);
                Vector3 worldMove = player.transform.TransformDirection(localMove);
                pushableBoxTarget.StartPush(worldMove);
            }
            else
            {
                Debug.Log("멈춤");
                pushableBoxTarget.StopPush();
            }
        }

        if (!player.CheckPushableObject())
        {
            pushableBoxTarget?.StopPush();
            stateMachine.ChangeState(player.pushExitState);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            pushableBoxTarget?.StopPush();
            stateMachine.ChangeState(player.pushExitState);
        }
    }

    public override void Exit()
    {
        pushableBoxTarget?.StopPush();
        pushableBoxTarget = null;
    }
}

public class PlayerPushExitState : PlayerBaseState
{
    private static readonly int PushExitTrigger = Animator.StringToHash("Push_Exit");

    public PlayerPushExitState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.rb.velocity = Vector3.zero;
        player.animator.SetTrigger(PushExitTrigger);
    }

    public override void Update()
    {
        AnimatorStateInfo info = player.animator.GetCurrentAnimatorStateInfo(0);
        if (info.IsName("Push_Exit") && info.normalizedTime >= 0.9f)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }
}

public class PlayerLadderEnterUpState : PlayerBaseState
{
    private static readonly int EnterLadderUp = Animator.StringToHash("EnterLadder_Up");
    private bool entered = false;

    public PlayerLadderEnterUpState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        entered = false;
        player.rb.velocity = Vector3.zero;
        player.rb.useGravity = false;

        player.animator.SetTrigger(EnterLadderUp);
    }

    public override void Update()
    {
        AnimatorStateInfo info = player.animator.GetCurrentAnimatorStateInfo(0);

        if (!entered && info.IsName("Ladder_Enter_Up") && info.normalizedTime >= 0.9f)
        {
            entered = true;
            stateMachine.ChangeState(player.ladderClimbState);
        }
    }

    public override void Exit()
    {
        
    }
}

public class PlayerLadderEnterDownState : PlayerBaseState
{
    private static readonly int EnterLadderFront = Animator.StringToHash("EnterLadder_Front");
    private bool entered = false;

    private Quaternion startRotation;
    private Quaternion targetRotation;
    
    public PlayerLadderEnterDownState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        entered = false;
        player.capsule.enabled = false;
        player.rb.velocity = Vector3.zero;
        player.rb.useGravity = false;

        player.animator.applyRootMotion = true;
        player.animator.SetTrigger(EnterLadderFront);
        
        startRotation = player.transform.rotation;
        targetRotation = Quaternion.LookRotation(-player.currentLadder.forward);
    }

    public override void Update()
    {
        AnimatorStateInfo info = player.animator.GetCurrentAnimatorStateInfo(0);

        float t = info.normalizedTime;
        t = Mathf.Clamp01(t);

        Quaternion interpolatedRotation = Quaternion.Slerp(startRotation, targetRotation, t);
        player.transform.rotation = interpolatedRotation;

        // 종료 조건
        if (!entered && info.IsName("Ladder_Enter_Dn") && info.normalizedTime >= 0.95f)
        {
            entered = true;

            player.animator.applyRootMotion = false;
            stateMachine.ChangeState(player.ladderClimbState);
        }
    }

    public override void Exit()
    {
        player.capsule.enabled = true;
        player.animator.applyRootMotion = false;
    }
}

public class PlayerLadderClimbState : PlayerBaseState
{
    private static readonly int LadderSpeed = Animator.StringToHash("LadderSpeed");

    public float climbSpeed = 1.5f;
    private Transform ladderTransform;
    private float bottomCheckDelay = 4.0f;
    private float stateEnterTime;

    public PlayerLadderClimbState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.rb.useGravity = false;
        player.rb.velocity = Vector3.zero;
        ladderTransform = player.currentLadder;
        stateEnterTime = Time.time;
    }

    public override void Update()
    {
        float inputY = Input.GetAxisRaw("Vertical");

        float targetSpeed = Mathf.Clamp(inputY, -1f, 1f);
        float currentSpeed = player.animator.GetFloat(LadderSpeed);
        float lerped = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);
        player.animator.SetFloat(LadderSpeed, lerped);

        if (Mathf.Abs(inputY) > 0.1f)
        {
            Vector3 move = ladderTransform.up * (inputY * climbSpeed * Time.deltaTime);
            player.rb.MovePosition(player.transform.position + move);
        }
        
        if (Time.time - stateEnterTime > bottomCheckDelay && player.CheckLadderBottom())
        {
            stateMachine.ChangeState(player.ladderExitBottomState);
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            stateMachine.ChangeState(player.ladderExitBottomState);
        }
    }

    public override void Exit()
    {
        player.animator.SetFloat(LadderSpeed, 0f);
    }
}

public class PlayerLadderExitTopState : PlayerBaseState
{
    private bool exited = false;

    public PlayerLadderExitTopState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        exited = false;
        player.rb.velocity = Vector3.zero;

        player.capsule.enabled = false;
        player.animator.applyRootMotion = true;
        player.animator.Play("Ladder_Exit_Dn");
    }

    public override void Update()
    {
        AnimatorStateInfo info = player.animator.GetCurrentAnimatorStateInfo(0);

        if (!exited && info.IsName("Ladder_Exit_Dn") && info.normalizedTime >= 0.9f)
        {
            exited = true;
            stateMachine.ChangeState(player.idleState);
        }
    }

    public override void Exit()
    {
        player.animator.applyRootMotion = false;
        player.capsule.enabled = true;
        player.rb.useGravity = true;
        player.currentLadder = null;
    }
}

public class PlayerLadderExitBottomState : PlayerBaseState
{
    private bool exited = false;

    public PlayerLadderExitBottomState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        exited = false;
        player.rb.velocity = Vector3.zero;

        player.animator.Play("Ladder_Exit_Up");
    }

    public override void Update()
    {
        AnimatorStateInfo info = player.animator.GetCurrentAnimatorStateInfo(0);

        if (!exited && info.IsName("Ladder_Exit_Up") && info.normalizedTime >= 0.9f)
        {
            exited = true;
            stateMachine.ChangeState(player.idleState);
        }
    }

    public override void Exit()
    {
        player.rb.useGravity = true;
        player.currentLadder = null;
    }
}




