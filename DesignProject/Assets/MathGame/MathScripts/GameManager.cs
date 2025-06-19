using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour, IGameDataProvider
{
    [System.Serializable]
    public class NumberData
    {
        public int value;
        public Sprite sprite;
    }

    [Header("UI References")]
    public NumberData[] numberSprites;  // 0-9 arası sayı spriteları
    public Sprite[] operatorSprites;   // +, -, *, / işlem spriteları
    public GameObject[] operationContainers; // İşlem alanları (Num1, Operator, Num2 için)
    public Transform[] dropZones;       // Sonuç bırakma alanları
    public Transform numberParent;      // Alt kısımdaki sayılar için parent
    public GameObject numberPrefab;     // Sürüklenebilir sayı prefabı

    [Header("Score UI")]
    public TextMeshProUGUI correctCountText;  // Doğru sayısını gösteren text
    public TextMeshProUGUI wrongCountText;    // Yanlış sayısını gösteren text

    [Header("Game Settings")]
    public int totalNumbers = 10;       // Alt kısımda gösterilecek toplam sayı
    public int maxNumber = 9;           // Rastgele sayılar için maksimum değer
    public float newRoundDelay = 2f;    // Yeni round için bekleme süresi

    [Header("Audio")]
    public AudioSource audioSource; // Inspector'dan atanacak
    public AudioClip correctSound; // Doğru cevap ses efekti
    public AudioClip wrongSound;   // Yanlış cevap ses efekti

    private int[] operationResults;
    private int[] firstNumbers;   // İlk sayıları takip etmek için
    private int[] secondNumbers;  // İkinci sayıları takip etmek için
    private int[] operationTypes; // İşlem türlerini takip etmek için
    private List<int> availableNumbers = new List<int>();

    // Yeni eklenen değişkenler
    private bool[] operationCompleted; // Hangi işlemlerin tamamlandığını takip eder
    private int completedOperationsCount = 0; // Tamamlanan işlem sayısı

    // Score tracking değişkenleri
    private int correctAnswersCount = 0;
    private int wrongAnswersCount = 0;
    private bool[] operationHadWrongAnswer; // Her işlem için yanlış cevap verilip verilmediğini takip eder

    // API entegrasyonu için eklenen değişkenler
    private float gameStartTime;
    private int totalScore = 0;
    private int baseScorePerCorrect = 10; // Her doğru cevap için temel puan

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Oyun başlangıç zamanını kaydet
        gameStartTime = Time.time;

        InitializeGame();

        // GameDataSender'a bu sahnenin başladığını bildir
        if (GameDataSender.Instance != null)
        {
            Debug.Log("🎮 MathGame başlatıldı, GameDataSender ile bağlantı kuruldu");
        }
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

                // Eğer sonuç daha önce kullanılmamışsa, işlemi kabul et
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

            // Eğer geçerli bir işlem bulunamazsa, önceki işlemleri sıfırla ve baştan başla
            if (!foundValidOperation)
            {
                Debug.Log("Yeterli benzersiz sonuç bulunamadı, işlemler yeniden oluşturuluyor...");
                usedResults.Clear();
                i = -1; // Döngü i'yi arttıracağı için -1'den başlıyoruz
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
                    firstNumbers[index] = Random.Range(0, 6);  // Daha küçük sayılarla başla
                    secondNumbers[index] = Random.Range(0, 6);
                } while (firstNumbers[index] + secondNumbers[index] > 9);
                operationResults[index] = firstNumbers[index] + secondNumbers[index];
                break;

            case 1: // Çıkarma
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

        // İşlem sonuçlarını ekle
        foreach (int result in operationResults)
        {
            if (result >= 0 && result <= 9)
            {
                availableNumbers.Add(result);
            }
        }

        // Kalan boşlukları doldur
        while (availableNumbers.Count < totalNumbers)
        {
            int randomNum = Random.Range(0, 10);
            if (!availableNumbers.Contains(randomNum))
            {
                availableNumbers.Add(randomNum);
            }
        }

        ShuffleList(availableNumbers);

        // Sayıları oluşturmadan önce bir frame bekle
        StartCoroutine(CreateNumbersWithDelay());
    }

    // Sayıları gecikmeli oluşturan coroutine
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
        // Mevcut tüm sayı objelerini temizle
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
                // Doğru cevap ses efektini çal
                if (correctSound != null)
                {
                    audioSource.PlayOneShot(correctSound);
                }

                // Bu işlemi tamamlandı olarak işaretle
                if (!operationCompleted[zoneIndex])
                {
                    operationCompleted[zoneIndex] = true;
                    completedOperationsCount++;

                    // Her doğru cevap için doğru sayısını artır
                    correctAnswersCount++;

                    // Puan hesapla (zorluğa göre bonus puan)
                    int earnedPoints = CalculateScore(zoneIndex);
                    totalScore += earnedPoints;

                    UpdateScoreUI();

                    // GameDataSender'a bildir
                    if (GameDataSender.Instance != null)
                    {
                        GameDataSender.Instance.AddCorrectAnswer(1);
                        GameDataSender.Instance.AddScore(earnedPoints);
                    }

                    Debug.Log($"✅ Operation {zoneIndex} completed correctly - earned {earnedPoints} points");
                    Debug.Log($"Operation {zoneIndex} completed! Total completed: {completedOperationsCount}/{operationContainers.Length}");

                    // Tüm işlemler tamamlandıysa yeni round başlat
                    if (completedOperationsCount >= operationContainers.Length)
                    {
                        StartCoroutine(StartNewRound());
                    }
                }
            }
            else
            {
                // Yanlış cevap ses efektini çal
                if (wrongSound != null)
                {
                    audioSource.PlayOneShot(wrongSound);
                }

                // Bu işlem tamamlanmamışsa her yanlış cevap için yanlış sayısını artır
                if (!operationCompleted[zoneIndex])
                {
                    wrongAnswersCount++;
                    UpdateScoreUI();

                    // GameDataSender'a bildir
                    if (GameDataSender.Instance != null)
                    {
                        GameDataSender.Instance.AddWrongAnswer(1);
                    }

                    Debug.Log($"❌ Wrong answer for operation {zoneIndex} - wrong count increased to {wrongAnswersCount}");
                }
            }

            return isCorrect;
        }

        Debug.LogWarning("Drop zone not recognized!");
        return false;
    }

    // Puan hesaplama metodu - işlem türüne göre farklı puanlar
    private int CalculateScore(int operationIndex)
    {
        int baseScore = baseScorePerCorrect;

        // İşlem türüne göre bonus puan
        switch (operationTypes[operationIndex])
        {
            case 0: // Toplama
                return baseScore;
            case 1: // Çıkarma
                return baseScore + 5;
            case 2: // Çarpma
                return baseScore + 10;
            case 3: // Bölme
                return baseScore + 15;
            default:
                return baseScore;
        }
    }

    // Score UI'sını güncelleme metodu
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

    // Yeni eklenen metod: Yeni round başlatma
    private IEnumerator StartNewRound()
    {
        Debug.Log("All operations completed! Starting new round in " + newRoundDelay + " seconds...");

        // Belirtilen süre kadar bekle
        yield return new WaitForSeconds(newRoundDelay);

        // Drop zone'lardan sayıları temizle
        ClearDropZones();

        // Bir frame bekle ki Unity objelerini düzgün temizleyebilsin
        yield return null;

        // Yeni oyunu başlat (score sıfırlanmaz, sadece operasyon durumları sıfırlanır)
        InitializeGame();

        Debug.Log("New round started!");
    }

    // Yeni eklenen metod: Drop zone'ları temizleme
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

            // DropZone'u sıfırla
            DropZone dropZoneComponent = dropZone.GetComponent<DropZone>();
            if (dropZoneComponent != null)
            {
                dropZoneComponent.ResetZone();
            }
        }
    }

    // Yeni eklenen metod: Manuel yeni round başlatma (opsiyonel)
    public void ForceNewRound()
    {
        StopAllCoroutines(); // Mevcut coroutine'leri durdur
        StartCoroutine(StartNewRound());
    }

    public void ResetGame()
    {
        StopAllCoroutines(); // Tüm coroutine'leri durdur
        ClearDropZones(); // Drop zone'ları temizle

        // Score'ları sıfırla
        correctAnswersCount = 0;
        wrongAnswersCount = 0;
        totalScore = 0;
        gameStartTime = Time.time; // Zamanı sıfırla
        UpdateScoreUI();

        InitializeGame();

        Debug.Log("🔄 MathGame resetlendi");
    }

    // Score'ları sıfırlama metodu (opsiyonel)
    public void ResetScores()
    {
        correctAnswersCount = 0;
        wrongAnswersCount = 0;
        totalScore = 0;
        UpdateScoreUI();
        Debug.Log("Scores reset!");
    }

    // ================== IGameDataProvider Implementation ==================

    public int GetCorrectAnswers()
    {
        return correctAnswersCount;
    }

    public int GetWrongAnswers()
    {
        return wrongAnswersCount;
    }

    public int GetScore()
    {
        return totalScore;
    }

    public float GetTimeSpent()
    {
        return Time.time - gameStartTime;
    }

    // ================== Eski metodlar (geriye uyumluluk için) ==================

    public int GetCorrectCount()
    {
        return correctAnswersCount;
    }

    public int GetWrongCount()
    {
        return wrongAnswersCount;
    }

    // ================== Debug ve Test Metodları ==================

    [ContextMenu("Test - Show Current Stats")]
    void ShowCurrentStats()
    {
        Debug.Log($"📊 MathGame Stats:");
        Debug.Log($"Correct Answers: {GetCorrectAnswers()}");
        Debug.Log($"Wrong Answers: {GetWrongAnswers()}");
        Debug.Log($"Total Score: {GetScore()}");
        Debug.Log($"Time Spent: {GetTimeSpent():F1} seconds");
        Debug.Log($"Accuracy: {(GetCorrectAnswers() > 0 ? (float)GetCorrectAnswers() / (GetCorrectAnswers() + GetWrongAnswers()) * 100f : 0):F1}%");
    }

    [ContextMenu("Test - Send Data to API")]
    void SendDataToAPI()
    {
        if (GameDataSender.Instance != null)
        {
            GameDataSender.Instance.SendSessionData();
        }
        else
        {
            Debug.LogWarning("GameDataSender instance not found!");
        }
    }

    // Oyun sonunda çağrılacak metod
    void OnDestroy()
    {
        // GameDataSender'a sahne bittiğini bildir
        if (GameDataSender.Instance != null)
        {
            Debug.Log("🎮 MathGame sahne sonu - final stats gönderildi");
        }
    }
}