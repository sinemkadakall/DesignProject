using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Hareket Ayarlarý")]
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float rotationSpeed = 500f;

    [Header("Zemin Kontrolü")]
    [SerializeField] float groundCheckRadius = 0.2f;
    [SerializeField] Vector3 gorundCheckOffset;
    [SerializeField] LayerMask groundLayer;

    [Header("Kamera Ayarlarý")]
    [SerializeField] bool useRelativeMovement = false; // Kameraya göre hareket etsin mi?

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
        // Oyuncudan yatay ve dikey girdi alýyor
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Hareket miktarýný hesaplamak için
        float moveAmount = Mathf.Clamp01(Mathf.Abs(h) + Mathf.Abs(v));

        // Hareket girdisini normalize et
        var moveInput = (new Vector3(h, 0, v)).normalized;

        Vector3 moveDir;

        if (useRelativeMovement && cameraController != null)
        {
            // Kamera rotasyonuna göre hareket yönünü ayarla
            moveDir = cameraController.PlanarRotation * moveInput;
        }
        else
        {
            // Dünya koordinatlarýna göre hareket (WASD = Kuzey/Güney/Doðu/Batý)
            moveDir = moveInput;
        }

        // Zemin kontrolü
        GroundCheck();
        if (isGrounded)
        {
            ySpeed = -0.5f;
        }
        else
        {
            ySpeed += Physics.gravity.y * Time.deltaTime;
        }

        // Hýzý hesaplamak için
        var velocity = moveDir * moveSpeed;
        velocity.y = ySpeed;

        // Karakteri hareket ettir
        characterController.Move(velocity * Time.deltaTime);

        if (moveAmount > 0)
        {
            // Karakterin yüzü hareket yönüne doðru yönelir
            targetRotation = Quaternion.LookRotation(moveDir);
        }

        // Karakteri yavaþça hedef noktasýna döndürmek için
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
