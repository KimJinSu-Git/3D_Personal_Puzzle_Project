using UnityEngine;

public class PushableDoor : MonoBehaviour
{
    public float openAngle = 90f;
    public float openSpeed = 90f;
    public float autoCloseDelay = 5f;
    public bool isBeingPushed = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    [HideInInspector] public Transform pushingPlayer;

    private float closeTimer = 0f;
    
    public MonoBehaviour[] unlockConditionScripts;
    private IUnlockCondition[] unlockConditions;

    private void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation;
        
        if (unlockConditionScripts != null && unlockConditionScripts.Length > 0)
        {
            unlockConditions = new IUnlockCondition[unlockConditionScripts.Length];
            for (int i = 0; i < unlockConditionScripts.Length; i++)
            {
                unlockConditions[i] = unlockConditionScripts[i] as IUnlockCondition;
            }
        }
    }

    private void Update()
    {
        if (isBeingPushed)
        {
            float angleToTarget = Quaternion.Angle(transform.localRotation, openRotation);

            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation,
                openRotation,
                openSpeed * Time.deltaTime
            );

            if (angleToTarget <= 0.5f)
            {
                transform.localRotation = openRotation;
                isBeingPushed = false;
            }
        }
        else
        {
            if (pushingPlayer != null)
            {
                Animator animator = pushingPlayer.GetComponent<Animator>();

                if (animator != null)
                {
                    AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

                    if (info.IsName("Push"))
                        return;

                    float distance = Vector3.Distance(pushingPlayer.position, transform.position);
                    if (distance > 2f)
                        pushingPlayer = null;
                }
            }

            closeTimer += Time.deltaTime;

            if (closeTimer > autoCloseDelay && pushingPlayer == null)
            {
                float angleToClosed = Quaternion.Angle(transform.localRotation, closedRotation);

                transform.localRotation = Quaternion.RotateTowards(
                    transform.localRotation,
                    closedRotation,
                    openSpeed * Time.deltaTime
                );
            }
        }
    }

    public void StartPushRotation(Transform player)
    {
        if (unlockConditions != null)
        {
            foreach (var condition in unlockConditions)
            {
                if (condition == null || !condition.IsUnlocked())
                {
                    Debug.Log("아직 모든 조건이 만족되지 않아 문을 밀 수 없습니다.");
                    return;
                }
            }
        }
        
        isBeingPushed = true;
        closeTimer = 0f;
        pushingPlayer = player;

        float dot = Vector3.Dot(player.forward, transform.forward);
        float signedAngle = (dot > 0f) ? -openAngle : openAngle;

        closedRotation = Quaternion.Euler(0f, 0f, 0f);
        openRotation = Quaternion.Euler(0f, signedAngle, 0f);
    }

    public void StopPush()
    {
        isBeingPushed = false;
    }
}