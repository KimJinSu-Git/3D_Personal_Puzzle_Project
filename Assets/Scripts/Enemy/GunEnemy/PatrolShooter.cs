using System;
using System.Collections;
using UnityEngine;

public class PatrolShooter : MonoBehaviour
{
    public enum GunnerState { Patrol, Shooting }
    
    [Header("이동 설정")]
    public float walkSpeed = 2f;
    public float patrolZMin = 100f;
    public float patrolZMax = 120f;
    public float rotationSpeed = 5f;
    
    [Header("감지 설정")]
    public SpotlightShooterDetector spotlightSensor;
    
    [Header("애니메이션")]
    public Animator animator;

    private GunnerState currentState = GunnerState.Patrol;
    private Vector3 moveDirection = Vector3.forward;
    private float cooldownTimer = 0f;
    private bool isFacingRight = true;

    private bool isShoot = false;
    private AnimatorStateInfo animatorStateInfo;

    private void Start()
    {
        animator.Play("Walk");
    }

    private void Update()
    {
        animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        switch (currentState)
        {
            case GunnerState.Patrol:
                Patrol();
                break;
            case GunnerState.Shooting:
                break;
        }
    }

    public void OnPlayerDetected(PlayerController player)
    {
        if (isShoot) return;
        ChangeState(GunnerState.Shooting);
        StartCoroutine(ShootSequence(player));
    }
    
    private IEnumerator ShootSequence(PlayerController player)
    {
        isShoot = true;
        
        animator.Play("Idle");
        
        Vector3 targetDir = (player.transform.position - transform.position).normalized;
        Quaternion targetRot = Quaternion.LookRotation(new Vector3(targetDir.x, 0, targetDir.z));
        while (Quaternion.Angle(transform.rotation, targetRot) > 1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            yield return null;
        }
        
        animator.Play("Aim_Enter");
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);

        animator.Play("Aim_Shoot");
        SoundManager.Instance.PlaySFX("HandGun_Shoot");
        player.gunDrowning = true;
        player.stateMachine.ChangeState(player.drowningState);
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);

        animator.Play("Aim_Exit");
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);

        animator.Play("Idle");
    }
    
    
    private void Patrol()
    {
        transform.Translate(moveDirection * (walkSpeed * Time.deltaTime), Space.World);

        float z = transform.position.z;
        if (z <= patrolZMin || z >= patrolZMax)
        {
            moveDirection = -moveDirection;
            StartCoroutine(RotateSmoothly());
        }
    }

    private IEnumerator RotateSmoothly()
    {
        float targetY = isFacingRight ? 180f : 0f;
        isFacingRight = !isFacingRight;

        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.Euler(0, targetY, 0);

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * rotationSpeed;
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
    }
    
    private void ChangeState(GunnerState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case GunnerState.Patrol:
                animator.Play("Walk");
                break;
        }
    }

    public void ResetEnemy()
    {
        ChangeState(GunnerState.Patrol);
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