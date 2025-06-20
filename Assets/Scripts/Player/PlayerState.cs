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

    protected void PosReset()
    {
        Vector3 pos = player.transform.position;
        pos.x = 0f;
        player.transform.position = pos;
    }
    
    protected void RespawnPlayer()
    {
        PlayerRespawnManager.Instance.RespawnPlayer(player.gameObject);

        stateMachine.ChangeState(player.idleState);
    }
}

public class PlayerIdleState : PlayerBaseState
{
    private static readonly int Speed = Animator.StringToHash("Speed");
    public PlayerIdleState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.SetStandingCollider();
        player.rb.useGravity = true;
        
        player.rb.velocity = new Vector3(0, player.rb.velocity.y, 0);
        player.animator.SetFloat(Speed, 0f);
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
        
        if (!player.isGrounded && player.rb.velocity.y < -1f)
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

    public override void Exit()
    {
        PosReset();
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
        
        if (player.rb.velocity.y < -1f)
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
    
    public override void Exit()
    {
        PosReset();
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
        if (player.rb.velocity.y < -0.4f)
        {
            stateMachine.ChangeState(player.fallState);
            return;
        }

        if (player.isGrounded && player.rb.velocity.y == 0)
        {
            stateMachine.ChangeState(player.idleState);
            player.animator.Play("Idle_Walk_Run");
            return;
        }
        
        if (player.CheckLadderInFront() && Input.GetKeyDown(KeyCode.E))
        {
            player.currentLadder = player.GetLadderInFront();
            stateMachine.ChangeState(player.ladderEnterUpState);
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
    
    private float fallStartY;
    private float fallEndY;
    private float deathFallThreshold = 7f;

    public PlayerFallState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.animator.Play("Jump_in_Place_Loop");
        playedExitAnim = false;
        exitAnimTimer = 0f;
        
        fallStartY = player.transform.position.y;
    }

    public override void Update()
    {
        if (player.IsInWater() && player.isInWater)
        {
            player.lastFallVelocity = player.rb.velocity;
            stateMachine.ChangeState(player.waterImpactState);
            return;
        }
        
        if (IsNearGround())
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
        float offsetY = 0.1f;
        float rayDistance = groundCheckDistance;
        float forwardOffset = 0.1f;

        Vector3 origin = player.transform.position + Vector3.up * offsetY + player.transform.forward * forwardOffset;
        Vector3 direction = Vector3.down;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, rayDistance,
                LayerMask.GetMask("Ground", "Pushable", "Wall")))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);

            if (angle < 30f)
            {
                fallEndY = hit.point.y;

                float fallDistance = fallStartY - fallEndY;

                if (fallDistance >= deathFallThreshold)
                {
                    Debug.Log("높은 곳에서 떨어져 사망!");
                    stateMachine.ChangeState(player.deathState);
                    return false;
                }

                return true;
            }
        }

        return false;
    }
}

public class PlayerDeathState : PlayerBaseState
{
    private static readonly int DeathFwd = Animator.StringToHash("Death_Fwd");
    private float deathTimer = 0f;
    private float respawnDelay = 5f;
    private bool respawned = false;
    public PlayerDeathState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }
    
    public override void Enter()
    {
        if (!player.caughtDie)
        {
            player.animator.SetTrigger(DeathFwd);
            player.SetDeathCollider(0.5f);
        }
        else
        {
            player.capsule.direction = 1;
            player.rb.freezeRotation = false; // 나중에 y는 잠궈주자.
        }
        player.rb.velocity = Vector3.zero;
        deathTimer = 0f;
        respawned = false;
    }

    public override void Update()
    {
        deathTimer += Time.deltaTime;

        if (!respawned && deathTimer >= respawnDelay)
        {
            GameResetEvent.BroadcastPlayerReset();
            RespawnPlayer();
            respawned = true;
        }
    }

    public override void Exit()
    {
        player.transform.localRotation = Quaternion.Euler(0, 0, 0);
        player.rb.freezeRotation = true;
        player.caughtDie = false;
        player.animator.ResetTrigger(DeathFwd);
        player.waterSurfaceY = null;
        player.isInWater = false;
        player.underwaterTime = 0f;
        player.SetStandingCollider(0.5f);
    }
}

