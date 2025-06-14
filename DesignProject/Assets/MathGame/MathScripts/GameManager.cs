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
    public NumberData[] numberSprites;  // 0-9 aras� say� spritelar�
    public Sprite[] operatorSprites;   // +, -, *, / i�lem spritelar�
    public GameObject[] operationContainers; // ��lem alanlar� (Num1, Operator, Num2 i�in)
    public Transform[] dropZones;       // Sonu� b�rakma alanlar�
    public Transform numberParent;      // Alt k�s�mdaki say�lar i�in parent
    public GameObject numberPrefab;     // S�r�klenebilir say� prefab�

    [Header("Score UI")]
    public TextMeshProUGUI correctCountText;  // Do�ru say�s�n� g�steren text
    public TextMeshProUGUI wrongCountText;    // Yanl�� say�s�n� g�steren text

    [Header("Game Settings")]
    public int totalNumbers = 10;       // Alt k�s�mda g�sterilecek toplam say�
    public int maxNumber = 9;           // Rastgele say�lar i�in maksimum de�er
    public float newRoundDelay = 2f;    // Yeni round i�in bekleme s�resi

    [Header("Audio")]
    public AudioSource audioSource; // Inspector'dan atanacak
    public AudioClip correctSound; // Do�ru cevap ses efekti
    public AudioClip wrongSound;   // Yanl�� cevap ses efekti

    private int[] operationResults;
    private int[] firstNumbers;   // �lk say�lar� takip etmek i�in
    private int[] secondNumbers;  // �kinci say�lar� takip etmek i�in
    private int[] operationTypes; // ��lem t�rlerini takip etmek i�in
    private List<int> availableNumbers = new List<int>();

    // Yeni eklenen de�i�kenler
    private bool[] operationCompleted; // Hangi i�lemlerin tamamland���n� takip eder
    private int completedOperationsCount = 0; // Tamamlanan i�lem say�s�

    // Score tracking de�i�kenleri
    private int correctAnswersCount = 0;
    private int wrongAnswersCount = 0;
    private bool[] operationHadWrongAnswer; // Her i�lem i�in yanl�� cevap verilip verilmedi�ini takip eder

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

                // E�er sonu� daha �nce kullan�lmam��sa, i�lemi kabul et
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

            // E�er ge�erli bir i�lem bulunamazsa, �nceki i�lemleri s�f�rla ve ba�tan ba�la
            if (!foundValidOperation)
            {
                Debug.Log("Yeterli benzersiz sonu� bulunamad�, i�lemler yeniden olu�turuluyor...");
                usedResults.Clear();
                i = -1; // D�ng� i'yi artt�raca�� i�in -1'den ba�l�yoruz
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
                    firstNumbers[index] = Random.Range(0, 6);  // Daha k���k say�larla ba�la
                    secondNumbers[index] = Random.Range(0, 6);
                } while (firstNumbers[index] + secondNumbers[index] > 9);
                operationResults[index] = firstNumbers[index] + secondNumbers[index];
                break;

            case 1: // ��karma
                do
                {
                    firstNumbers[index] = Random.Range(1, 10);
                    secondNumbers[index] = Random.Range(0, firstNumbers[index]);
                } while (firstNumbers[index] - secondNumbers[index] > 9);
                operationResults[index] = firstNumbers[index] - secondNumbers[index];
                break;

            case 2: // �arpma
                do
                {
                    firstNumbers[index] = Random.Range(0, 4);
                    secondNumbers[index] = Random.Range(0, 4);
                } while (firstNumbers[index] * secondNumbers[index] > 9);
                operationResults[index] = firstNumbers[index] * secondNumbers[index];
                break;

            case 3: // B�lme
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
            case 2: return "�";
            case 3: return "�";
            default: return "?";
        }
    }

    void GenerateNumbers()
    {
        ClearExistingNumbers();
        availableNumbers.Clear();

        // ��lem sonu�lar�n� ekle
        foreach (int result in operationResults)
        {
            if (result >= 0 && result <= 9)
            {
                availableNumbers.Add(result);
            }
        }

        // Kalan bo�luklar� doldur
        while (availableNumbers.Count < totalNumbers)
        {
            int randomNum = Random.Range(0, 10);
            if (!availableNumbers.Contains(randomNum))
            {
                availableNumbers.Add(randomNum);
            }
        }

        ShuffleList(availableNumbers);

        // Say�lar� olu�turmadan �nce bir frame bekle
        StartCoroutine(CreateNumbersWithDelay());
    }

    // Say�lar� gecikmeli olu�turan coroutine
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
        // Mevcut t�m say� objelerini temizle
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
                // Do�ru cevap ses efektini �al
                if (correctSound != null)
                {
                    audioSource.PlayOneShot(correctSound);
                }

                // Bu i�lemi tamamland� olarak i�aretle
                if (!operationCompleted[zoneIndex])
                {
                    operationCompleted[zoneIndex] = true;
                    completedOperationsCount++;

                    // Do�ru cevap verildi�inde sadece do�ru say�s�n� art�r
                    // Daha �nce yanl�� yap�lm�� olsa bile, do�ru tamamland���nda sadece do�ru sayar
                    correctAnswersCount++;

                    UpdateScoreUI();

                    Debug.Log($"Operation {zoneIndex} completed correctly - counted as correct");

                    Debug.Log($"Operation {zoneIndex} completed! Total completed: {completedOperationsCount}/{operationContainers.Length}");

                    // T�m i�lemler tamamland�ysa yeni round ba�lat
                    if (completedOperationsCount >= operationContainers.Length)
                    {
                        StartCoroutine(StartNewRound());
                    }
                }
            }
            else
            {
                // Yanl�� cevap
                if (wrongSound != null)
                {
                    audioSource.PlayOneShot(wrongSound);
                }

                // Bu i�lem tamamlanmam��sa yanl�� say�s�n� art�r
                if (!operationCompleted[zoneIndex])
                {
                    // E�er bu i�lem i�in daha �nce yanl�� cevap verilmemi�se yanl�� say�s�n� art�r
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

    // Score UI's�n� g�ncelleme metodu
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

    // Yeni eklenen metod: Yeni round ba�latma
    private IEnumerator StartNewRound()
    {
        Debug.Log("All operations completed! Starting new round in " + newRoundDelay + " seconds...");

        // Belirtilen s�re kadar bekle
        yield return new WaitForSeconds(newRoundDelay);

        // Drop zone'lardan say�lar� temizle
        ClearDropZones();

        // Bir frame bekle ki Unity objelerini d�zg�n temizleyebilsin
        yield return null;

        // Yeni oyunu ba�lat (score s�f�rlanmaz, sadece operasyon durumlar� s�f�rlan�r)
        InitializeGame();

        Debug.Log("New round started!");
    }

    // Yeni eklenen metod: Drop zone'lar� temizleme
    private void ClearDropZones()
    {
        foreach (Transform dropZone in dropZones)
        {
            // �nce t�m �ocuk objeleri bir listeye al
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

            // DropZone'u s�f�rla
            DropZone dropZoneComponent = dropZone.GetComponent<DropZone>();
            if (dropZoneComponent != null)
            {
                dropZoneComponent.ResetZone();
            }
        }
    }

    // Yeni eklenen metod: Manuel yeni round ba�latma (opsiyonel)
    public void ForceNewRound()
    {
        StopAllCoroutines(); // Mevcut coroutine'leri durdur
        StartCoroutine(StartNewRound());
    }

    public void ResetGame()
    {
        StopAllCoroutines(); // T�m coroutine'leri durdur
        ClearDropZones(); // Drop zone'lar� temizle

        // Score'lar� s�f�rla
        correctAnswersCount = 0;
        wrongAnswersCount = 0;
        UpdateScoreUI();

        InitializeGame();
    }

    // Score'lar� s�f�rlama metodu (opsiyonel)
    public void ResetScores()
    {
        correctAnswersCount = 0;
        wrongAnswersCount = 0;
        UpdateScoreUI();
        Debug.Log("Scores reset!");
    }

    // Mevcut score'lar� alma metotlar� (opsiyonel)
    public int GetCorrectCount()
    {
        return correctAnswersCount;
    }


    public int GetWrongCount()
    {
        return wrongAnswersCount;
    }
}