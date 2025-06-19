using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using System.Linq;

public class GameDataSender : MonoBehaviour
{
    [Header("API Ayarları")]
    public string apiUrl = "https://webhook.site/70151d05-335f-46dc-a120-f516cd912e1e";
    public bool useLocalTest = true;
    public string localTestUrl = "https://webhook.site/70151d05-335f-46dc-a120-f516cd912e1et";

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

    [System.Serializable]
    public class OfflineDataWrapper
    {
        public List<GameSessionData> sessions;
    }

    // Private variables
    private GameSessionData currentSession;
    private SceneData currentSceneData;
    private float sceneStartTime;
    private float sessionStartTime;
    private bool isSending = false;
    private string lastSceneName = "";
    private int currentRetryAttempt = 0;
    private List<GameSessionData> offlineDataQueue = new List<GameSessionData>();

    [Header("Sahne İsimleri")]
    public List<string> gameScenes = new List<string>
    {
         "MathGame",
         "NewMaze",
         "PuzzleGame",
         "NewTower",
         "WhackAMole"
    };

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
        StartCoroutine(CheckConnectionAndSendOfflineData());
        if (showDebugLogs)
            Debug.Log($"🎮 GameDataSender başlatıldı. Oyuncu: {playerName}");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            CompleteSessionAndSend();
        }

        if (currentSceneData != null && IsGameScene(SceneManager.GetActiveScene().name))
        {
            UpdateCurrentSceneData();
        }

        if (autoSendOnInterval && Time.time - sessionStartTime >= sendInterval)
        {
            SendSessionData();
        }

        // Test controls
        if (Input.GetKeyDown(KeyCode.S)) SendSessionData();
        if (Input.GetKeyDown(KeyCode.R)) CompleteSessionAndSend();
        if (Input.GetKeyDown(KeyCode.I)) ShowCurrentStats();
        if (Input.GetKeyDown(KeyCode.T)) TestConnection();
        if (Input.GetKeyDown(KeyCode.P)) TestCurrentProvider();
    }

    IEnumerator CheckConnectionAndSendOfflineData()
    {
        yield return new WaitForSeconds(2f);
        string testUrl = useLocalTest ? localTestUrl : apiUrl;

        using (UnityWebRequest request = UnityWebRequest.Get(testUrl))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (showDebugLogs)
                    Debug.Log("🌐 İnternet bağlantısı aktif - offline veriler gönderiliyor");
                if (saveOfflineData && offlineDataQueue.Count > 0)
                {
                    StartCoroutine(SendOfflineDataQueue());
                }
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning("📱 Offline modda çalışılıyor");
            }
        }
    }

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
                SaveOfflineData();
                if (showDebugLogs)
                    Debug.Log($"✅ Offline veri gönderildi. Kalan: {offlineDataQueue.Count}");
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning("❌ Offline veri gönderilemedi");
                break;
            }
            yield return new WaitForSeconds(1f);
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

        if (currentSceneData != null && !string.IsNullOrEmpty(lastSceneName) && lastSceneName != scene.name)
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
        if (showDebugLogs)
            Debug.Log($"🆕 Yeni sahne başlatıldı: {sceneName}");
        StartCoroutine(FindDataProviderWithDelay(sceneName));
    }

    void FinishCurrentScene()
    {
        if (currentSceneData == null) return;

        UpdateCurrentSceneData();
        currentSceneData.timeSpent = Time.time - sceneStartTime;
        currentSceneData.sceneEndTime = DateTime.Now;
        currentSceneData.completed = true;

        var existingScene = currentSession.sceneDataList.Find(s => s.sceneName == currentSceneData.sceneName);
        if (existingScene != null)
        {
            currentSession.sceneDataList.Remove(existingScene);
        }

        currentSession.sceneDataList.Add(currentSceneData);

        if (showDebugLogs)
        {
            Debug.Log($"✅ Sahne tamamlandı: {currentSceneData.sceneName}");
            Debug.Log($"📊 Doğru: {currentSceneData.correctAnswers}, Yanlış: {currentSceneData.wrongAnswers}, Skor: {currentSceneData.score}");
        }
        currentSceneData = null;
    }

    IEnumerator FindDataProviderWithDelay(string sceneName)
    {
        yield return new WaitForSeconds(0.5f);
        IGameDataProvider provider = null;

        var gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            provider = gameManager;
            if (showDebugLogs)
                Debug.Log($"✅ GameManager found in scene {sceneName}");
        }

        if (provider == null)
        {
            switch (sceneName)
            {
                case "PuzzleGame":
                    var puzzleManager = FindObjectOfType<PuzzleGameManager>();
                    if (puzzleManager != null) provider = puzzleManager;
                    break;
                case "WhackAMole":
                    var moleManager = FindObjectOfType<MoleManager>();
                    if (moleManager != null) provider = moleManager;
                    break;
               
            }
        }

        if (provider == null)
        {
            var allComponents = FindObjectsOfType<MonoBehaviour>();
            foreach (var component in allComponents)
            {
                if (component is IGameDataProvider dataProvider)
                {
                    provider = dataProvider;
                    break;
                }
            }
        }

        if (provider != null)
        {
            gameDataProviders[sceneName] = provider;
            if (showDebugLogs)
                Debug.Log($"✅ {sceneName} data provider kaydedildi");
            UpdateCurrentSceneData();
        }
        else
        {
            if (showDebugLogs)
                Debug.LogError($"❌ {sceneName} için data provider bulunamadı!");
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
                currentSceneData.timeSpent = provider.GetTimeSpent();
            }
        }
    }

    bool IsGameScene(string sceneName)
    {
        return gameScenes.Contains(sceneName);
    }

    public void SendSessionData()
    {
        if (showDebugLogs)
            Debug.Log("📤 SendSessionData çağrıldı");

        if (currentSceneData != null)
        {
            UpdateCurrentSceneData();
        }

        if (isSending)
        {
            if (showDebugLogs)
                Debug.LogWarning("⚠️ Zaten gönderim devam ediyor");
            return;
        }

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

        string jsonData = CreateApiCompatibleJson(sessionData);

        if (showDebugLogs)
        {
            Debug.Log("=== API SEND ===");
            Debug.Log($"📤 JSON ({jsonData.Length} chars):");
            Debug.Log(jsonData);
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

            if (showDebugLogs)
            {
                Debug.Log("=== API RESPONSE ===");
                Debug.Log($"Response Code: {request.responseCode}");
                Debug.Log($"Error: {request.error}");
                Debug.Log($"Response: {request.downloadHandler.text}");
            }

            if (request.responseCode == 200 || request.responseCode == 201)
            {
                if (showDebugLogs)
                    Debug.Log("✅ Veri başarıyla gönderildi!");
                callback(true);
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.LogError($"❌ API hatası: {request.error}");
                    Debug.LogError($"Server: {request.downloadHandler.text}");
                }
                callback(false);
            }
        }
    }

    private string CreateApiCompatibleJson(GameSessionData sessionData)
    {
        if (sessionData == null)
            throw new System.Exception("SessionData null!");

        sessionData.CalculateTotals();

        string playerName = sessionData.playerName ?? "Unknown";
        string sessionId = sessionData.sessionId ?? System.Guid.NewGuid().ToString();
        string gameVersion = sessionData.gameVersion ?? "1.0";

        System.DateTime startTime = sessionData.sessionStartTime;
        System.DateTime endTime = sessionData.sessionEndTime;

        if (startTime == default(System.DateTime))
            startTime = System.DateTime.UtcNow.AddMinutes(-5);
        if (endTime == default(System.DateTime))
            endTime = System.DateTime.UtcNow;

        var sceneList = sessionData.sceneDataList ?? new List<SceneData>();
        var sceneJsons = new List<string>();

        foreach (var scene in sceneList)
        {
            if (scene == null) continue;

            string sceneName = scene.sceneName ?? "Unknown";
            float timeSpent = float.IsNaN(scene.timeSpent) || float.IsInfinity(scene.timeSpent) ? 0f : scene.timeSpent;

            System.DateTime sceneStart = scene.sceneStartTime;
            System.DateTime sceneEnd = scene.sceneEndTime;

            if (sceneStart == default(System.DateTime)) sceneStart = startTime;
            if (sceneEnd == default(System.DateTime)) sceneEnd = endTime;

            string sceneJson = string.Format(@"{{
    ""sceneName"": ""{0}"",
    ""correctAnswers"": {1},
    ""wrongAnswers"": {2},
    ""timeSpent"": {3},
    ""score"": {4},
    ""sceneStartTime"": ""{5}"",
    ""sceneEndTime"": ""{6}"",
    ""completed"": {7}
  }}",
                sceneName,
                scene.correctAnswers,
                scene.wrongAnswers,
                timeSpent.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                scene.score,
                sceneStart.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                sceneEnd.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                scene.completed.ToString().ToLower()
            );
            sceneJsons.Add(sceneJson);
        }

        string sceneArray = "[" + string.Join(",", sceneJsons.ToArray()) + "]";

        float totalGameTime = float.IsNaN(sessionData.totalGameTime) || float.IsInfinity(sessionData.totalGameTime) ? 0f : sessionData.totalGameTime;
        float overallAccuracy = float.IsNaN(sessionData.overallAccuracy) || float.IsInfinity(sessionData.overallAccuracy) ? 0f : sessionData.overallAccuracy;

        string mainJson = string.Format(@"{{
  ""playerName"": ""{0}"",
  ""sessionId"": ""{1}"",
  ""sessionStartTime"": ""{2}"",
  ""sessionEndTime"": ""{3}"",
  ""totalGameTime"": {4},
  ""totalCorrectAnswers"": {5},
  ""totalWrongAnswers"": {6},
  ""totalScore"": {7},
  ""overallAccuracy"": {8},
  ""gameVersion"": ""{9}"",
  ""sessionCompleted"": {10},
  ""sceneDataList"": {11}
}}",
            playerName,
            sessionId,
            startTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            endTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            totalGameTime.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            sessionData.totalCorrectAnswers,
            sessionData.totalWrongAnswers,
            sessionData.totalScore,
            overallAccuracy.ToString("F6", System.Globalization.CultureInfo.InvariantCulture),
            gameVersion,
            sessionData.sessionCompleted.ToString().ToLower(),
            sceneArray
        );

        return mainJson;
    }

    void SaveDataOffline()
    {
        if (currentSceneData != null)
            UpdateCurrentSceneData();

        currentSession.CalculateTotals();
        offlineDataQueue.Add(currentSession);

        while (offlineDataQueue.Count > maxOfflineDataCount)
        {
            offlineDataQueue.RemoveAt(0);
        }

        SaveOfflineData();
        if (showDebugLogs)
            Debug.Log($"💾 Offline kaydedildi. Kuyruk: {offlineDataQueue.Count}");
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
                Debug.LogError("Offline kaydetme hatası: " + e.Message);
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
                        Debug.Log($"💾 Offline veri yüklendi: {offlineDataQueue.Count}");
                }
            }
        }
        catch (System.Exception e)
        {
            if (showDebugLogs)
                Debug.LogError("Offline yükleme hatası: " + e.Message);
            offlineDataQueue = new List<GameSessionData>();
        }
    }

    // Public API methods
    public void SetPlayerName(string newName)
    {
        playerName = newName;
        currentSession.playerName = newName;
        PlayerPrefs.SetString("PlayerName", playerName);
    }

    public void AddScore(int points)
    {
        if (currentSceneData != null)
            currentSceneData.score += points;
    }

    public void AddCorrectAnswer(int count = 1)
    {
        if (currentSceneData != null)
            currentSceneData.correctAnswers += count;
    }

    public void AddWrongAnswer(int count = 1)
    {
        if (currentSceneData != null)
            currentSceneData.wrongAnswers += count;
    }

    public void ShowCurrentStats()
    {
        if (currentSceneData != null)
        {
            float currentTime = Time.time - sceneStartTime;
            Debug.Log($"📊 Mevcut Sahne ({currentSceneData.sceneName}): Doğru: {currentSceneData.correctAnswers}, Yanlış: {currentSceneData.wrongAnswers}, Skor: {currentSceneData.score}, Süre: {currentTime:F1}s");
        }

        currentSession.CalculateTotals();
        Debug.Log($"📊 Toplam: Doğru: {currentSession.totalCorrectAnswers}, Yanlış: {currentSession.totalWrongAnswers}, Skor: {currentSession.totalScore}");

        if (offlineDataQueue.Count > 0)
            Debug.Log($"💾 Offline: {offlineDataQueue.Count} oturum");
    }

    public string GetSessionId()
    {
        return currentSession?.sessionId ?? "";
    }

    public bool IsCurrentlySending()
    {
        return isSending;
    }

    // Test methods
    [ContextMenu("Test Connection")]
    public void TestConnection()
    {
        StartCoroutine(TestConnectionCoroutine());
    }

    IEnumerator TestConnectionCoroutine()
    {
        string testUrl = useLocalTest ? localTestUrl : apiUrl;
        using (UnityWebRequest request = UnityWebRequest.Get(testUrl))
        {
            request.timeout = connectionTimeout;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log("✅ Bağlantı başarılı!");
            else
                Debug.LogError($"❌ Bağlantı hatası: {request.error}");
        }
    }

    [ContextMenu("Test Current Provider")]
    void TestCurrentProvider()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"🔍 Current Scene: {currentScene}");

        if (currentSceneData != null)
        {
            Debug.Log($"📊 Scene Data: {currentSceneData.sceneName} - Doğru: {currentSceneData.correctAnswers}, Yanlış: {currentSceneData.wrongAnswers}");
        }

        if (gameDataProviders.ContainsKey(currentScene))
        {
            var provider = gameDataProviders[currentScene];
            if (provider != null)
            {
                Debug.Log($"✅ Provider: {provider.GetType().Name}");
                Debug.Log($"📊 Provider Data - Doğru: {provider.GetCorrectAnswers()}, Yanlış: {provider.GetWrongAnswers()}, Skor: {provider.GetScore()}");
            }
        }
        else
        {
            Debug.LogWarning($"❌ No provider for: {currentScene}");
        }
    }

    [ContextMenu("Debug - Send Field by Field")]
    public void DebugSendFieldByField()
    {
        StartCoroutine(DebugSendFieldByFieldCoroutine());
    }

    IEnumerator DebugSendFieldByFieldCoroutine()
    {
        string targetUrl = useLocalTest ? localTestUrl : apiUrl;

        // Test 1: Minimal required fields
        Debug.Log("🧪 TEST 1: Minimal fields");
        string test1Json = @"{
        ""playerName"": ""TestPlayer"",
        ""sessionId"": ""test-session-123"",
        ""sessionStartTime"": ""2025-06-19T17:00:00.000Z"",
        ""sessionEndTime"": ""2025-06-19T17:05:00.000Z"",
        ""totalGameTime"": 300.0,
        ""totalCorrectAnswers"": 10,
        ""totalWrongAnswers"": 2,
        ""totalScore"": 150,
        ""overallAccuracy"": 0.833333,
        ""gameVersion"": ""1.0"",
        ""sessionCompleted"": true,
        ""sceneDataList"": []
    }";

        yield return StartCoroutine(TestSpecificJson(test1Json, "MINIMAL"));
        yield return new WaitForSeconds(2f);

        // Test 2: With scene data
        Debug.Log("🧪 TEST 2: With scene data");
        string test2Json = @"{
        ""playerName"": ""TestPlayer"",
        ""sessionId"": ""test-session-456"",
        ""sessionStartTime"": ""2025-06-19T17:00:00.000Z"",
        ""sessionEndTime"": ""2025-06-19T17:05:00.000Z"",
        ""totalGameTime"": 300.0,
        ""totalCorrectAnswers"": 10,
        ""totalWrongAnswers"": 2,
        ""totalScore"": 150,
        ""overallAccuracy"": 0.833333,
        ""gameVersion"": ""1.0"",
        ""sessionCompleted"": true,
        ""sceneDataList"": [
            {
                ""sceneName"": ""MathGame"",
                ""correctAnswers"": 5,
                ""wrongAnswers"": 1,
                ""timeSpent"": 150.0,
                ""score"": 75,
                ""sceneStartTime"": ""2025-06-19T17:00:00.000Z"",
                ""sceneEndTime"": ""2025-06-19T17:02:30.000Z"",
                ""completed"": true
            }
        ]
    }";

        yield return StartCoroutine(TestSpecificJson(test2Json, "WITH SCENES"));
    }

    IEnumerator TestSpecificJson(string jsonData, string testName)
    {
        string targetUrl = useLocalTest ? localTestUrl : apiUrl;

        Debug.Log($"📤 {testName} test:");
        Debug.Log(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(targetUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = connectionTimeout;

            yield return request.SendWebRequest();

            Debug.Log($"=== {testName} RESULT ===");
            Debug.Log($"Code: {request.responseCode}");
            Debug.Log($"Response: {request.downloadHandler.text}");

            if (request.responseCode == 200 || request.responseCode == 201)
                Debug.Log($"✅ {testName} BAŞARILI!");
            else if (request.responseCode == 400)
                Debug.LogError($"❌ {testName} - 400 ERROR: {request.downloadHandler.text}");
            else
                Debug.LogError($"❌ {testName} - ERROR: {request.error}");
        }
    }

    [ContextMenu("Send Real Game Data")]
    public void SendRealGameData()
    {
        if (currentSession == null)
        {
            Debug.LogWarning("⚠️ No active session, creating new one...");
            InitializeSession();
        }

        Debug.Log("📤 Sending real game data...");
        SendSessionData();
    }

    [ContextMenu("Log Final JSON")]
    public void LogFinalJson()
    {
        if (currentSession == null)
        {
            Debug.LogError("❌ Session null!");
            return;
        }

        if (currentSceneData != null)
        {
            UpdateCurrentSceneData();
        }

        currentSession.CalculateTotals();
        string json = CreateApiCompatibleJson(currentSession);
        Debug.Log("🧾 FINAL JSON:");
        Debug.Log(json);
    }

    [ContextMenu("Test - Send API Compatible")]
    public void TestApiCompatible()
    {
        StartCoroutine(TestApiCompatibleCoroutine());
    }

    IEnumerator TestApiCompatibleCoroutine()
    {
        var testData = new GameSessionData("TestPlayer")
        {
            sessionId = System.Guid.NewGuid().ToString(),
            sessionStartTime = System.DateTime.UtcNow.AddMinutes(-5),
            sessionEndTime = System.DateTime.UtcNow,
            totalGameTime = 300f,
            totalCorrectAnswers = 10,
            totalWrongAnswers = 2,
            totalScore = 150,
            overallAccuracy = 83.33f,
            gameVersion = "1.0",
            sessionCompleted = true
        };

        testData.sceneDataList.Add(new SceneData("MathGame")
        {
            correctAnswers = 5,
            wrongAnswers = 1,
            score = 75,
            timeSpent = 150f,
            sceneStartTime = System.DateTime.UtcNow.AddMinutes(-5),
            sceneEndTime = System.DateTime.UtcNow.AddMinutes(-2.5f),
            completed = true
        });

        Debug.Log("🧪 API compatible test data sending...");

        bool success = false;
        yield return StartCoroutine(SendSingleSessionData(testData, (result) => success = result));

        if (success)
            Debug.Log("✅ TEST SUCCESS! API working perfectly!");
        else
            Debug.LogError("❌ Still issues, check response code");
    }

    public void OnSceneComplete()
    {
        if (currentSceneData != null)
        {
            FinishCurrentScene();
        }
        SendSessionData();
        Debug.Log("📤 Scene completed, data sent to API");
    }

    public void OnGameComplete()
    {
        CompleteSessionAndSend();
        Debug.Log("🎮 Game completed, final data sent to API");
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

    // Inspector test methods
    [ContextMenu("Test - Send Session Data")]
    void TestSendSession()
    {
        SendSessionData();
    }

    [ContextMenu("Test - Show Stats")]
    void TestShowStats()
    {
        ShowCurrentStats();
    }

    [ContextMenu("Test - Complete Session")]
    void TestCompleteSession()
    {
        CompleteSessionAndSend();
    }

    [ContextMenu("Test - Send Offline Data")]
    void TestSendOfflineData()
    {
        StartCoroutine(SendOfflineDataQueue());
    }

    [ContextMenu("Reset Session")]
    void ResetSession()
    {
        InitializeSession();
        Debug.Log("🔄 Session reset");
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