public class PlayerCaughtState : PlayerBaseState
{
    public PlayerCaughtState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.animator.Play("Idle");
        player.rb.isKinematic = true;
        player.caughtDie = true;
    }

    public override void Update()
    {
        player.transform.localPosition = new Vector3(0, 0, -0.5f);
        player.transform.localRotation = Quaternion.Euler(-90f, 0f, 180f);
    }

    public override void Exit()
    {
        player.rb.isKinematic = false;
        // player.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
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

        if (stateInfo.IsName("Crouch_Settle"))
        {
            player.skirtPos.transform.localPosition = new Vector3(0.1488715f, 0.12f, 0.01021058f);
            player.skirtPos.transform.localScale = new Vector3(0.9999992f, 1.5f, 1);
            if (stateInfo.normalizedTime >= 0.80f)
            {
                player.crouching = true;
                stateMachine.ChangeState(player.crouchBlendState);
            }
        }
        
        if (stateInfo.IsName("Crouch_Settle_Reverse"))
        {
            player.skirtPos.transform.localPosition = new Vector3(0.1488715f, -0.004006684f, 0.01021058f);
            player.skirtPos.transform.localScale = new Vector3(0.9999992f, 0.9999998f, 1);
            if (stateInfo.normalizedTime >= 0.80f)
            {
                player.crouching = false;
                stateMachine.ChangeState(player.idleState);
            }
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

        float moveSpeed = player.walkSpeed * 0.8f;
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
            player.crawling = true;
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

        float crawlSpeed = player.walkSpeed * 0.6f;
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
            player.crawling = false;
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
    private PushableDoor pushableDoorTarget;
    private bool isVisualRotatingBack = false;

    public PlayerPushBlendState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        player.rb.velocity = Vector3.zero;
        isVisualRotatingBack = false;
        
        player.originalVisualRotation = player.visualRoot.rotation;

        if (Physics.Raycast(player.transform.position + Vector3.up * 0.5f, player.transform.forward, out RaycastHit hit, 0.5f, LayerMask.GetMask("Pushable")))
        {
            pushableBoxTarget = hit.collider.GetComponent<PushableBox>();
            pushableDoorTarget = hit.collider.GetComponent<PushableDoor>();
            
        }
    }

    public override void Update()
    {
        AnimatorStateInfo info = player.animator.GetCurrentAnimatorStateInfo(0);

        float inputZ = Input.GetAxisRaw("Horizontal");
        float moveSpeed = player.walkSpeed * 0.4f;
        
        float yRotation = player.transform.eulerAngles.y;
        bool isFacingRight = Mathf.Approximately(yRotation, 0f);

        player.rb.velocity = new Vector3(0f, player.rb.velocity.y, inputZ * moveSpeed);

        float current = player.animator.GetFloat(PushSpeed);
        float target = Mathf.Abs(inputZ) > 0.1f ? 1f : 0f;
        float lerped = Mathf.Lerp(current, target, Time.deltaTime * 10f);
        player.animator.SetFloat(PushSpeed, lerped);

        if (info.IsName("Push"))
        {
            if (pushableBoxTarget != null)
            {
                if (Mathf.Abs(inputZ) > 0.1f)
                {
                    float direction = player.isFacingRight ? 1f : -1f;
                    Vector3 localMove = new Vector3(0f, 0f, inputZ * direction * moveSpeed * Time.deltaTime);
                    Vector3 worldMove = player.transform.TransformDirection(localMove);

                    pushableBoxTarget.StartPush(worldMove);
                }
                else
                {
                    Debug.Log("여긴가 ?");
                    pushableBoxTarget.StopPush();
                }
            }

            if (pushableDoorTarget != null)
            {
                if (Mathf.Abs(inputZ) > 0.1f)
                {
                    pushableDoorTarget.StartPushRotation(player.transform);
                    
                    if (isFacingRight)
                    {
                        Vector3 doorForward = pushableDoorTarget.transform.forward;
                        doorForward.y = 0f; 
                        Quaternion targetRot = Quaternion.LookRotation(doorForward, Vector3.up);
                        player.visualRoot.rotation = Quaternion.Lerp(player.visualRoot.rotation, targetRot, Time.deltaTime * 4f);
                    }
                    else
                    {
                        Vector3 doorBackward = pushableDoorTarget.transform.forward * -1;
                        doorBackward.y = 0f; 
                        Quaternion targetRot = Quaternion.LookRotation(doorBackward, Vector3.up);
                        player.visualRoot.rotation = Quaternion.Lerp(player.visualRoot.rotation, targetRot, Time.deltaTime * 4f);
                    }
                }
                else
                {
                    pushableDoorTarget.StopPush();
                }
            }
        }
        
        if (pushableDoorTarget != null)
        {
            Vector3 playerDir = player.transform.forward;
            Vector3 doorDir = pushableDoorTarget.transform.forward;

            float angle = Vector3.Angle(playerDir, doorDir);
            Vector3 cross = Vector3.Cross(doorDir, playerDir);
            float direction = Mathf.Sign(cross.y);

            if (Mathf.Abs(angle - 90f) < 1f && Mathf.Abs(direction) > 0.9f)
            {
                pushableBoxTarget?.StopPush();
                pushableDoorTarget?.StopPush();
                stateMachine.ChangeState(player.pushExitState);
            }
        }

        if (!player.CheckPushableObject() && (pushableDoorTarget == null || !pushableDoorTarget.isBeingPushed))
        {
            Debug.Log("아님 여긴가 ?");
            pushableBoxTarget?.StopPush();
            pushableDoorTarget?.StopPush();
            stateMachine.ChangeState(player.pushExitState);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            pushableBoxTarget?.StopPush();
            pushableDoorTarget?.StopPush();
            stateMachine.ChangeState(player.pushExitState);
        }
    }

    public override void Exit()
    {
        pushableBoxTarget?.StopPush();
        pushableDoorTarget?.StopPush();

        pushableBoxTarget = null;
        pushableDoorTarget = null;
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
        Quaternion current = player.visualRoot.rotation;
        Quaternion target = player.originalVisualRotation;

        player.visualRoot.rotation = Quaternion.Lerp(current, target, Time.deltaTime * 5f);

        AnimatorStateInfo info = player.animator.GetCurrentAnimatorStateInfo(0);
        if (Quaternion.Angle(current, target) < 1f && info.IsName("Push_Exit") && info.normalizedTime >= 0.9f)
        {
            player.visualRoot.rotation = target;
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
        player.capsule.enabled = false;
        
        player.animator.Play("Ladder_Enter_Up");
        // player.animator.SetTrigger(EnterLadderUp);
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
        player.capsule.enabled = true;
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

        if (!entered && info.IsName("Ladder_Enter_Dn") && info.normalizedTime >= 0.95f)
        {
            entered = true;

            player.animator.applyRootMotion = false;
            stateMachine.ChangeState(player.ladderClimbState);
        }
    }

    public override void Exit()
    {
        player.animator.ResetTrigger(EnterLadderFront);
        player.transform.rotation = Quaternion.LookRotation(-player.currentLadder.forward);
        player.capsule.enabled = true;
        player.animator.applyRootMotion = false;
    }
}

public class PlayerLadderClimbState : PlayerBaseState
{
    private static readonly int LadderSpeed = Animator.StringToHash("LadderSpeed");

    private float climbSpeed = 1.5f;
    private float inputY;

    public PlayerLadderClimbState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.rb.velocity = Vector3.zero;
        player.rb.useGravity = false;
        player.animator.applyRootMotion = true;
    }

    public override void Update()
    {
        inputY = Input.GetAxisRaw("Vertical");

        float targetSpeed = Mathf.Clamp(inputY, -1f, 1f);
        float currentSpeed = player.animator.GetFloat(LadderSpeed);
        float lerped = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);
        player.animator.SetFloat(LadderSpeed, lerped);
        player.transform.rotation = Quaternion.LookRotation(-player.currentLadder.forward);

        if (inputY < -0.1f && player.CheckLadderBottom())
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
        player.transform.rotation = Quaternion.LookRotation(-player.currentLadder.forward);
        player.animator.applyRootMotion = false;
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
        player.transform.rotation = Quaternion.LookRotation(-player.currentLadder.forward);
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

public class PlayerWaterImpactState : PlayerBaseState
{
    private float impactSpeed;
    private float maxDiveDepth = 2.5f; // 깊게 들어갈수록 현실감 ↑
    private float timer = 0f;
    private float sinkDuration = 0.9f; // 잠기는 시간
    private Vector3 startPos;
    private Vector3 targetPos;

    public PlayerWaterImpactState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        player.rb.useGravity = false;

        impactSpeed = Mathf.Abs(player.lastFallVelocity.y);
        float diveDepth = Mathf.Clamp(impactSpeed * 0.1f, 0.8f, maxDiveDepth);

        startPos = player.transform.position;
        targetPos = startPos - new Vector3(0, diveDepth, 0);

        timer = 0f;

        player.animator.CrossFade("Dive_under", 0.2f);
    }

    public override void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / sinkDuration);

        player.transform.position = Vector3.Lerp(startPos, targetPos, t);

        if (timer >= sinkDuration)
        {
            if (player.waterSurfaceY.HasValue)
            {
                bool isSubmerged = player.transform.position.y < player.waterSurfaceY.Value;
                
                Debug.Log(isSubmerged);
                Debug.Log(player.transform.position.y);
                if (isSubmerged)
                    stateMachine.ChangeState(player.underwaterSwimState);
                else
                    stateMachine.ChangeState(player.swimSurfaceState);
            }
        }
    }

    public override void Exit()
    {
        player.rb.velocity = Vector3.zero;
    }
}

