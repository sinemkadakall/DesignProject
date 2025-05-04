using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    // Hareket de�i�kenleri
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;

    // Z�plama kontrol� i�in de�i�kenler
    private bool isGrounded;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;

    // Component referanslar�
    private Rigidbody rb;

    private void Start()
    {
        // Rigidbody2D component'ini al
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Yerde olup olmad���n� kontrol et
        isGrounded = Physics2D.OverlapCircle(transform.position, groundCheckRadius, groundLayer);

        // Yatay hareket i�in input al
        float moveInput = Input.GetAxisRaw("Horizontal");

        // Yatay hareketi uygula
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

        // Z�plama kontrol�
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    private void Jump()
    {
        // Z�plama kuvvetini uygula
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    // Zemini kontrol etmek i�in gizmo �iz (Editor'de g�r�n�r)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, groundCheckRadius);
    }




}
