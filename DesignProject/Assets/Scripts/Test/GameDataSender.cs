using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;

public class GameDataSender : MonoBehaviour
{
    [Header("API Ayarları")]
    public string apiUrl = "https://admin-dashboard-git-main-hacerkilic01s-projects.vercel.app/api/game-result ";
    public bool useLocalTest = true; // Test için yerel sunucu kullan
    public string localTestUrl = "https://admin-dashboard-git-main-hacerkilic01s-projects.vercel.app/api/game-result ";

    [Header("Bağlantı Ayarları")]
    public int connectionTimeout = 30;
    public int maxRetryAttempts = 3;
    public float retryDelay = 5f;

    [Header("Oyuncu Bilgileri")]
    public string playerName = "Player1";

    [Header("Otomatik Gönderim Ayarları")]
    public bool autoSendOnInterval = false;
    public float sendInterval = 300f;
    public bool sendOnSceneChange = true;
    public bool sendOnGameEnd = true;

    [Header("Offline Depolama")]
    public bool saveOfflineData = true;
    public int maxOfflineDataCount = 100;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Singleton pattern
    public static GameDataSender Instance { get; private set; }

    [System.Serializable]
    public class SceneData
    {
        public string sceneName;
        public int correctAnswers;
        public int wrongAnswers;
        public float timeSpent;
        public int score;
        public DateTime sceneStartTime;
        public DateTime sceneEndTime;
        public bool completed;

        public SceneData(string name)
        {
            sceneName = name;
            correctAnswers = 0;
            wrongAnswers = 0;
            timeSpent = 0f;
            score = 0;
            sceneStartTime = DateTime.Now;
            sceneEndTime = DateTime.Now;
            completed = false;
        }
    }

    [System.Serializable]
    public class GameSessionData
    {
        public string playerName;
        public string sessionId;
        public DateTime sessionStartTime;
        public DateTime sessionEndTime;
        public float totalGameTime;
        public int totalCorrectAnswers;
        public int totalWrongAnswers;
        public int totalScore;
        public float overallAccuracy;
        public string gameVersion;
        public List<SceneData> sceneDataList;
        public bool sessionCompleted;

        public GameSessionData(string name)
        {
            playerName = name;
            sessionId = System.Guid.NewGuid().ToString();
            sessionStartTime = DateTime.Now;
            sessionEndTime = DateTime.Now;
            totalGameTime = 0f;
            totalCorrectAnswers = 0;
            totalWrongAnswers = 0;
            totalScore = 0;
            overallAccuracy = 0f;
            gameVersion = Application.version;
            sceneDataList = new List<SceneData>();
            sessionCompleted = false;
        }

        public void CalculateTotals()
        {
            totalCorrectAnswers = 0;
            totalWrongAnswers = 0;
            totalScore = 0;
            totalGameTime = 0f;

            foreach (var sceneData in sceneDataList)
            {
                totalCorrectAnswers += sceneData.correctAnswers;
                totalWrongAnswers += sceneData.wrongAnswers;
                totalScore += sceneData.score;
                totalGameTime += sceneData.timeSpent;
            }

            int totalQuestions = totalCorrectAnswers + totalWrongAnswers;
            overallAccuracy = totalQuestions > 0 ? (float)totalCorrectAnswers / totalQuestions * 100f : 0f;
            sessionEndTime = DateTime.Now;
        }
    }

    // Oyun oturumu verisi
    private GameSessionData currentSession;
    private SceneData currentSceneData;
    private float sceneStartTime;
    private float sessionStartTime;
    private bool isSending = false;
    private string lastSceneName = "";
    private int currentRetryAttempt = 0;

    // Offline data storage
    private List<GameSessionData> offlineDataQueue = new List<GameSessionData>();

    // Desteklenen sahne isimleri
    [Header("Sahne İsimleri")]
    public List<string> gameScenes = new List<string>
    {
         "MathGame",
         "NewMaze",
         "PuzzleGame",
         "NewTower",
         "WhackAMole"
    };