public class PlayerSwimSurfaceState : PlayerBaseState
{
    private static readonly int SwimSpeed = Animator.StringToHash("SwimSpeed");

    private float currentSpeed = 0f;
    private float targetSpeed = 0f;

    private int lastFacingDirection = 1;

    public PlayerSwimSurfaceState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        if (player.drowningParticle != null)
            player.drowningParticle.SetActive(false);
        
        player.rb.velocity = Vector3.zero;
        player.rb.useGravity = false;
        player.underwaterTime = 0f;
        currentSpeed = 0f;
        lastFacingDirection = player.isFacingRight ? 1 : -1;
        player.animator.Play("SwimSurface");
    }

    public override void Update()
    {
        float input = Input.GetAxisRaw("Horizontal");
        targetSpeed = Mathf.Abs(input);
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);
        player.animator.SetFloat(SwimSpeed, currentSpeed);

        Vector3 moveDir = new Vector3(0f, 0f, input);
        player.rb.velocity = moveDir * player.swimSpeed;

        if (input != 0)
        {
            int currentDir = input > 0 ? 1 : -1;
            if (currentDir != lastFacingDirection)
            {
                player.swimTurnState.SetTurnData(currentDir);
                stateMachine.ChangeState(player.swimTurnState);
                return;
            }
        }

        if (Input.GetAxisRaw("Vertical") < -0.1f)
        {
            stateMachine.ChangeState(player.underwaterSwimState);
            return;
        }

        if (!player.IsInWater())
        {
            player.rb.useGravity = true;
            stateMachine.ChangeState(player.idleState);
            return;
        }
    }

    public override void Exit()
    {
        player.animator.SetFloat(SwimSpeed, 0f);
        player.rb.velocity = Vector3.zero;
    }
}

