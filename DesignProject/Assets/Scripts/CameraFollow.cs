using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // Takip edilecek hedef (karakter)

    [Header("Camera Back View Settings")]
    [SerializeField] private float distance = 5f; // Karakterden uzakl�k
    [SerializeField] private float height = 1.6f; // Kamera y�ksekli�i
    [SerializeField] private float smoothSpeed = 0.125f; // Kamera takip yumu�akl���

    private void LateUpdate()
    {
        if (target == null) return;

        // Karakterin arkas�ndaki tam pozisyonu hesapla
        Vector3 desiredPosition = target.position
            - target.forward * distance  // Karakterin tam arkas�na al
            + Vector3.up * height;        // Belirli bir y�ksekli�e ayarla

        // Yumu�ak kamera takibi
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Kameran�n konumunu g�ncelle
        transform.position = smoothedPosition;

        // Kameray� hedefe do�ru �evir
        transform.LookAt(target);
    }
}
