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

    [Header("Score UI")]
    public TextMeshProUGUI correctCountText;  // Doðru sayýsýný gösteren text
    public TextMeshProUGUI wrongCountText;    // Yanlýþ sayýsýný gösteren text

    [Header("Game Settings")]
    public int totalNumbers = 10;       // Alt kýsýmda gösterilecek toplam sayý
    public int maxNumber = 9;           // Rastgele sayýlar için maksimum deðer
    public float newRoundDelay = 2f;    // Yeni round için bekleme süresi

    [Header("Audio")]
    public AudioSource audioSource; // Inspector'dan atanacak
    public AudioClip correctSound; // Doðru cevap ses efekti
    public AudioClip wrongSound;   // Yanlýþ cevap ses efekti

    private int[] operationResults;
    private int[] firstNumbers;   // Ýlk sayýlarý takip etmek için
    private int[] secondNumbers;  // Ýkinci sayýlarý takip etmek için
    private int[] operationTypes; // Ýþlem türlerini takip etmek için
    private List<int> availableNumbers = new List<int>();

    // Yeni eklenen deðiþkenler
    private bool[] operationCompleted; // Hangi iþlemlerin tamamlandýðýný takip eder
    private int completedOperationsCount = 0; // Tamamlanan iþlem sayýsý

    // Score tracking deðiþkenleri
    private int correctAnswersCount = 0;
    private int wrongAnswersCount = 0;
    private bool[] operationHadWrongAnswer; // Her iþlem için yanlýþ cevap verilip verilmediðini takip eder

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
        operationCompleted = new bool[operationContainers.Length];
        operationHadWrongAnswer = new bool[operationContainers.Length]; // Yeni eklenen
        completedOperationsCount = 0;

        GenerateOperations();
        GenerateNumbers();
        UpdateScoreUI();
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

        // Sayýlarý oluþturmadan önce bir frame bekle
        StartCoroutine(CreateNumbersWithDelay());
    }

    // Sayýlarý gecikmeli oluþturan coroutine
    IEnumerator CreateNumbersWithDelay()
    {
        yield return null; // Bir frame bekle

        foreach (int number in availableNumbers)
        {
            CreateNumberObject(number);
        }

        Debug.Log($"Generated {availableNumbers.Count} numbers for new round");
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
        // Mevcut tüm sayý objelerini temizle
        List<Transform> childrenToDestroy = new List<Transform>();

        foreach (Transform child in numberParent)
        {
            childrenToDestroy.Add(child);
        }

        foreach (Transform child in childrenToDestroy)
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

            if (isCorrect)
            {
                // Doðru cevap ses efektini çal
                if (correctSound != null)
                {
                    audioSource.PlayOneShot(correctSound);
                }

                // Bu iþlemi tamamlandý olarak iþaretle
                if (!operationCompleted[zoneIndex])
                {
                    operationCompleted[zoneIndex] = true;
                    completedOperationsCount++;

                    // Doðru cevap verildiðinde sadece doðru sayýsýný artýr
                    // Daha önce yanlýþ yapýlmýþ olsa bile, doðru tamamlandýðýnda sadece doðru sayar
                    correctAnswersCount++;

                    UpdateScoreUI();

                    Debug.Log($"Operation {zoneIndex} completed correctly - counted as correct");

                    Debug.Log($"Operation {zoneIndex} completed! Total completed: {completedOperationsCount}/{operationContainers.Length}");

                    // Tüm iþlemler tamamlandýysa yeni round baþlat
                    if (completedOperationsCount >= operationContainers.Length)
                    {
                        StartCoroutine(StartNewRound());
                    }
                }
            }
            else
            {
                // Yanlýþ cevap
                if (wrongSound != null)
                {
                    audioSource.PlayOneShot(wrongSound);
                }

                // Bu iþlem tamamlanmamýþsa yanlýþ sayýsýný artýr
                if (!operationCompleted[zoneIndex])
                {
                    // Eðer bu iþlem için daha önce yanlýþ cevap verilmemiþse yanlýþ sayýsýný artýr
                    if (!operationHadWrongAnswer[zoneIndex])
                    {
                        wrongAnswersCount++;
                        operationHadWrongAnswer[zoneIndex] = true;
                        UpdateScoreUI();
                        Debug.Log($"First wrong answer for operation {zoneIndex} - wrong count increased");
                    }
                    else
                    {
                        Debug.Log($"Already counted wrong answer for operation {zoneIndex}");
                    }
                }
            }

            return isCorrect;
        }

        Debug.LogWarning("Drop zone not recognized!");
        return false;
    } 

    // Score UI'sýný güncelleme metodu
    private void UpdateScoreUI()
    {
        if (correctCountText != null)
        {
            correctCountText.text = correctAnswersCount.ToString();
        }

        if (wrongCountText != null)
        {
            wrongCountText.text = wrongAnswersCount.ToString();
        }
    }

    // Yeni eklenen metod: Yeni round baþlatma
    private IEnumerator StartNewRound()
    {
        Debug.Log("All operations completed! Starting new round in " + newRoundDelay + " seconds...");

        // Belirtilen süre kadar bekle
        yield return new WaitForSeconds(newRoundDelay);

        // Drop zone'lardan sayýlarý temizle
        ClearDropZones();

        // Bir frame bekle ki Unity objelerini düzgün temizleyebilsin
        yield return null;

        // Yeni oyunu baþlat (score sýfýrlanmaz, sadece operasyon durumlarý sýfýrlanýr)
        InitializeGame();

        Debug.Log("New round started!");
    }

    // Yeni eklenen metod: Drop zone'larý temizleme
    private void ClearDropZones()
    {
        foreach (Transform dropZone in dropZones)
        {
            // Önce tüm çocuk objeleri bir listeye al
            List<Transform> childrenToDestroy = new List<Transform>();

            foreach (Transform child in dropZone)
            {
                NumberController numberController = child.GetComponent<NumberController>();
                if (numberController != null)
                {
                    childrenToDestroy.Add(child);
                }
            }

            // Sonra listedeki objeleri yok et
            foreach (Transform child in childrenToDestroy)
            {
                Destroy(child.gameObject);
            }

            // DropZone'u sýfýrla
            DropZone dropZoneComponent = dropZone.GetComponent<DropZone>();
            if (dropZoneComponent != null)
            {
                dropZoneComponent.ResetZone();
            }
        }
    }

    // Yeni eklenen metod: Manuel yeni round baþlatma (opsiyonel)
    public void ForceNewRound()
    {
        StopAllCoroutines(); // Mevcut coroutine'leri durdur
        StartCoroutine(StartNewRound());
    }

    public void ResetGame()
    {
        StopAllCoroutines(); // Tüm coroutine'leri durdur
        ClearDropZones(); // Drop zone'larý temizle

        // Score'larý sýfýrla
        correctAnswersCount = 0;
        wrongAnswersCount = 0;
        UpdateScoreUI();

        InitializeGame();
    }

    // Score'larý sýfýrlama metodu (opsiyonel)
    public void ResetScores()
    {
        correctAnswersCount = 0;
        wrongAnswersCount = 0;
        UpdateScoreUI();
        Debug.Log("Scores reset!");
    }

    // Mevcut score'larý alma metotlarý (opsiyonel)
    public int GetCorrectCount()
    {
        return correctAnswersCount;
    }


    public int GetWrongCount()
    {
        return wrongAnswersCount;
    }
}