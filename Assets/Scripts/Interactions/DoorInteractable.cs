using System.Collections;
using UnityEngine;

public class DoorInteractable : MonoBehaviour
{
    public enum InteractionType
    {
        Default,
        InsideOpen,
        InsideClose,
        OutsideOpen,
        OutsideClose
    }

    public InteractionType interactionType;

    public Transform doorMesh;
    public float openAngle = 90f;
    public float openDuration = 0.9f;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private bool isOpen = false;
    private bool isAnimating = false;

    private void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = Quaternion.Euler(0f, openAngle, 0f) * closedRotation;
    }

    public void Interact()
    {
        if (isAnimating) return;

        if (!isOpen)
            StartCoroutine(AnimateDoor(open: true));
        else
            StartCoroutine(AnimateDoor(open: false));
    }

    private IEnumerator AnimateDoor(bool open)
    {
        isAnimating = true;
        float elapsed = 0f;

        Quaternion from = transform.localRotation;
        Quaternion to = open ? openRotation : closedRotation;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            transform.localRotation = Quaternion.Slerp(from, to, t);
            yield return null;
        }
        
        
        ToggleState(); 
        transform.localRotation = to;
        isOpen = open;
        isAnimating = false;
    }

    public void ToggleState()
    {
        switch (interactionType)
        {
            case InteractionType.InsideOpen:
                interactionType = InteractionType.InsideClose;
                break;
            case InteractionType.InsideClose:
                interactionType = InteractionType.InsideOpen;
                break;
            case InteractionType.OutsideOpen:
                interactionType = InteractionType.OutsideClose;
                break;
            case InteractionType.OutsideClose:
                interactionType = InteractionType.OutsideOpen;
                break;
        }
    }
}