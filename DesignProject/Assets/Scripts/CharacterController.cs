using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    private Animator animator;
    private float horizontalInput;
    private float verticalInput;

    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;

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
        // Yatay ve dikey input deðerlerini al
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Hareket yönünü hesapla
        moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        // Koþma kontrolü (X tuþu ile)
        isRunning = Input.GetKey(KeyCode.X );

        // Hareket var mý yok mu kontrolü
        isWalking = moveDirection.magnitude > 0.1f;
    }

    private void UpdateAnimationStates()
    {
        // Animator parametrelerini güncelle
        animator.SetBool(IS_WALKING, isWalking);
        animator.SetBool(IS_RUNNING, isRunning);
    }

    private void MoveCharacter()
    {
        if (moveDirection.magnitude > 0.1f)
        {
            float currentSpeed = isRunning ? runSpeed : walkSpeed;

            // Kamera yönüne göre hareket
            Vector3 movement = Camera.main.transform.TransformDirection(moveDirection);
            movement.y = 0; // Y eksenini sýfýrla
            movement = movement.normalized * currentSpeed * Time.fixedDeltaTime;

            // Pozisyonu doðrudan güncelle
            rb.MovePosition(rb.position + movement);
        }
    }

    private void HandleRotation()
    {
        if (moveDirection != Vector3.zero)
        {
            // Kamera yönüne göre dönüþ
            Vector3 positionToLook = transform.position + Camera.main.transform.TransformDirection(moveDirection);
            transform.LookAt(positionToLook);
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0); // Sadece Y ekseninde dönüþ
        }
    }
}
