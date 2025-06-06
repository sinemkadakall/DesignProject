using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropZone : MonoBehaviour
{
    private int expectedValue;
    private GameManager gameManager;
    private bool isCompleted = false; // Bu zone'un tamamlanýp tamamlanmadýðýný takip eder

    void Start()
    {
        // GameManager referansýný al
        gameManager = FindObjectOfType<GameManager>();
    }

    // GameManager'dan beklenen deðeri almak için metod
    public void SetExpectedValue(int value)
    {
        expectedValue = value;
        isCompleted = false; // Yeni deðer atandýðýnda tamamlanma durumunu sýfýrla
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

    // Zone'un tamamlanma durumunu kontrol etmek için
    public bool IsCompleted()
    {
        return isCompleted;
    }

    // Zone'u sýfýrlamak için (yeni round baþladýðýnda)
    public void ResetZone()
    {
        isCompleted = false;
        expectedValue = 0;
    }

    // Zone'daki yerleþtirilmiþ sayýyý almak için (opsiyonel)
    public int GetPlacedValue()
    {
        // Eðer bu zone'da bir NumberController varsa, deðerini döndür
        NumberController placedNumber = GetComponentInChildren<NumberController>();
        if (placedNumber != null)
        {
            return placedNumber.GetNumberValue(); // Bu metodu NumberController'a eklemek gerekebilir
        }
        return -1; // Yerleþtirilmiþ sayý yok
    }
}