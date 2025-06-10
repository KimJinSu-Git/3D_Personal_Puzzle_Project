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
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            // Idle or Move 상태에서 숙이기 진입
            stateMachine.ChangeState(new PlayerCrouchToggleState(player, stateMachine, true));
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
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            stateMachine.ChangeState(new PlayerCrouchToggleState(player, stateMachine, true));
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
        if (player.isGrounded)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }

    public override void Exit()
    {
        player.animator.Play("Idle_Walk_Run");
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
        // 방향에 따라 Bool 상태 설정
        player.animator.SetBool(IsCrouching, goingDown);
        // 공통 Trigger 발동 (Crouch_Settle 애니메이션)
        player.animator.SetTrigger(CrouchSettleTrigger);
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

    private float previousDirection = 0f;

    public PlayerCrouchBlendState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        previousDirection = Mathf.Sign(Input.GetAxisRaw("Horizontal"));
    }

    public override void Update()
    {
        float inputZ = Input.GetAxisRaw("Horizontal");
        bool isMoving = Mathf.Abs(inputZ) > 0.1f;

        // 턴 감지
        float currentDirection = Mathf.Sign(inputZ);
        if (isMoving && currentDirection != previousDirection && previousDirection != 0f)
        {
            player.crouchTurnState.SetTurnData(currentDirection);
            stateMachine.ChangeState(player.crouchTurnState);
            return;
        }
        previousDirection = currentDirection;

        // 속도 적용
        float speedValue = isMoving ? 1f : 0f;
        float currentSpeed = player.animator.GetFloat(CrouchSpeed);
        float newSpeed = Mathf.Lerp(currentSpeed, speedValue, Time.deltaTime * 10f);
        player.animator.SetFloat(CrouchSpeed, newSpeed);

        // 이동 제한 + 속도 적용
        float moveSpeed = player.walkSpeed * 0.5f;
        player.rb.velocity = new Vector3(0f, player.rb.velocity.y, inputZ * moveSpeed);

        // C키로 해제
        if (Input.GetKeyDown(KeyCode.C))
        {
            stateMachine.ChangeState(new PlayerCrouchToggleState(player, stateMachine, false));
            return;
        }

        // X키로 크롤링 진입
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
    private float turnDuration = 0.6f;
    private float elapsedTime;

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
        player.animator.Play("Crouch_Turn_180");
    }

    public override void Update()
    {
        elapsedTime += Time.deltaTime;
        if (elapsedTime >= turnDuration)
        {
            if (targetDirection > 0f)
                player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            else
                player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            stateMachine.ChangeState(player.crouchBlendState);
        }
    }
}

public class PlayerCrawlTransitionState : PlayerBaseState
{
    public PlayerCrawlTransitionState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        player.rb.velocity = Vector3.zero;
        player.animator.Play("Crouch_Crawling");
    }

    public override void Update()
    {
        AnimatorStateInfo stateInfo = player.animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("Crouch_Crawling") && stateInfo.normalizedTime >= 0.95f)
        {
            // 이후 CrawlState로 전환할 예정이면 여기에 구현
            // 예: stateMachine.ChangeState(player.crawlMoveState);
        }
    }
}



