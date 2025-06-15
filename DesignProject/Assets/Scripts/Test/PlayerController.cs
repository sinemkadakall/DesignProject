using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Hareket Ayarlar�")]
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float rotationSpeed = 500f;

    [Header("Zemin Kontrol�")]
    [SerializeField] float groundCheckRadius = 0.2f;
    [SerializeField] Vector3 gorundCheckOffset;
    [SerializeField] LayerMask groundLayer;

    [Header("Kamera Ayarlar�")]
    [SerializeField] bool useRelativeMovement = false; // Kameraya g�re hareket etsin mi?

    bool isGrounded;
    float ySpeed;
    Quaternion targetRotation;
    CameraController cameraController;
    Animator animator;
    CharacterController characterController;

    private void Awake()
    {
        cameraController = Camera.main.GetComponent<CameraController>();
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Oyuncudan yatay ve dikey girdi al�yor
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Hareket miktar�n� hesaplamak i�in
        float moveAmount = Mathf.Clamp01(Mathf.Abs(h) + Mathf.Abs(v));

        // Hareket girdisini normalize et
        var moveInput = (new Vector3(h, 0, v)).normalized;

        Vector3 moveDir;

        if (useRelativeMovement && cameraController != null)
        {
            // Kamera rotasyonuna g�re hareket y�n�n� ayarla
            moveDir = cameraController.PlanarRotation * moveInput;
        }
        else
        {
            // D�nya koordinatlar�na g�re hareket (WASD = Kuzey/G�ney/Do�u/Bat�)
            moveDir = moveInput;
        }

        // Zemin kontrol�
        GroundCheck();
        if (isGrounded)
        {
            ySpeed = -0.5f;
        }
        else
        {
            ySpeed += Physics.gravity.y * Time.deltaTime;
        }

        // H�z� hesaplamak i�in
        var velocity = moveDir * moveSpeed;
        velocity.y = ySpeed;

        // Karakteri hareket ettir
        characterController.Move(velocity * Time.deltaTime);

        if (moveAmount > 0)
        {
            // Karakterin y�z� hareket y�n�ne do�ru y�nelir
            targetRotation = Quaternion.LookRotation(moveDir);
        }

        // Karakteri yava��a hedef noktas�na d�nd�rmek i�in
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        animator.SetFloat("moveAmount", moveAmount, 0.2f, Time.deltaTime);
    }

    private void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(transform.TransformPoint(gorundCheckOffset), groundCheckRadius, groundLayer);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0.5f);
        Gizmos.DrawSphere(transform.TransformPoint(gorundCheckOffset), groundCheckRadius);
    }

}
