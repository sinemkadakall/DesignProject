 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    [Header("Takip Ayarlarý")]
    [SerializeField] Transform followTarget;
    [SerializeField] float distance = 5f;
    [SerializeField] Vector3 offset = new Vector3(0, 2, 0); // Karakterin arkasýnda ve yukarýsýnda

    [Header("Collision Detection")]
    [SerializeField] LayerMask obstacleLayer = -1; // Hangi layerlarý obstacle olarak görsün
    [SerializeField] float collisionRadius = 0.3f;
    [SerializeField] float minDistance = 2f; // Minimum kamera mesafesi (artýrýldý)
    [SerializeField] float characterBuffer = 0.5f; // Karaktere olan minimum mesafe

    [Header("Hareket Ayarlarý")]
    [SerializeField] float followSpeed = 5f; // Kameranýn takip hýzý
    [SerializeField] bool smoothFollow = true;

    [Header("Kamera Açýsý")]
    [SerializeField] bool fixedAngle = true; // Sabit açý için
    [SerializeField] Vector3 fixedRotation = new Vector3(15, 0, 0); // Sabit bakýþ açýsý

    private Vector3 currentVelocity;
    private float currentDistance;

    private void Start()
    {
        currentDistance = distance;

        // Eðer sabit açý kullanýyorsak fare kontrolünü kapat
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
            // Sabit açý modu
            UpdateFixedAngleCamera(targetPosition);
        }
        else
        {
            // Orijinal mouse kontrollü mod (isteðe baðlý)
            UpdateMouseControlledCamera(targetPosition);
        }
    }

    private void UpdateFixedAngleCamera(Vector3 targetPosition)
    {
        // Kamera pozisyonunu hesapla
        Vector3 direction = -transform.forward; // Kameranýn baktýðý yönün tersi

        // Sabit açý kullanýyorsak, direction'ý yeniden hesapla
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

        // Kameranýn karaktere bakmasýný saðla
        transform.LookAt(targetPosition);
    }

    private void UpdateMouseControlledCamera(Vector3 targetPosition)
    {
        // Orijinal mouse kontrollü kodunuz (isteðe baðlý)
        // Bu kýsým eski kodunuzdan alýnmýþtýr, eðer mouse kontrolü de istiyorsanýz
    }

    private Vector3 HandleCollision(Vector3 targetPosition, Vector3 desiredPosition)
    {
        Vector3 direction = (desiredPosition - targetPosition).normalized;
        float desiredDistance = Vector3.Distance(targetPosition, desiredPosition);

        // Ýlk olarak, karakterin kendisi ile collision kontrolü
        Collider playerCollider = followTarget.GetComponent<Collider>();
        if (playerCollider == null)
            playerCollider = followTarget.GetComponent<CharacterController>();

        // Karakterin büyüklüðünü hesapla
        float playerRadius = 0.5f; // Varsayýlan deðer
        if (playerCollider != null)
        {
            playerRadius = Mathf.Max(playerCollider.bounds.size.x, playerCollider.bounds.size.z) * 0.6f;
        }

        // Minimum mesafeyi karakterin büyüklüðüne göre ayarla
        float dynamicMinDistance = Mathf.Max(minDistance, playerRadius + 0.5f);

        // Eðer kamera çok yakýnsa, zorla uzaklaþtýr
        if (desiredDistance < dynamicMinDistance)
        {
            currentDistance = dynamicMinDistance;
            return targetPosition + direction * dynamicMinDistance;
        }

        // Raycast ile collision kontrolü (birden fazla kontrol)
        RaycastHit hit;

        // Ana SphereCast
        if (Physics.SphereCast(targetPosition, collisionRadius, direction, out hit, desiredDistance, obstacleLayer))
        {
            // Collision varsa, kamerayý güvenli mesafeye yerleþtir
            float safeDistance = Mathf.Max(hit.distance - collisionRadius - 0.1f, dynamicMinDistance);
            currentDistance = safeDistance;
            return targetPosition + direction * safeDistance;
        }

        // Ek kontrol: Kameranýn bulunacaðý pozisyonda overlap var mý?
        if (Physics.CheckSphere(desiredPosition, collisionRadius, obstacleLayer))
        {
            // Overlap varsa, kamerayý güvenli mesafeye çek
            float safeDistance = dynamicMinDistance;
            currentDistance = safeDistance;
            return targetPosition + direction * safeDistance;
        }

        // Collision yoksa, kamerayý orijinal mesafesine döndür
        currentDistance = Mathf.Lerp(currentDistance, distance, Time.deltaTime * 2f);
        return targetPosition + direction * currentDistance;
    }

    // Debug için
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

    // Player Controller için gerekli (eðer hala kullanýyorsanýz)
    public Quaternion PlanarRotation => Quaternion.Euler(0, fixedRotation.y, 0);
}
