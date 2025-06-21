using UnityEngine;
using Cinemachine;
using System.Collections;

public class CameraDetachZone : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform playerpivotTransform;
    [SerializeField] private Transform playerpivot2Transform;

    private bool isPlayerInside = false;
    private Coroutine rotationCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInside = true;
        virtualCamera.Follow = playerpivot2Transform;

        if (rotationCoroutine != null)
            StopCoroutine(rotationCoroutine);

        rotationCoroutine = StartCoroutine(RotateAwayFromCenter());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (isPlayerInside)
        {
            if (rotationCoroutine != null)
                StopCoroutine(rotationCoroutine);

            rotationCoroutine = StartCoroutine(RotateBackAndFollow());
            isPlayerInside = false;
        }
    }

    private IEnumerator RotateAwayFromCenter()
    {
        float baseY = 270f;
        float playerY = playerTransform.eulerAngles.y;

        // 기준 회전
        float peakY = (Mathf.Abs(Mathf.DeltaAngle(playerY, 0f)) < 45f) ? 290f : 250f;
        float duration = 0.4f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float yRot = Mathf.LerpAngle(baseY, peakY, t);

            Vector3 euler = virtualCamera.transform.eulerAngles;
            euler.y = yRot;
            virtualCamera.transform.eulerAngles = euler;

            yield return null;
        }

        // 회전 끝 상태 유지 (Follow는 여전히 null)
        Vector3 finalEuler = virtualCamera.transform.eulerAngles;
        finalEuler.y = peakY;
        virtualCamera.transform.eulerAngles = finalEuler;

        rotationCoroutine = null;
    }

    private IEnumerator RotateBackAndFollow()
    {
        virtualCamera.Follow = playerpivotTransform;

        float currentY = virtualCamera.transform.eulerAngles.y;
        float targetY = 270f;
        float duration = 0.7f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float yRot = Mathf.LerpAngle(currentY, targetY, t);

            Vector3 euler = virtualCamera.transform.eulerAngles;
            euler.y = yRot;
            virtualCamera.transform.eulerAngles = euler;

            yield return null;
        }

        Vector3 finalEuler = virtualCamera.transform.eulerAngles;
        finalEuler.y = targetY;
        virtualCamera.transform.eulerAngles = finalEuler;

        rotationCoroutine = null;
    }
}