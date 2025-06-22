using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineImpulseListener))]
public class CameraShakeReceiver : MonoBehaviour
{
    private void Reset()
    {
        // Listener가 없다면 자동 추가
        if (!TryGetComponent(out CinemachineImpulseListener listener))
        {
            gameObject.AddComponent<CinemachineImpulseListener>();
        }

        Debug.Log("<color=cyan>[CameraShakeReceiver]</color> ImpulseListener attached.");
    }
}