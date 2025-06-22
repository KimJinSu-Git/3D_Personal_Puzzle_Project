using System.Collections;
using UnityEngine;

public class LeverController : MonoBehaviour
{
    [SerializeField] private Transform leverHandle;
    [SerializeField] private float rotateDuration = 0.5f;

    [SerializeField] private MovingBox movingBox;

    private Coroutine rotateCoroutine;
    private bool isRotating = false;

    private bool isLeverUp = true;

    public bool IsLeverUp => isLeverUp;
    public bool IsRotating() => isRotating;

    public void StartLeverRotation()
    {
        if (rotateCoroutine != null)
            StopCoroutine(rotateCoroutine);

        float targetAngle = isLeverUp ? 60f : -60f;
        rotateCoroutine = StartCoroutine(RotateLeverTo(targetAngle));
    }

    private IEnumerator RotateLeverTo(float targetXAngle)
    {
        isRotating = true;
        
        if (isLeverUp)
            yield return new WaitForSeconds(0.8f);
        else
            yield return new WaitForSeconds(0.7f); 

        Quaternion startRot = leverHandle.localRotation;
        Quaternion targetRot = Quaternion.Euler(targetXAngle, 0, 0);

        float elapsed = 0f;
        while (elapsed < rotateDuration)
        {
            leverHandle.localRotation = Quaternion.Slerp(startRot, targetRot, elapsed / rotateDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        leverHandle.localRotation = targetRot;
        isRotating = false;

        isLeverUp = !isLeverUp;

        if (isLeverUp)
            movingBox.MoveDown();
        else
            movingBox.MoveUp();   
    }
}