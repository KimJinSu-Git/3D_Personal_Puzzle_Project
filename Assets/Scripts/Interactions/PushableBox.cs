using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PushableBox : MonoBehaviour
{
    private Rigidbody rb;
    private bool isBeingPushed = false;
    private Vector3 moveDirection = Vector3.zero;

    [Header("Mass Settings")]
    public float defaultMass = 100f;  // 평소에는 밀리지 않게 무겁게
    public float pushableMass = 1f;   // 밀기 상태일 때만 가볍게

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = defaultMass;
    }

    private void FixedUpdate()
    {
        if (isBeingPushed && moveDirection != Vector3.zero)
        {
            rb.MovePosition(rb.position + moveDirection);
        }
    }

    public void StartPush(Vector3 direction)
    {
        Debug.Log("StartPush");
        if (!isBeingPushed)
        {
            isBeingPushed = true;
            rb.mass = pushableMass;
        }

        moveDirection = direction;
    }

    public void StopPush()
    {
        Debug.Log("StopPush");
        if (isBeingPushed)
        {
            isBeingPushed = false;
            rb.mass = defaultMass;
        }

        moveDirection = Vector3.zero;
    }
}
