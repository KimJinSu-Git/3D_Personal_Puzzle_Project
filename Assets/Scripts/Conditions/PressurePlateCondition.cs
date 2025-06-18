using UnityEngine;

public class PressurePlateCondition : MonoBehaviour, IUnlockCondition
{
    private bool isPressed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Pushable"))
        {
            isPressed = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Pushable"))
        {
            isPressed = false;
        }
    }

    public bool IsUnlocked()
    {
        return isPressed;
    }
}