public class PlayerSwimTurnState : PlayerBaseState
{
    private Quaternion fromRotation;
    private Quaternion toRotation;
    private float rotationDuration = 0.3f;
    private float timer = 0f;

    private int targetDirection;

    public PlayerSwimTurnState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public void SetTurnData(int direction)
    {
        targetDirection = direction;
    }

    public override void Enter()
    {
        timer = 0f;
        player.rb.velocity = Vector3.zero;

        fromRotation = player.transform.rotation;
        toRotation = (targetDirection > 0) ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180f, 0);
    }

    public override void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / rotationDuration);

        player.transform.rotation = Quaternion.Slerp(fromRotation, toRotation, t);

        if (t >= 1f)
        {
            player.isFacingRight = targetDirection > 0;
            stateMachine.ChangeState(player.swimSurfaceState);
        }
    }

    public override void Exit()
    {
        
    }
}

public class PlayerUnderwaterSwimState : PlayerBaseState
{
    private static readonly int SwimFwd = Animator.StringToHash("SwimFwd");
    private static readonly int SwimVert = Animator.StringToHash("SwimVert");

    private float horizontalInput;
    private float verticalInput;

    private float currentFwd = 0f;
    private float currentVert = 0f;
    private float blendSmoothTime = 5f;
    
    private const float surfaceBufferDistance = 0.05f;

