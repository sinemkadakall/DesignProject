using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // Takip edilecek hedef (karakter)

    [Header("Camera Back View Settings")]
    [SerializeField] private float distance = 5f; // Karakterden uzaklýk
    [SerializeField] private float height = 1.6f; // Kamera yüksekliði
    [SerializeField] private float smoothSpeed = 0.125f; // Kamera takip yumuþaklýðý

    private void LateUpdate()
    {
        if (target == null) return;

        // Karakterin arkasýndaki tam pozisyonu hesapla
        Vector3 desiredPosition = target.position
            - target.forward * distance  // Karakterin tam arkasýna al
            + Vector3.up * height;        // Belirli bir yüksekliðe ayarla

        // Yumuþak kamera takibi
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Kameranýn konumunu güncelle
        transform.position = smoothedPosition;

        // Kamerayý hedefe doðru çevir
        transform.LookAt(target);
    }
}
