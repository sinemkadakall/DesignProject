using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //Bu kod sayesinde karakter y�r�me,d�nme, yer �ekimine ba�l� d��me
    //gibi temel haraktelerini yapmas�n� sa�lar

    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float rotationSpeed = 500f;

    [SerializeField] float groundCheckRadius = 0.2f;
    [SerializeField] Vector3 gorundCheckOffset;
    [SerializeField] LayerMask groundLayer;

    bool isGrounded;

    //Yer�ekimi etkisi bu h�zdan kontrol edilir
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

        //Oyuncudan yatay ve dikey girdi al�yor.
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        //Hareket miktar�n� hesaplamak i�in
        float moveAmount = Mathf.Clamp01(Mathf.Abs(h) + Mathf.Abs(v));

        //Hareket girdisini normalize et
        var moveInput = (new Vector3(h, 0, v)).normalized;

        //Kamera rotasyonuna g�re hareket y�n�n� ayarla
        var moveDir=cameraController.PlanarRotation * moveInput;

        //Zemin kontrol�
        GroundCheck();

        if (isGrounded)
        {
            ySpeed = -0.5f;
        }
        else
        {
            ySpeed += Physics.gravity.y * Time.deltaTime;
        }

        //H�z� hesaplamak i�in
        var velocity = moveDir * moveSpeed;
        velocity.y = ySpeed;

        //Karakteri hesaplamak i�in
        characterController.Move(velocity * Time.deltaTime);

        if (moveAmount > 0)
        {
            //Karakterin y�z� hareket y�n�ne do�ru y�nelir
            targetRotation= Quaternion.LookRotation(moveDir);
        }

        //Karakteri yava��a hedef noktas�na d�nd�rmek i�in
        transform.rotation = Quaternion.RotateTowards( transform.rotation, targetRotation,rotationSpeed * Time.deltaTime);


        animator.SetFloat("moveAmount",moveAmount,0.2f,Time.deltaTime);

    }


    private void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(transform.TransformPoint(gorundCheckOffset),groundCheckRadius,groundLayer);


    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0,1,0.5f);
        Gizmos.DrawSphere(transform.TransformPoint(gorundCheckOffset), groundCheckRadius);
    }






}