    // Her sahne türü için GameManager benzeri component'ler
    private Dictionary<string, IGameDataProvider> gameDataProviders = new Dictionary<string, IGameDataProvider>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSession();
            LoadOfflineData();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            if (showDebugLogs)
                Debug.Log("🎮 GameDataSender singleton oluşturuldu");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = PlayerPrefs.GetString("PlayerName", "Player" + UnityEngine.Random.Range(1000, 9999));
        }

        // Bağlantı kontrolü yap
        StartCoroutine(CheckConnectionAndSendOfflineData());

        if (showDebugLogs)
            Debug.Log($"🎮 GameDataSender başlatıldı. Oyuncu: {playerName}");
    }

    void Update()
    {
        UpdateCurrentSceneData();

        if (autoSendOnInterval && Time.time - sessionStartTime >= sendInterval)
        {
            SendSessionData();
        }

        // Test kontrolleri
        if (Input.GetKeyDown(KeyCode.S))
        {
            SendSessionData();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            CompleteSessionAndSend();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            ShowCurrentStats();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            TestConnection();
        }
    }

    // Bağlantı testi
    [ContextMenu("Test Connection")]
    public void TestConnection()
    {
        StartCoroutine(TestConnectionCoroutine());
    }

    IEnumerator TestConnectionCoroutine()
    {
        string testUrl = useLocalTest ? localTestUrl : apiUrl;

        if (showDebugLogs)
            Debug.Log($"🔍 Bağlantı test ediliyor: {testUrl}");

        using (UnityWebRequest request = UnityWebRequest.Get(testUrl))
        {
            request.timeout = connectionTimeout;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (showDebugLogs)
                    Debug.Log("✅ Bağlantı başarılı!");
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.LogError($"❌ Bağlantı hatası: {request.error}");
                    Debug.LogError($"Response Code: {request.responseCode}");
                    Debug.LogError("Çözüm önerileri kontrol ediliyor...");
                }
                LogConnectionSolutions(request.error, request.responseCode);
            }
        }
    }

    // Bağlantı hatası çözüm önerileri
    void LogConnectionSolutions(string error, long responseCode)
    {
        Debug.LogWarning("🔧 BAĞLANTI HATASI ÇÖZÜMLERİ:");

        if (error.Contains("Cannot connect to destination host"))
        {
            Debug.LogWarning("1. API URL'ini kontrol edin: " + (useLocalTest ? localTestUrl : apiUrl));
            Debug.LogWarning("2. Sunucu çalışıyor mu kontrol edin");
            Debug.LogWarning("3. Firewall/Antivirus Unity'yi engelliyor olabilir");
            Debug.LogWarning("4. İnternet bağlantınızı kontrol edin");
            Debug.LogWarning("5. Test için useLocalTest = true yapın ve yerel sunucu çalıştırın");
        }

        if (responseCode == 0)
        {
            Debug.LogWarning("6. DNS sorunu olabilir - IP adresi deneyin");
            Debug.LogWarning("7. Port numarası doğru mu kontrol edin");
        }

        Debug.LogWarning("8. Offline moda geçmek için saveOfflineData = true yapın");
    }

    // Bağlantı kontrolü ve offline veri gönderimi
    IEnumerator CheckConnectionAndSendOfflineData()
    {
        yield return new WaitForSeconds(2f); // Başlangıçta bekle

        string testUrl = useLocalTest ? localTestUrl : apiUrl;

        using (UnityWebRequest request = UnityWebRequest.Get(testUrl))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (showDebugLogs)
                    Debug.Log("🌐 İnternet bağlantısı aktif - offline veriler gönderiliyor");

                // Offline verileri gönder
                if (saveOfflineData && offlineDataQueue.Count > 0)
                {
                    StartCoroutine(SendOfflineDataQueue());
                }
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning("📱 Offline modda çalışılıyor - veriler yerel olarak saklanacak");
            }
        }
    }

    // Offline veri kuyruğunu gönder
    IEnumerator SendOfflineDataQueue()
    {
        while (offlineDataQueue.Count > 0)
        {
            var sessionData = offlineDataQueue[0];
            bool success = false;

            yield return StartCoroutine(SendSingleSessionData(sessionData, (result) => success = result));

            if (success)
            {
                offlineDataQueue.RemoveAt(0);
                SaveOfflineData(); // Güncel listeyi kaydet
                if (showDebugLogs)
                    Debug.Log($"✅ Offline veri gönderildi. Kalan: {offlineDataQueue.Count}");
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning("❌ Offline veri gönderilemedi, daha sonra tekrar denenecek");
                break;
            }

            yield return new WaitForSeconds(1f); // Sunucuya yük vermemek için
        }
    }

    void InitializeSession()
    {
        currentSession = new GameSessionData(playerName);
        sessionStartTime = Time.time;

        if (showDebugLogs)
            Debug.Log($"📊 Yeni oyun oturumu başlatıldı: {currentSession.sessionId}");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (showDebugLogs)
            Debug.Log($"🏞️ Sahne yüklendi: {scene.name}");

        if (currentSceneData != null && !string.IsNullOrEmpty(lastSceneName))
        {
            FinishCurrentScene();
        }

        if (IsGameScene(scene.name))
        {
            StartNewScene(scene.name);
        }

        lastSceneName = scene.name;
    }

    void OnSceneUnloaded(Scene scene)
    {
        if (showDebugLogs)
            Debug.Log($"🏞️ Sahne boşaltıldı: {scene.name}");

        if (IsGameScene(scene.name) && sendOnSceneChange)
        {
            SendSessionData();
        }
    }

    void StartNewScene(string sceneName)
    {
        currentSceneData = new SceneData(sceneName);
        sceneStartTime = Time.time;
        StartCoroutine(FindDataProviderWithDelay(sceneName));

        if (showDebugLogs)
            Debug.Log($"📊 Yeni sahne verisi başlatıldı: {sceneName}");
    }

    void FinishCurrentScene()
    {
        if (currentSceneData == null) return;

        currentSceneData.timeSpent = Time.time - sceneStartTime;
        currentSceneData.sceneEndTime = DateTime.Now;
        currentSceneData.completed = true;

        UpdateCurrentSceneData();

        var existingScene = currentSession.sceneDataList.Find(s => s.sceneName == currentSceneData.sceneName);
        if (existingScene != null)
        {
            currentSession.sceneDataList.Remove(existingScene);
        }

        currentSession.sceneDataList.Add(currentSceneData);

        if (showDebugLogs)
            Debug.Log($"✅ Sahne tamamlandı: {currentSceneData.sceneName} - Doğru: {currentSceneData.correctAnswers}, Yanlış: {currentSceneData.wrongAnswers}, Skor: {currentSceneData.score}");
    }


    IEnumerator FindDataProviderWithDelay(string sceneName)
    {
        yield return new WaitForSeconds(0.2f);

        IGameDataProvider provider = null;

        switch (sceneName)
        {
            case "MathGame":
                var mathGameManager = FindObjectOfType<GameManager>();
                if (mathGameManager != null)
                {
                    var dataProviderComponent = mathGameManager.GetComponent<IGameDataProvider>();
                    if (dataProviderComponent != null)
                    {
                        provider = dataProviderComponent;
                    }
                    else
                    {
                        provider = new GameManagerAdapter(mathGameManager);
                    }
                }
                break;

            case "PuzzleGame":
                var puzzleGameManager = FindObjectOfType<PuzzleGameManager>();
                if (puzzleGameManager != null)
                {
                    provider = puzzleGameManager; // PuzzleGameManager IGameDataProvider implement ediyor
                    if (showDebugLogs)
                        Debug.Log("✅ PuzzleGameManager found and connected!");
                }
                break;

            case "NewMaze":
                // Maze oyunu için provider kodları
                break;

            case "NewTower":
                // Tower oyunu için provider kodları
                break;

            case "WhackAMole":
                var moleManager = FindObjectOfType<MoleManager>();
                if (moleManager != null)
                {
                    provider = moleManager; // MoleManager IGameDataProvider implement ediyor
                    if (showDebugLogs)
                        Debug.Log("✅ MoleManager found and connected!");
                }
                break;

            default:
                // Genel arama - IGameDataProvider implement eden component'leri bul
                var allComponents = FindObjectsOfType<MonoBehaviour>();
                foreach (var component in allComponents)
                {
                    if (component is IGameDataProvider dataProvider)
                    {
                        provider = dataProvider;
                        break;
                    }
                }
                break;
        }

        if (provider != null)
        {
            gameDataProviders[sceneName] = provider;
            if (showDebugLogs)
                Debug.Log($"✅ {sceneName} için data provider bulundu: {provider.GetType().Name}");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ {sceneName} için data provider bulunamadı!");
        }
    }
    void UpdateCurrentSceneData()
    {
        if (currentSceneData == null || string.IsNullOrEmpty(currentSceneData.sceneName)) return;

        if (gameDataProviders.ContainsKey(currentSceneData.sceneName))
        {
            var provider = gameDataProviders[currentSceneData.sceneName];
            if (provider != null)
            {
                currentSceneData.correctAnswers = provider.GetCorrectAnswers();
                currentSceneData.wrongAnswers = provider.GetWrongAnswers();
                currentSceneData.score = provider.GetScore();
            }
        }
    }

    bool IsGameScene(string sceneName)
    {
        return gameScenes.Contains(sceneName);
    }

    public void SendSessionData()
    {
        if (isSending) return;
        StartCoroutine(SendSessionDataCoroutine());
    }

    public void CompleteSessionAndSend()
    {
        if (currentSceneData != null)
        {
            FinishCurrentScene();
        }
        currentSession.sessionCompleted = true;
        SendSessionData();
    }

    IEnumerator SendSessionDataCoroutine()
    {
        if (isSending) yield break;
        isSending = true;
        currentRetryAttempt = 0;

        bool success = false;
        yield return StartCoroutine(SendWithRetry((result) => success = result));

        if (!success && saveOfflineData)
        {
            SaveDataOffline();
        }

        isSending = false;
    }

    IEnumerator SendWithRetry(System.Action<bool> callback)
    {
        while (currentRetryAttempt < maxRetryAttempts)
        {
            bool success = false;
            yield return StartCoroutine(SendSingleSessionData(currentSession, (result) => success = result));

            if (success)
            {
                callback(true);
                yield break;
            }

            currentRetryAttempt++;
            if (currentRetryAttempt < maxRetryAttempts)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"⏳ Tekrar deneniyor... ({currentRetryAttempt}/{maxRetryAttempts})");
                yield return new WaitForSeconds(retryDelay);
            }
        }

        callback(false);
    }

    IEnumerator SendSingleSessionData(GameSessionData sessionData, System.Action<bool> callback)
    {
        if (currentSceneData != null)
        {
            UpdateCurrentSceneData();
        }

        sessionData.CalculateTotals();
        string jsonData = JsonUtility.ToJson(sessionData, true);

        if (showDebugLogs)
        {
            Debug.Log("📤 Gönderilecek oturum verisi:");
            Debug.Log($"Toplam Doğru: {sessionData.totalCorrectAnswers}, Toplam Yanlış: {sessionData.totalWrongAnswers}");
            Debug.Log($"JSON Boyutu: {jsonData.Length} karakter");
        }

        string targetUrl = useLocalTest ? localTestUrl : apiUrl;

        using (UnityWebRequest request = new UnityWebRequest(targetUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.timeout = connectionTimeout;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (showDebugLogs)
                {
                    Debug.Log("✅ Oturum verisi başarıyla gönderildi!");
                    Debug.Log("Server yanıtı: " + request.downloadHandler.text);
                }
                callback(true);
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.LogError("❌ Veri gönderme hatası: " + request.error);
                    Debug.LogError("Response Code: " + request.responseCode);
                }
                LogConnectionSolutions(request.error, request.responseCode);
                callback(false);
            }
        }
    }

    void SaveDataOffline()
    {
        if (currentSceneData != null)
        {
            UpdateCurrentSceneData();
        }

        currentSession.CalculateTotals();

        offlineDataQueue.Add(currentSession);

        // Maksimum offline veri sayısını kontrol et
        while (offlineDataQueue.Count > maxOfflineDataCount)
        {
            offlineDataQueue.RemoveAt(0);
        }

        SaveOfflineData();

        if (showDebugLogs)
            Debug.Log($"💾 Veri offline olarak kaydedildi. Kuyruk: {offlineDataQueue.Count}");
    }

    void SaveOfflineData()
    {
        try
        {
            string json = JsonUtility.ToJson(new OfflineDataWrapper { sessions = offlineDataQueue }, true);
            PlayerPrefs.SetString("OfflineGameData", json);
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            if (showDebugLogs)
                Debug.LogError("Offline veri kaydetme hatası: " + e.Message);
        }
    }

    void LoadOfflineData()
    {
        try
        {
            string json = PlayerPrefs.GetString("OfflineGameData", "");
            if (!string.IsNullOrEmpty(json))
            {
                var wrapper = JsonUtility.FromJson<OfflineDataWrapper>(json);
                if (wrapper != null && wrapper.sessions != null)
                {
                    offlineDataQueue = wrapper.sessions;
                    if (showDebugLogs)
                        Debug.Log($"💾 Offline veri yüklendi: {offlineDataQueue.Count} oturum");
                }
            }
        }
        catch (System.Exception e)
        {
            if (showDebugLogs)
                Debug.LogError("Offline veri yükleme hatası: " + e.Message);
            offlineDataQueue = new List<GameSessionData>();
        }
    }

    [System.Serializable]
    public class OfflineDataWrapper
    {
        public List<GameSessionData> sessions;
    }

    // Public API metodlar
    public void SetPlayerName(string newName)
    {
        playerName = newName;
        currentSession.playerName = newName;
        PlayerPrefs.SetString("PlayerName", playerName);
    }

    public void AddScore(int points)
    {
        if (currentSceneData != null)
        {
            currentSceneData.score += points;
        }
    }

    public void AddCorrectAnswer(int count = 1)
    {
        if (currentSceneData != null)
        {
            currentSceneData.correctAnswers += count;
        }
    }

    public void AddWrongAnswer(int count = 1)
    {
        if (currentSceneData != null)
        {
            currentSceneData.wrongAnswers += count;
        }
    }

    public void ShowCurrentStats()
    {
        if (currentSceneData != null)
        {
            float currentTime = Time.time - sceneStartTime;
            Debug.Log($"📊 Mevcut Sahne ({currentSceneData.sceneName}): Doğru: {currentSceneData.correctAnswers}, Yanlış: {currentSceneData.wrongAnswers}, Skor: {currentSceneData.score}, Süre: {currentTime:F1}s");
        }

        currentSession.CalculateTotals();
        Debug.Log($"📊 Toplam Oturum: Doğru: {currentSession.totalCorrectAnswers}, Yanlış: {currentSession.totalWrongAnswers}, Skor: {currentSession.totalScore}");

        if (offlineDataQueue.Count > 0)
        {
            Debug.Log($"💾 Offline Kuyruk: {offlineDataQueue.Count} oturum bekliyor");
        }
    }

    public string GetSessionId()
    {
        return currentSession?.sessionId ?? "";
    }

    public bool IsCurrentlySending()
    {
        return isSending;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        if (sendOnGameEnd)
        {
            CompleteSessionAndSend();
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && sendOnGameEnd)
        {
            SendSessionData();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && sendOnGameEnd)
        {
            SendSessionData();
        }
    }

    // Inspector test metodları
    [ContextMenu("Test - Oturum Verisi Gönder")]
    void TestSendSession()
    {
        SendSessionData();
    }

    [ContextMenu("Test - İstatistikleri Göster")]
    void TestShowStats()
    {
        ShowCurrentStats();
    }

    [ContextMenu("Test - Oturumu Tamamla")]
    void TestCompleteSession()
    {
        CompleteSessionAndSend();
    }

    [ContextMenu("Test - Offline Verileri Gönder")]
    void TestSendOfflineData()
    {
        StartCoroutine(SendOfflineDataQueue());
    }
}

// Data provider interface
public interface IGameDataProvider
{
    int GetCorrectAnswers();
    int GetWrongAnswers();
    int GetScore();
    float GetTimeSpent();
}

// Mevcut GameManager için adapter
public class GameManagerAdapter : IGameDataProvider
{
    private GameManager gameManager;

    public GameManagerAdapter(GameManager gm)
    {
        gameManager = gm;
    }

    public int GetCorrectAnswers()
    {
        return gameManager?.GetCorrectCount() ?? 0;
    }

    public int GetWrongAnswers()
    {
        return gameManager?.GetWrongCount() ?? 0;
    }

    public int GetScore()
    {
        return (gameManager?.GetCorrectCount() ?? 0) * 10;
    }

    public float GetTimeSpent()
    {
        return 0f;
    }
}