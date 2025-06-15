 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    [Header("Takip Ayarlar�")]
    [SerializeField] Transform followTarget;
    [SerializeField] float distance = 5f;
    [SerializeField] Vector3 offset = new Vector3(0, 2, 0); // Karakterin arkas�nda ve yukar�s�nda

    [Header("Collision Detection")]
    [SerializeField] LayerMask obstacleLayer = -1; // Hangi layerlar� obstacle olarak g�rs�n
    [SerializeField] float collisionRadius = 0.3f;
    [SerializeField] float minDistance = 2f; // Minimum kamera mesafesi (art�r�ld�)
    [SerializeField] float characterBuffer = 0.5f; // Karaktere olan minimum mesafe

    [Header("Hareket Ayarlar�")]
    [SerializeField] float followSpeed = 5f; // Kameran�n takip h�z�
    [SerializeField] bool smoothFollow = true;

    [Header("Kamera A��s�")]
    [SerializeField] bool fixedAngle = true; // Sabit a�� i�in
    [SerializeField] Vector3 fixedRotation = new Vector3(15, 0, 0); // Sabit bak�� a��s�

    private Vector3 currentVelocity;
    private float currentDistance;

    private void Start()
    {
        currentDistance = distance;

        // E�er sabit a�� kullan�yorsak fare kontrol�n� kapat
        if (fixedAngle)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void LateUpdate()
    {
        if (followTarget == null) return;

        // Hedef pozisyonu hesapla
        Vector3 targetPosition = followTarget.position + offset;

        if (fixedAngle)
        {
            // Sabit a�� modu
            UpdateFixedAngleCamera(targetPosition);
        }
        else
        {
            // Orijinal mouse kontroll� mod (iste�e ba�l�)
            UpdateMouseControlledCamera(targetPosition);
        }
    }

    private void UpdateFixedAngleCamera(Vector3 targetPosition)
    {
        // Kamera pozisyonunu hesapla
        Vector3 direction = -transform.forward; // Kameran�n bakt��� y�n�n tersi

        // Sabit a�� kullan�yorsak, direction'� yeniden hesapla
        Quaternion rotation = Quaternion.Euler(fixedRotation);
        direction = rotation * Vector3.back; // Vector3.back = (0,0,-1)

        Vector3 desiredPosition = targetPosition + direction * currentDistance;

        // Collision detection
        desiredPosition = HandleCollision(targetPosition, desiredPosition);

        // Kamera pozisyonunu ayarla
        if (smoothFollow)
        {
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / followSpeed);
        }
        else
        {
            transform.position = desiredPosition;
        }

        // Kameran�n karaktere bakmas�n� sa�la
        transform.LookAt(targetPosition);
    }

    private void UpdateMouseControlledCamera(Vector3 targetPosition)
    {
        // Orijinal mouse kontroll� kodunuz (iste�e ba�l�)
        // Bu k�s�m eski kodunuzdan al�nm��t�r, e�er mouse kontrol� de istiyorsan�z
    }

    private Vector3 HandleCollision(Vector3 targetPosition, Vector3 desiredPosition)
    {
        Vector3 direction = (desiredPosition - targetPosition).normalized;
        float desiredDistance = Vector3.Distance(targetPosition, desiredPosition);

        // �lk olarak, karakterin kendisi ile collision kontrol�
        Collider playerCollider = followTarget.GetComponent<Collider>();
        if (playerCollider == null)
            playerCollider = followTarget.GetComponent<CharacterController>();

        // Karakterin b�y�kl���n� hesapla
        float playerRadius = 0.5f; // Varsay�lan de�er
        if (playerCollider != null)
        {
            playerRadius = Mathf.Max(playerCollider.bounds.size.x, playerCollider.bounds.size.z) * 0.6f;
        }

        // Minimum mesafeyi karakterin b�y�kl���ne g�re ayarla
        float dynamicMinDistance = Mathf.Max(minDistance, playerRadius + 0.5f);

        // E�er kamera �ok yak�nsa, zorla uzakla�t�r
        if (desiredDistance < dynamicMinDistance)
        {
            currentDistance = dynamicMinDistance;
            return targetPosition + direction * dynamicMinDistance;
        }

        // Raycast ile collision kontrol� (birden fazla kontrol)
        RaycastHit hit;

        // Ana SphereCast
        if (Physics.SphereCast(targetPosition, collisionRadius, direction, out hit, desiredDistance, obstacleLayer))
        {
            // Collision varsa, kameray� g�venli mesafeye yerle�tir
            float safeDistance = Mathf.Max(hit.distance - collisionRadius - 0.1f, dynamicMinDistance);
            currentDistance = safeDistance;
            return targetPosition + direction * safeDistance;
        }

        // Ek kontrol: Kameran�n bulunaca�� pozisyonda overlap var m�?
        if (Physics.CheckSphere(desiredPosition, collisionRadius, obstacleLayer))
        {
            // Overlap varsa, kameray� g�venli mesafeye �ek
            float safeDistance = dynamicMinDistance;
            currentDistance = safeDistance;
            return targetPosition + direction * safeDistance;
        }

        // Collision yoksa, kameray� orijinal mesafesine d�nd�r
        currentDistance = Mathf.Lerp(currentDistance, distance, Time.deltaTime * 2f);
        return targetPosition + direction * currentDistance;
    }

    // Debug i�in
    private void OnDrawGizmosSelected()
    {
        if (followTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(followTarget.position + offset, 0.5f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, collisionRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(followTarget.position + offset, transform.position);
        }
    }

    // Player Controller i�in gerekli (e�er hala kullan�yorsan�z)
    public Quaternion PlanarRotation => Quaternion.Euler(0, fixedRotation.y, 0);
}
