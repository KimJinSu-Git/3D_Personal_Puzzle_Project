using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f; 
    public float jumpForce = 6f;
    private Rigidbody rb;
    private bool isGrounded;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    private void Update()
    {
        float moveZ = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y, moveZ * moveSpeed);
    
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts[0].normal.y > 0.5f)
            isGrounded = true;
    }
    
    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}
