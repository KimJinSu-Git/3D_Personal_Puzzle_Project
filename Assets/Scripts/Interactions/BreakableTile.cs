using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableTile : MonoBehaviour
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("FallingBox"))
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            // 파티클, 사운드, 임펄스 등 추가 연출도 여기서 실행
        }
    }
}