    public PlayerUnderwaterSwimState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        if (player.drowningParticle != null)
            player.drowningParticle.SetActive(true);
        
        player.rb.useGravity = false;
        
        player.animator.Play("SwimUnderwater");
        player.animator.SetFloat(SwimFwd, 0f);
        player.animator.SetFloat(SwimVert, 0f);
        
        currentFwd = 0f;
        currentVert = 0f;
    }

    public override void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (horizontalInput != 0)
        {
            int currentDir = horizontalInput > 0 ? 1 : -1;
            if ((player.isFacingRight && currentDir == -1) || (!player.isFacingRight && currentDir == 1))
            {
                player.underwaterTurnState.SetTurnData(currentDir);
                stateMachine.ChangeState(player.underwaterTurnState);
                return;
            }
        }

        Vector3 moveDir = new Vector3(0f, verticalInput, horizontalInput).normalized;
        player.rb.velocity = moveDir * player.swimSpeed;

        float targetFwd = Mathf.Abs(horizontalInput);
        float targetVert = verticalInput;

        currentFwd = Mathf.Lerp(currentFwd, targetFwd, Time.deltaTime * blendSmoothTime);
        currentVert = Mathf.Lerp(currentVert, targetVert, Time.deltaTime * blendSmoothTime);

        player.animator.SetFloat(SwimFwd, currentFwd);
        player.animator.SetFloat(SwimVert, currentVert);

        if (IsTouchingWaterSurface() && verticalInput > 0)
        {
            stateMachine.ChangeState(player.swimSurfaceState);
            return;
        }

        if (!player.IsInWater())
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        player.underwaterTime += Time.deltaTime;
        if (player.underwaterTime > player.maxUnderwaterTime)
        {
            stateMachine.ChangeState(player.drowningState);
        }
    }

    public override void Exit()
    {
        player.animator.SetFloat(SwimFwd, 0f);
        player.animator.SetFloat(SwimVert, 0f);
        player.rb.velocity = Vector3.zero;
    }

    private bool IsTouchingWaterSurface()
    {
        return player.waterSurfaceY != null && Mathf.Abs((float)(player.transform.position.y - player.waterSurfaceY)) <= surfaceBufferDistance;
    }
}

public class PlayerUnderwaterTurnState : PlayerBaseState
{
    private Quaternion fromRotation;
    private Quaternion toRotation;
    private float duration = 0.3f;
    private float timer = 0f;

    private int targetDirection;

    public PlayerUnderwaterTurnState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public void SetTurnData(int direction)
    {
        targetDirection = direction;
    }

    public override void Enter()
    {
        timer = 0f;
        player.rb.velocity = Vector3.zero;

        fromRotation = player.transform.rotation;
        toRotation = (targetDirection > 0) ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180f, 0);
    }

    public override void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        player.transform.rotation = Quaternion.Slerp(fromRotation, toRotation, t);

        if (t >= 1f)
        {
            player.isFacingRight = targetDirection > 0;
            stateMachine.ChangeState(player.underwaterSwimState);
        }
    }

    public override void Exit()
    {
        
    }
}

public class PlayerDrowningState : PlayerBaseState
{
    private static readonly int Drowning = Animator.StringToHash("Drowning");
    private float respawnDelay = 5f;
    private float timer = 0f;

    public PlayerDrowningState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        player.rb.velocity = Vector3.zero;
        player.rb.useGravity = false;

        player.animator.SetTrigger(Drowning);
        timer = 0f;
    }

    public override void Update()
    {
        timer += Time.deltaTime;

        if (timer >= respawnDelay)
        {
            RespawnPlayer();
        }
    }

    public override void Exit()
    {
        if (player.drowningParticle != null)
            player.drowningParticle.SetActive(false);
        
        player.waterSurfaceY = null;
        player.isInWater = false;
        player.underwaterTime = 0f;
        
        player.animator.Play("Idle_Walk_Run");
    }
}

