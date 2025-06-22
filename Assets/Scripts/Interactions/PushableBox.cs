using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PushableBox : MonoBehaviour
{
    [Header("Mass Settings")]
    public float defaultMass = 100f; 
    public float pushableMass = 1f;
    
    public bool isBeingPushed = false;
    
    [SerializeField] private string fallingTag = "FallingBox";
    [SerializeField] private float fallingThreshold = -1.5f;
    
    private bool isFalling = false;
    private Vector3 tempPos;
    
    private Rigidbody rb;
    private Vector3 moveDirection = Vector3.zero;
    
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
        
        if (!isFalling && rb.velocity.y < fallingThreshold)
        {
            isFalling = true;
            gameObject.tag = fallingTag;
        }
        else if (isFalling && Mathf.Abs(rb.velocity.y) < 0.01f && rb.IsSleeping())
        {
            isFalling = false;
            gameObject.tag = "Untagged";
        }
    }

    public void StartPush(Vector3 direction)
    {
        if (!isBeingPushed)
        {
            isBeingPushed = true;
            rb.mass = pushableMass;
        }
        
        tempPos = transform.position;
        tempPos.x = 0;
        transform.position = tempPos;

        moveDirection = direction/4;
    }

    public void StopPush()
    {
        if (isBeingPushed)
        {
            isBeingPushed = false;
            rb.mass = defaultMass;
        }

        moveDirection = Vector3.zero;
    }
}
