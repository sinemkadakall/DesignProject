using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    private Animator animator;
    private float horizontalInput;
    private float verticalInput;

   // [SerializeField] private float walkSpeed = 5f;
   // [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float moveSpeed = 0.4f;

    private bool isWalking = false;
    private bool isRunning = false;

    private Vector3 moveDirection;
    private Rigidbody rb;

    // Animator parameter isimleri
    private readonly string IS_WALKING = "isWalking";
    private readonly string IS_RUNNING = "isRunning";

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        GetInputs();
        UpdateAnimationStates();
        HandleRotation();
    }

    private void FixedUpdate()
    {
        MoveCharacter();
    }

    private void GetInputs()
    {
        // Yatay ve dikey input de�erlerini al
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Hareket y�n�n� hesapla
        moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        // Ko�ma kontrol� (Left Shift tu�u ile)
        isRunning = Input.GetKey(KeyCode.X);

        // Hareket var m� yok mu kontrol�
        isWalking = moveDirection.magnitude > 0.1f;
    }

    private void UpdateAnimationStates()
    {
        // Animator parametrelerini g�ncelle
        animator.SetBool(IS_WALKING, isWalking);
        animator.SetBool(IS_RUNNING, isRunning);
    }

    private void MoveCharacter()
    {
        if (moveDirection.magnitude > 0.1f)
        {
            // Hareket h�z�n� belirle
           // float currentSpeed = isRunning ? runSpeed : walkSpeed;

            // Karakteri hareket ettir
            Vector3 movement = moveDirection * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movement);
        }
    }

    private void HandleRotation()
    {
        if (moveDirection != Vector3.zero)
        {
            // Hareket y�n�ne do�ru d�nd�r
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, 720f * Time.deltaTime);
        }
    }
}
