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
        float moveX = Input.GetAxisRaw("Horizontal");
        Vector3 velocity = rb.velocity;
        rb.velocity = new Vector3(moveX * moveSpeed, velocity.y, 0);
    
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // 땅에 닿았을 때만 점프 가능
        if (collision.contacts[0].normal.y > 0.5f)
            isGrounded = true;
    }
    
    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}
