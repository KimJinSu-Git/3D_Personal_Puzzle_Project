using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PushableBox : MonoBehaviour
{
    private Rigidbody rb;
    private bool isBeingPushed = false;
    private Vector3 moveDirection = Vector3.zero;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
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
        isBeingPushed = true;
        moveDirection = direction;
    }

    public void StopPush()
    {
        isBeingPushed = false;
        moveDirection = Vector3.zero;
    }
}
