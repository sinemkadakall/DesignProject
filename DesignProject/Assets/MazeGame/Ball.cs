using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    // Hareket deðiþkenleri
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;

    // Zýplama kontrolü için deðiþkenler
    private bool isGrounded;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;

    // Component referanslarý
    private Rigidbody rb;

    private void Start()
    {
        // Rigidbody2D component'ini al
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Yerde olup olmadýðýný kontrol et
        isGrounded = Physics2D.OverlapCircle(transform.position, groundCheckRadius, groundLayer);

        // Yatay hareket için input al
        float moveInput = Input.GetAxisRaw("Horizontal");

        // Yatay hareketi uygula
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

        // Zýplama kontrolü
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    private void Jump()
    {
        // Zýplama kuvvetini uygula
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    // Zemini kontrol etmek için gizmo çiz (Editor'de görünür)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, groundCheckRadius);
    }




}
