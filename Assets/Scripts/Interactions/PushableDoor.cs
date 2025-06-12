using UnityEngine;

public class PushableDoor : MonoBehaviour
{
    public float openAngle = 90f;
    public float openSpeed = 90f;
    public float autoCloseDelay = 5f;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private bool isBeingPushed = false;
    private float closeTimer = 0f;

    private void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation; // 초기화만
    }

    private void Update()
    {
        if (isBeingPushed)
        {
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, openRotation, openSpeed * Time.deltaTime);

            if (Quaternion.Angle(transform.localRotation, openRotation) < 1f)
            {
                isBeingPushed = false;
            }
        }
        else
        {
            closeTimer += Time.deltaTime;
            if (closeTimer > autoCloseDelay)
            {
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation, closedRotation, openSpeed * Time.deltaTime);
            }
        }
    }

    public void StartPushRotation(Transform player)
    {
        isBeingPushed = true;
        closeTimer = 0f;

        float dot = Vector3.Dot(player.forward, transform.forward); // 밀고 있는 방향과 문 방향의 유사도
        float signedAngle = (dot > 0f) ? -openAngle : openAngle;

        // 기준 각도는 항상 0도
        closedRotation = Quaternion.Euler(0f, 0f, 0f);
        openRotation = Quaternion.Euler(0f, signedAngle, 0f);

        Debug.Log($"🧭 dot: {dot}, 열릴 방향: {(dot > 0f ? "-90도" : "90도")}");
    }

    public void StopPush()
    {
        isBeingPushed = false;
    }
}