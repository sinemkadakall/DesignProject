using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public class NumberData
    {
        public int value;
        public Sprite sprite;
    }

    [Header("UI References")]
    public NumberData[] numberSprites;  // 0-9 arasý sayý spritelarý
    public Sprite[] operatorSprites;   // +, -, *, / iþlem spritelarý
    public GameObject[] operationContainers; // Ýþlem alanlarý (Num1, Operator, Num2 için)
    public Transform[] dropZones;       // Sonuç býrakma alanlarý
    public Transform numberParent;      // Alt kýsýmdaki sayýlar için parent
    public GameObject numberPrefab;     // Sürüklenebilir sayý prefabý

    [Header("Game Settings")]
    public int totalNumbers = 10;       // Alt kýsýmda gösterilecek toplam sayý
    public int maxNumber = 9;           // Rastgele sayýlar için maksimum deðer

    [Header("Audio")]
    public AudioSource audioSource; // Inspector'dan atanacak
    public AudioClip correctSound; // Doðru cevap ses efekti
    public AudioClip wrongSound;   // Yanlýþ cevap ses efekti

    private int[] operationResults;
    // Her iþlem için doðru sonuçlar
    private int[] firstNumbers;   // Ýlk sayýlarý takip etmek için
    private int[] secondNumbers;  // Ýkinci sayýlarý takip etmek için
    private int[] operationTypes; // Ýþlem türlerini takip etmek için
    private List<int> availableNumbers = new List<int>();

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        InitializeGame();
    }

 
    void InitializeGame()
    {
        operationResults = new int[operationContainers.Length];
        firstNumbers = new int[operationContainers.Length];
        secondNumbers = new int[operationContainers.Length];
        operationTypes = new int[operationContainers.Length];
        GenerateOperations();
        GenerateNumbers();
    }


    void GenerateOperations()
    {
        HashSet<int> usedResults = new HashSet<int>();

        for (int i = 0; i < operationContainers.Length; i++)
        {
            Transform container = operationContainers[i].transform;

            if (container.childCount < 3)
            {
                Debug.LogError($"Operation container {i} requires exactly 3 children!");
                continue;
            }

            int maxAttempts = 50;
            int attempts = 0;
            bool foundValidOperation = false;

            while (!foundValidOperation && attempts < maxAttempts)
            {
                int operationType = Random.Range(0, operatorSprites.Length);
                operationTypes[i] = operationType;

                GenerateValidNumbers(i, operationType);

                // Eðer sonuç daha önce kullanýlmamýþsa, iþlemi kabul et
                if (!usedResults.Contains(operationResults[i]))
                {
                    usedResults.Add(operationResults[i]);
                    foundValidOperation = true;

                    container.GetChild(0).GetComponent<Image>().sprite = GetNumberSprite(firstNumbers[i]);
                    container.GetChild(1).GetComponent<Image>().sprite = operatorSprites[operationType];
                    container.GetChild(2).GetComponent<Image>().sprite = GetNumberSprite(secondNumbers[i]);

                    if (dropZones[i] != null)
                    {
                        DropZone dropZone = dropZones[i].GetComponent<DropZone>();
                        if (dropZone != null)
                        {
                            dropZone.SetExpectedValue(operationResults[i]);
                            Debug.Log($"Operation {i}: {firstNumbers[i]} {GetOperatorSymbol(operationType)} {secondNumbers[i]} = {operationResults[i]}");
                        }
                    }
                }
                attempts++;
            }

            // Eðer geçerli bir iþlem bulunamazsa, önceki iþlemleri sýfýrla ve baþtan baþla
            if (!foundValidOperation)
            {
                Debug.Log("Yeterli benzersiz sonuç bulunamadý, iþlemler yeniden oluþturuluyor...");
                usedResults.Clear();
                i = -1; // Döngü i'yi arttýracaðý için -1'den baþlýyoruz
                continue;
            }
        }
    }
    void GenerateValidNumbers(int index, int operationType)
    {
        switch (operationType)
        {
            case 0: // Toplama
                do
                {
                    firstNumbers[index] = Random.Range(0, 6);  // Daha küçük sayýlarla baþla
                    secondNumbers[index] = Random.Range(0, 6);
                } while (firstNumbers[index] + secondNumbers[index] > 9);
                operationResults[index] = firstNumbers[index] + secondNumbers[index];
                break;

            case 1: // Çýkarma
                do
                {
                    firstNumbers[index] = Random.Range(1, 10);
                    secondNumbers[index] = Random.Range(0, firstNumbers[index]);
                } while (firstNumbers[index] - secondNumbers[index] > 9);
                operationResults[index] = firstNumbers[index] - secondNumbers[index];
                break;

            case 2: // Çarpma
                do
                {
                    firstNumbers[index] = Random.Range(0, 4);
                    secondNumbers[index] = Random.Range(0, 4);
                } while (firstNumbers[index] * secondNumbers[index] > 9);
                operationResults[index] = firstNumbers[index] * secondNumbers[index];
                break;

            case 3: // Bölme
                do
                {
                    secondNumbers[index] = Random.Range(1, 4);
                    firstNumbers[index] = secondNumbers[index] * Random.Range(1, 4);
                } while (firstNumbers[index] > 9 || firstNumbers[index] / secondNumbers[index] > 9);
                operationResults[index] = firstNumbers[index] / secondNumbers[index];
                break;
        }
    }

    string GetOperatorSymbol(int operationType)
    {
        switch (operationType)
        {
            case 0: return "+";
            case 1: return "-";
            case 2: return "×";
            case 3: return "÷";
            default: return "?";
        }
    }


    void GenerateNumbers()
    {
        ClearExistingNumbers();
        availableNumbers.Clear();

        // Ýþlem sonuçlarýný ekle
        foreach (int result in operationResults)
        {
            if (result >= 0 && result <= 9)
            {
                availableNumbers.Add(result);
            }
        }

        // Kalan boþluklarý doldur
        while (availableNumbers.Count < totalNumbers)
        {
            int randomNum = Random.Range(0, 10);
            if (!availableNumbers.Contains(randomNum))
            {
                availableNumbers.Add(randomNum);
            }
        }

        ShuffleList(availableNumbers);

        foreach (int number in availableNumbers)
        {
            CreateNumberObject(number);
        }
    }


    void CreateNumberObject(int number)
    {
        GameObject numberObj = Instantiate(numberPrefab, numberParent);
        Image numberImage = numberObj.GetComponent<Image>();
        numberImage.sprite = GetNumberSprite(number);

        NumberController controller = numberObj.GetComponent<NumberController>();
        if (controller != null)
        {
            controller.Initialize(number);
        }
    }

    void ClearExistingNumbers()
    {
        foreach (Transform child in numberParent)
        {
            Destroy(child.gameObject);
        }
    }

   
    Sprite GetNumberSprite(int number)
    {
        foreach (NumberData data in numberSprites)
        {
            if (data.value == number)
                return data.sprite;
        }

        Debug.LogWarning($"No sprite found for number {number}!");
        return null;
    }

  
    void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

   
    public bool CheckAnswer(int answer, Transform dropZone)
    {
        int zoneIndex = System.Array.IndexOf(dropZones, dropZone);
        if (zoneIndex != -1)
        {
            bool isCorrect = operationResults[zoneIndex] == answer;

            // Doðru veya yanlýþ ses efektini çal
            if (isCorrect && correctSound != null)
            {
                audioSource.PlayOneShot(correctSound);
            }
            else if (!isCorrect && wrongSound != null)
            {
                audioSource.PlayOneShot(wrongSound);
            }

            return isCorrect;
        }

        Debug.LogWarning("Drop zone not recognized!");
        return false;
    }


    
    public void ResetGame()
    {
        InitializeGame();
    }
}


