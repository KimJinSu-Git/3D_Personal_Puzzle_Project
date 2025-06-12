using UnityEngine;

public class PushableDoor : MonoBehaviour
{
    public float openAngle = 90f;
    public float openSpeed = 90f;
    public float autoCloseDelay = 5f;
    public bool isBeingPushed = false;
    
    private Quaternion closedRotation;
    private Quaternion openRotation;
    
    private float closeTimer = 0f;

    private void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation;
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
            closeTimer += Time.deltaTime;
            if (closeTimer > autoCloseDelay)
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
        isBeingPushed = true;
        closeTimer = 0f;

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