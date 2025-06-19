using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MovBall : MonoBehaviour, IGameDataProvider // IGameDataProvider interface'ini ekledik
{
    private Rigidbody rb;
    public float speed = 1.9f;

    [Header("Timer Settings")]
    public float gameTime = 60f; // 60 saniye
    private float currentTime;
    public TextMeshProUGUI timerText; // UI TextMeshPro komponenti
    private bool gameActive = true;

    // GameDataSender için veri takip değişkenleri - GÜNCELLENMIŞ
    [Header("Game Data Tracking")]
    private int levelsCompleted = 0;
    private int timeoutFailures = 0; // Süre dolma başarısızlıkları
    private int currentScore = 0;
    private float gameStartTime;
    private int restartCount = 0; // Kaç kez yeniden başladı

    // SÜRE VERİSİ İÇİN YENİ EKLENENLER
    private float totalTimeSpent = 0f; // Toplam harcanan süre
    private float currentLevelTime = 0f; // Mevcut levelde harcanan süre
    private List<float> levelTimes = new List<float>(); // Her level için harcanan süreler
    private bool isFirstLevel = true;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentTime = gameTime;
        gameStartTime = Time.time; // Oyun başlangıç zamanını kaydet
        currentLevelTime = 0f; // Mevcut level süresini sıfırla

        // Eğer timerText atanmamışsa, otomatik olarak bul
        if (timerText == null)
        {
            timerText = GameObject.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
        }
        UpdateTimerDisplay();

        // Önceki level sürelerini yükle (PlayerPrefs'den)
        LoadPreviousTimes();

        // Debug log
        Debug.Log($"🎮 Maze oyunu başladı - Sahne: {SceneManager.GetActiveScene().name}");
        Debug.Log($"⏱️ Önceki toplam süre: {totalTimeSpent:F1} saniye");
    }

    private void Update()
    {
        if (gameActive)
        {
            // Zamanı azalt
            currentTime -= Time.deltaTime;
            currentLevelTime += Time.deltaTime; // Mevcut level süresini artır
            UpdateTimerDisplay();

            // Süre biterse oyunu yeniden başlat
            if (currentTime <= 0)
            {
                TimeUp();
            }
        }
    }

    private void FixedUpdate()
    {
        if (gameActive)
        {
            float yatay = Input.GetAxis("Horizontal"); // sağ-sol
            float dikey = Input.GetAxis("Vertical");   // ileri-geri
            Vector3 kuvvet = new Vector3(yatay, 0, dikey); // X,Z ekseninde hareket
            rb.AddForce(kuvvet * speed);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Finish") && gameActive)
        {
            gameActive = false;
            SaveCurrentLevelTime(); // Mevcut level süresini kaydet
            LevelCompleted();
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            // Süre azaldıkça renk değiştir (opsiyonel)
            if (currentTime <= 10)
            {
                timerText.color = Color.red;
            }
            else if (currentTime <= 30)
            {
                timerText.color = Color.yellow;
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }

    private void TimeUp()
    {
        gameActive = false;
        timeoutFailures++; // Süre dolma sayısını artır
        restartCount++; // Yeniden başlama sayısını artır

        // Süre dolduğunda da mevcut level süresini kaydet
        SaveCurrentLevelTime();

        Debug.Log($"⏰ Süre doldu! Toplam süre dolma: {timeoutFailures}, Yeniden başlama: {restartCount}");
        Debug.Log($"⏱️ Bu levelde harcanan süre: {currentLevelTime:F1} saniye");

        // GameDataSender'a bilgi gönder (opsiyonel)
        if (GameDataSender.Instance != null)
        {
            GameDataSender.Instance.AddWrongAnswer(); // Süre dolması bir hata olarak sayılabilir
        }

        // Süre verisini kaydet
        SaveTimeData();

        // Kısa bir bekleme süresi ekle, sonra sahneyi yeniden yükle
        StartCoroutine(RestartAfterDelay(1f));
    }

    private void LevelCompleted()
    {
        levelsCompleted++;

        // Skor hesaplama - kalan süreye göre bonus
        int timeBonus = Mathf.RoundToInt(currentTime * 10); // Her saniye 10 puan
        int levelBonus = 100; // Level tamamlama bonusu
        int currentLevelScore = levelBonus + timeBonus;
        currentScore += currentLevelScore;

        Debug.Log($"✅ Level tamamlandı! Toplam tamamlanan level: {levelsCompleted}");
        Debug.Log($"🏆 Bu level skoru: {currentLevelScore} (Level: {levelBonus} + Süre: {timeBonus})");
        Debug.Log($"📊 Toplam skor: {currentScore}");
        Debug.Log($"⏱️ Bu levelde harcanan süre: {currentLevelTime:F1} saniye");

        // GameDataSender'a bilgi gönder (opsiyonel)
        if (GameDataSender.Instance != null)
        {
            GameDataSender.Instance.AddCorrectAnswer(); // Level tamamlama
            GameDataSender.Instance.AddScore(currentLevelScore);
        }

        // Süre verisini kaydet
        SaveTimeData();

        string currentSceneName = SceneManager.GetActiveScene().name;

        // Hangi levelda olduğumuzu kontrol et ve uygun sahneye geç
        if (currentSceneName.Contains("Level 1") || currentSceneName.Contains("level 1") || currentSceneName.Contains("Maze") && !currentSceneName.Contains("NewMaze 2"))
        {
            // Level 1 tamamlandı, Level 2'ye geç
            Debug.Log("Level 1 tamamlandı! Level 2'ye geçiliyor...");
            SceneManager.LoadScene("NewMaze 2");
        }
        else if (currentSceneName == "NewMaze 2")
        {
            // Level 2 tamamlandı, ana sahneye dön
            Debug.Log("Level 2 tamamlandı! Ana sahneye dönülüyor...");
            SceneManager.LoadScene("SampleScene");
        }
        else
        {
            // Varsayılan durumda ana sahneye dön
            Debug.Log("Oyun tamamlandı! Ana sahneye dönülüyor...");
            SceneManager.LoadScene("SampleScene");
        }
    }

    private IEnumerator RestartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ===========================================
    // SÜRE VERİSİ YÖNETİMİ - YENİ METODLAR
    // ===========================================

    private void SaveCurrentLevelTime()
    {
        // Mevcut level süresini listeye ekle
        levelTimes.Add(currentLevelTime);
        totalTimeSpent += currentLevelTime;

        Debug.Log($"⏱️ Level süresi kaydedildi: {currentLevelTime:F1} saniye, Toplam: {totalTimeSpent:F1} saniye");
    }

    private void SaveTimeData()
    {
        // PlayerPrefs ile süre verilerini kaydet
        PlayerPrefs.SetFloat("MazeTotalTime", totalTimeSpent);
        PlayerPrefs.SetInt("MazeLevelsCompleted", levelsCompleted);
        PlayerPrefs.SetInt("MazeTimeouts", timeoutFailures);
        PlayerPrefs.SetInt("MazeScore", currentScore);
        PlayerPrefs.SetInt("MazeRestarts", restartCount);

        // Level sürelerini string olarak kaydet
        string levelTimesString = string.Join(",", levelTimes);
        PlayerPrefs.SetString("MazeLevelTimes", levelTimesString);

        PlayerPrefs.Save();
    }

    private void LoadPreviousTimes()
    {
        // Önceki oyun verilerini yükle
        totalTimeSpent = PlayerPrefs.GetFloat("MazeTotalTime", 0f);
        levelsCompleted = PlayerPrefs.GetInt("MazeLevelsCompleted", 0);
        timeoutFailures = PlayerPrefs.GetInt("MazeTimeouts", 0);
        currentScore = PlayerPrefs.GetInt("MazeScore", 0);
        restartCount = PlayerPrefs.GetInt("MazeRestarts", 0);

        // Level sürelerini yükle
        string levelTimesString = PlayerPrefs.GetString("MazeLevelTimes", "");
        if (!string.IsNullOrEmpty(levelTimesString))
        {
            string[] timeStrings = levelTimesString.Split(',');
            levelTimes.Clear();
            foreach (string timeString in timeStrings)
            {
                if (float.TryParse(timeString, out float time))
                {
                    levelTimes.Add(time);
                }
            }
        }
    }

    // ===========================================
    // IGameDataProvider Implementation - GÜNCELLENMİŞ
    // ===========================================

    public int GetCorrectAnswers()
    {
        return levelsCompleted; // Tamamlanan level sayısı
    }

    public int GetWrongAnswers()
    {
        return timeoutFailures + restartCount; // Süre dolması + yeniden başlama sayısı
    }

    public int GetScore()
    {
        return currentScore; // Hesaplanan toplam skor
    }

    public float GetTimeSpent()
    {
        // Toplam harcanan süre + mevcut level süresi
        return totalTimeSpent + currentLevelTime;
    }

    // ===========================================
    // EK VERİ METODLARI - YENİ EKLENEN
    // ===========================================

    public float GetCurrentLevelTime()
    {
        return currentLevelTime;
    }

    public float GetTotalGameTime()
    {
        return totalTimeSpent;
    }

    public List<float> GetLevelTimes()
    {
        return new List<float>(levelTimes); // Kopya döndür
    }

    public float GetAverageLevelTime()
    {
        if (levelTimes.Count == 0) return 0f;
        float total = 0f;
        foreach (float time in levelTimes)
        {
            total += time;
        }
        return total / levelTimes.Count;
    }

    public float GetRemainingTime()
    {
        return currentTime;
    }

    // ===========================================
    // Public metodlar - isteğe bağlı debug için - GÜNCELLENMİŞ
    // ===========================================

    public void ShowCurrentStats()
    {
        Debug.Log($"📊 Maze Oyunu İstatistikleri:");
        Debug.Log($"   • Tamamlanan Level: {levelsCompleted}");
        Debug.Log($"   • Süre Dolması: {timeoutFailures}");
        Debug.Log($"   • Yeniden Başlama: {restartCount}");
        Debug.Log($"   • Toplam Skor: {currentScore}");
        Debug.Log($"   • Toplam Geçen Süre: {GetTimeSpent():F1} saniye");
        Debug.Log($"   • Mevcut Level Süresi: {currentLevelTime:F1} saniye");
        Debug.Log($"   • Ortalama Level Süresi: {GetAverageLevelTime():F1} saniye");
        Debug.Log($"   • Kalan Süre: {currentTime:F1} saniye");

        // Her level süresini göster
        for (int i = 0; i < levelTimes.Count; i++)
        {
            Debug.Log($"   • Level {i + 1} Süresi: {levelTimes[i]:F1} saniye");
        }
    }

    // Inspector'dan test etmek için
    [ContextMenu("İstatistikleri Göster")]
    void TestShowStats()
    {
        ShowCurrentStats();
    }

    [ContextMenu("Süre Verilerini Sıfırla")]
    void ResetTimeData()
    {
        PlayerPrefs.DeleteKey("MazeTotalTime");
        PlayerPrefs.DeleteKey("MazeLevelsCompleted");
        PlayerPrefs.DeleteKey("MazeTimeouts");
        PlayerPrefs.DeleteKey("MazeScore");
        PlayerPrefs.DeleteKey("MazeRestarts");
        PlayerPrefs.DeleteKey("MazeLevelTimes");
        PlayerPrefs.Save();

        totalTimeSpent = 0f;
        currentLevelTime = 0f;
        levelsCompleted = 0;
        timeoutFailures = 0;
        currentScore = 0;
        restartCount = 0;
        levelTimes.Clear();

        Debug.Log("🔄 Tüm süre verileri sıfırlandı!");
    }

    // GameDataSender manuel test için
    [ContextMenu("Veri Gönder (Test)")]
    void TestSendData()
    {
        if (GameDataSender.Instance != null)
        {
            GameDataSender.Instance.SendSessionData();
            Debug.Log("📤 Test verisi gönderildi!");
        }
        else
        {
            Debug.LogWarning("⚠️ GameDataSender bulunamadı!");
        }
    }

    // Oyundan çıkarken verileri kaydet
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveCurrentLevelTime();
            SaveTimeData();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveCurrentLevelTime();
            SaveTimeData();
        }
    }

    void OnDestroy()
    {
        // Script yok edilirken son verileri kaydet
        if (gameActive)
        {
            SaveCurrentLevelTime();
            SaveTimeData();
        }
    }
}