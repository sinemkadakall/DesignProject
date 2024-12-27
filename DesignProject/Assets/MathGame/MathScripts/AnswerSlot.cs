using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropZone : MonoBehaviour
{
    private int expectedValue;
    private GameManager gameManager;

    void Start()
    {
        // GameManager referansýný al
        gameManager = FindObjectOfType<GameManager>();
    }

    // GameManager'dan beklenen deðeri almak için metod
    public void SetExpectedValue(int value)
    {
        expectedValue = value;
    }

    public bool IsCorrectDrop(int draggedValue)
    {
        // Debug.Log ile kontrol edelim
        Debug.Log($"Dropped Value: {draggedValue}, Expected Value: {expectedValue}");
        return draggedValue == expectedValue;
    }
}
