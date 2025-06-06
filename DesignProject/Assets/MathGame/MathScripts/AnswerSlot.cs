using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropZone : MonoBehaviour
{
    private int expectedValue;
    private GameManager gameManager;
    private bool isCompleted = false; // Bu zone'un tamamlan�p tamamlanmad���n� takip eder

    void Start()
    {
        // GameManager referans�n� al
        gameManager = FindObjectOfType<GameManager>();
    }

    // GameManager'dan beklenen de�eri almak i�in metod
    public void SetExpectedValue(int value)
    {
        expectedValue = value;
        isCompleted = false; // Yeni de�er atand���nda tamamlanma durumunu s�f�rla
    }

    public bool IsCorrectDrop(int draggedValue)
    {
        // Debug.Log ile kontrol edelim
        Debug.Log($"Dropped Value: {draggedValue}, Expected Value: {expectedValue}");

        bool isCorrect = draggedValue == expectedValue;

        if (isCorrect && !isCompleted)
        {
            isCompleted = true;
            Debug.Log($"Drop zone completed with value: {expectedValue}");
        }

        return isCorrect;
    }

    // Zone'un tamamlanma durumunu kontrol etmek i�in
    public bool IsCompleted()
    {
        return isCompleted;
    }

    // Zone'u s�f�rlamak i�in (yeni round ba�lad���nda)
    public void ResetZone()
    {
        isCompleted = false;
        expectedValue = 0;
    }

    // Zone'daki yerle�tirilmi� say�y� almak i�in (opsiyonel)
    public int GetPlacedValue()
    {
        // E�er bu zone'da bir NumberController varsa, de�erini d�nd�r
        NumberController placedNumber = GetComponentInChildren<NumberController>();
        if (placedNumber != null)
        {
            return placedNumber.GetNumberValue(); // Bu metodu NumberController'a eklemek gerekebilir
        }
        return -1; // Yerle�tirilmi� say� yok
    }
}