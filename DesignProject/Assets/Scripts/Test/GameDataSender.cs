using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine.Playables;

public class GameDataSender : MonoBehaviour
{
    [Header("API Ayarları")]
    public string apiUrl = "https://admin-dashboard-git-main-hacerkilic01s-projects.vercel.app/api/game-result";

    public bool useLocalTest = true; // Test için yerel sunucu kullan
    public string localTestUrl = "https://admin-dashboard-git-main-hacerkilic01s-projects.vercel.app/api/game-result";

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
        if (Input.GetKeyDown(KeyCode.X))
        {
            GameDataSender.Instance.CompleteSessionAndSend(); // ✅ Sahne bitirip veriyi gönderir
        }
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

        // Önceki sahne verisini kaydet
        if (currentSceneData != null && !string.IsNullOrEmpty(lastSceneName) && lastSceneName != scene.name)
        {
            FinishCurrentScene();
        }

        // Yeni sahne oyun sahnesi ise başlat
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
            Debug.Log($"🆕 Yeni sahne başlatıldı: {sceneName} - Zaman: {sceneStartTime}");

        StartCoroutine(FindDataProviderWithDelay(sceneName));
    }


    void FinishCurrentScene()
    {
        if (showDebugLogs)
            Debug.Log("🔄 FinishCurrentScene çağrıldı: " + (currentSceneData?.sceneName ?? "null"));

        if (currentSceneData == null) return;

        // Son kez veriyi güncelle
        UpdateCurrentSceneData();

        currentSceneData.timeSpent = Time.time - sceneStartTime;
        currentSceneData.sceneEndTime = DateTime.Now;
        currentSceneData.completed = true;

        // Aynı sahne zaten varsa kaldır (duplicate önleme)
        var existingScene = currentSession.sceneDataList.Find(s => s.sceneName == currentSceneData.sceneName);
        if (existingScene != null)
        {
            currentSession.sceneDataList.Remove(existingScene);
        }

        // Yeni veriyi ekle
        currentSession.sceneDataList.Add(currentSceneData);

        if (showDebugLogs)
            Debug.Log($"✅ Sahne tamamlandı ve kaydedildi: {currentSceneData.sceneName}");
        Debug.Log($"📊 Sahne Verileri - Doğru: {currentSceneData.correctAnswers}, Yanlış: {currentSceneData.wrongAnswers}, Skor: {currentSceneData.score}");
        Debug.Log($"📋 Toplam sahne sayısı: {currentSession.sceneDataList.Count}");

        currentSceneData = null; // Reset için
    }


    IEnumerator FindDataProviderWithDelay(string sceneName)
    {
        yield return new WaitForSeconds(0.5f);

        IGameDataProvider provider = null;

        switch (sceneName)
        {
            case "MathGame":
                var mathGameManager = FindObjectOfType<GameManager>();
                if (mathGameManager != null)
                {
                    provider = mathGameManager; // Artık direkt olarak IGameDataProvider implement ediyor
                    if (showDebugLogs)
                        Debug.Log("✅ MathGame GameManager found and connected!");
                }
                break;

            case "PuzzleGame":
                var puzzleGameManager = FindObjectOfType<PuzzleGameManager>();
                if (puzzleGameManager != null)
                {
                    provider = puzzleGameManager;
                    if (showDebugLogs)
                        Debug.Log("✅ PuzzleGameManager found and connected!");
                }
                break;

            case "WhackAMole":
                var moleManager = FindObjectOfType<MoleManager>();
                if (moleManager != null)
                {
                    provider = moleManager;
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
                        if (showDebugLogs)
                            Debug.Log($"✅ Generic provider found: {component.GetType().Name}");
                        break;
                    }
                }
                break;
        }

        if (provider != null)
        {
            gameDataProviders[sceneName] = provider;
            if (showDebugLogs)
                Debug.Log($"✅ {sceneName} için data provider kaydedildi: {provider.GetType().Name}");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogError($"❌ {sceneName} için data provider bulunamadı!");
        }

        // İlk veri güncellemesini yap
        UpdateCurrentSceneData();
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
                currentSceneData.timeSpent = provider.GetTimeSpent(); // ✅ Eklendi
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

        // Mevcut sahne verisini kaydet
        if (currentSceneData != null)
        {
            UpdateCurrentSceneData();
            if (showDebugLogs)
                Debug.Log($"📊 Mevcut sahne verisi güncellendi: {currentSceneData.sceneName}");
        }

        if (isSending)
        {
            if (showDebugLogs)
                Debug.LogWarning("⚠️ Zaten gönderim devam ediyor, bekleniyor...");
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

    /* IEnumerator SendSingleSessionData(GameSessionData sessionData, System.Action<bool> callback)
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
     }*/
    /*  IEnumerator SendSingleSessionData(GameSessionData sessionData, System.Action<bool> callback)
      {
          if (currentSceneData != null)
          {
              UpdateCurrentSceneData();
          }

          sessionData.CalculateTotals();
          string jsonData = JsonUtility.ToJson(sessionData, true);

          // DEBUG: JSON validasyonu yapın
          if (showDebugLogs)
          {
              Debug.Log("=== JSON DEBUG BAŞLANGIÇ ===");
              Debug.Log($"📤 Gönderilecek JSON verisi ({jsonData.Length} karakter):");
              Debug.Log(jsonData);

              // JSON syntax kontrolü
              try
              {
                  var testParse = JsonUtility.FromJson<GameSessionData>(jsonData);
                  Debug.Log("✅ JSON syntax valid");
              }
              catch (System.Exception e)
              {
                  Debug.LogError("❌ JSON syntax hatası: " + e.Message);
              }

              Debug.Log("=== JSON DEBUG BİTİŞ ===");
          }

          string targetUrl = useLocalTest ? localTestUrl : apiUrl;
          Debug.Log($"🌐 Target URL: {targetUrl}");

          using (UnityWebRequest request = new UnityWebRequest(targetUrl, "POST"))
          {
              byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
              request.uploadHandler = new UploadHandlerRaw(bodyRaw);
              request.downloadHandler = new DownloadHandlerBuffer();

              // Header'ları ekle
              request.SetRequestHeader("Content-Type", "application/json");
              request.SetRequestHeader("Accept", "application/json");

              // DEBUG: Request details
              if (showDebugLogs)
              {
                  Debug.Log("=== REQUEST DEBUG ===");
                  Debug.Log($"Method: {request.method}");
                  Debug.Log($"URL: {request.url}");
                  Debug.Log($"Content-Type: application/json");
                  Debug.Log($"Content-Length: {bodyRaw.Length}");
                  Debug.Log("=== REQUEST DEBUG BİTİŞ ===");
              }

              request.timeout = connectionTimeout;

              yield return request.SendWebRequest();

              // DEBUG: Response details
              if (showDebugLogs)
              {
                  Debug.Log("=== RESPONSE DEBUG ===");
                  Debug.Log($"Response Code: {request.responseCode}");
                  Debug.Log($"Result: {request.result}");

                  if (!string.IsNullOrEmpty(request.downloadHandler.text))
                  {
                      Debug.Log($"Server Response: {request.downloadHandler.text}");
                  }

                  // Response headers'ları kontrol et
                  var responseHeaders = request.GetResponseHeaders();
                  if (responseHeaders != null)
                  {
                      foreach (var header in responseHeaders)
                      {
                          Debug.Log($"Response Header: {header.Key} = {header.Value}");
                      }
                  }
                  Debug.Log("=== RESPONSE DEBUG BİTİŞ ===");
              }

              if (request.result == UnityWebRequest.Result.Success)
              {
                  if (showDebugLogs)
                  {
                      Debug.Log("✅ Oturum verisi başarıyla gönderildi!");
                  }
                  callback(true);
              }
              else
              {
                  if (showDebugLogs)
                  {
                      Debug.LogError("❌ Veri gönderme hatası: " + request.error);
                      Debug.LogError($"Response Code: {request.responseCode}");

                      // Server'ın döndürdüğü hata mesajını kontrol et
                      if (!string.IsNullOrEmpty(request.downloadHandler.text))
                      {
                          Debug.LogError("Server Error Message: " + request.downloadHandler.text);
                      }

                      // Yaygın 400 hatası sebeplerini kontrol et
                      Debug400Solutions(request.responseCode, request.downloadHandler.text);
                  }
                  callback(false);
              }
          }
      }*/
    private string CreateApiCompatibleJson(GameSessionData sessionData)
    {
        // API'nin kesinlikle ihtiyacı olan tüm alanları güvence altına alalım
        sessionData.CalculateTotals();

        // Null/undefined değerleri temizle
        string safePlayerName = string.IsNullOrEmpty(sessionData.playerName) ? "Unknown" : sessionData.playerName;
        string safeSessionId = string.IsNullOrEmpty(sessionData.sessionId) ? System.Guid.NewGuid().ToString() : sessionData.sessionId;
        string safeGameVersion = string.IsNullOrEmpty(sessionData.gameVersion) ? "1.0" : sessionData.gameVersion;

        // SceneDataList'i güvenli hale getir
        var safeSceneDataList = sessionData.sceneDataList ?? new List<SceneData>();

        // Manuel JSON oluştur - JsonUtility sorunlarını önlemek için
        var sceneJsonList = new List<string>();

        foreach (var scene in safeSceneDataList)
        {
            // Her sahne için güvenli değerler
            string safeSceneName = string.IsNullOrEmpty(scene.sceneName) ? "Unknown" : scene.sceneName;
            float safeTimeSpent = float.IsNaN(scene.timeSpent) ? 0f : scene.timeSpent;

            string sceneJson = $@"{{
      ""sceneName"": ""{safeSceneName}"",
      ""correctAnswers"": {scene.correctAnswers},
      ""wrongAnswers"": {scene.wrongAnswers},
      ""timeSpent"": {safeTimeSpent.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},
      ""score"": {scene.score},
      ""sceneStartTime"": ""{scene.sceneStartTime:yyyy-MM-ddTHH:mm:ssZ}"",
      ""sceneEndTime"": ""{scene.sceneEndTime:yyyy-MM-ddTHH:mm:ssZ}"",
      ""completed"": {scene.completed.ToString().ToLower()}
    }}";
            sceneJsonList.Add(sceneJson);
        }

        string sceneDataArray = "[" + string.Join(",", sceneJsonList) + "]";

        // Ana JSON'u oluştur - API'nin tam olarak beklediği format
        string finalJson = $@"{{
  ""playerName"": ""{safePlayerName}"",
  ""sessionId"": ""{safeSessionId}"",
  ""sessionStartTime"": ""{sessionData.sessionStartTime:yyyy-MM-ddTHH:mm:ssZ}"",
  ""sessionEndTime"": ""{sessionData.sessionEndTime:yyyy-MM-ddTHH:mm:ssZ}"",
  ""totalGameTime"": {sessionData.totalGameTime.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},
  ""totalCorrectAnswers"": {sessionData.totalCorrectAnswers},
  ""totalWrongAnswers"": {sessionData.totalWrongAnswers},
  ""totalScore"": {sessionData.totalScore},
  ""overallAccuracy"": {sessionData.overallAccuracy.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)},
  ""gameVersion"": ""{safeGameVersion}"",
  ""sessionCompleted"": {sessionData.sessionCompleted.ToString().ToLower()},
  ""sceneDataList"": {sceneDataArray}
}}";

        return finalJson;
    }
    /* IEnumerator SendSingleSessionData(GameSessionData sessionData, System.Action<bool> callback)
     {
         if (currentSceneData != null)
         {
             UpdateCurrentSceneData();
         }

         sessionData.CalculateTotals();

         // DateTime'ları string'e çevir - en basit çözüm
         string jsonData = CreateApiCompatibleJson(sessionData);

         // DEBUG: JSON validasyonu
         if (showDebugLogs)
         {
             Debug.Log("=== JSON DEBUG BAŞLANGIÇ ===");
             Debug.Log($"📤 API uyumlu JSON verisi ({jsonData.Length} karakter):");
             Debug.Log(jsonData);
             Debug.Log("=== JSON DEBUG BİTİŞ ===");
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
                     Debug.LogError("Server Response: " + request.downloadHandler.text);
                 }
                 callback(false);
             }
         }
     }*/
    IEnumerator SendSingleSessionData(GameSessionData sessionData, System.Action<bool> callback)
    {
        if (currentSceneData != null)
        {
            UpdateCurrentSceneData();
        }

        // API uyumlu JSON oluştur
        string jsonData = CreateApiCompatibleJson(sessionData);

        if (showDebugLogs)
        {
            Debug.Log("=== API UYUMLU JSON ===");
            Debug.Log($"📤 Gönderilecek JSON ({jsonData.Length} karakter):");
            Debug.Log(jsonData);

            // JSON validasyonu
            if (jsonData.Contains("null") || jsonData.Contains("undefined"))
            {
                Debug.LogWarning("⚠️ JSON'da null/undefined değerler var!");
            }
            else
            {
                Debug.Log("✅ JSON temiz, null değer yok");
            }
            Debug.Log("=== JSON HAZIR ===");
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
                Debug.Log($"Response Body: {request.downloadHandler.text}");
                Debug.Log("=== API RESPONSE END ===");
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (showDebugLogs)
                {
                    Debug.Log("✅ Veri başarıyla API'ye gönderildi!");
                }
                callback(true);
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.LogError("❌ API hatası: " + request.error);
                    Debug.LogError($"Response Code: {request.responseCode}");

                    // Detaylı hata analizi
                    if (request.responseCode == 400)
                    {
                        Debug.LogError("🔍 400 HATA ANALİZİ:");
                        Debug.LogError($"Server mesajı: {request.downloadHandler.text}");

                        // JSON'u logla ki hata nerede görelim
                        Debug.LogError("Gönderilen JSON:");
                        Debug.LogError(jsonData);
                    }
                }
                callback(false);
            }
        }
    }


    // DateTime sorununu çözen yeni metod
    /*  private string CreateApiCompatibleJson(GameSessionData sessionData)
      {
          // Unity'nin JsonUtility ile JSON oluştur
          string originalJson = JsonUtility.ToJson(sessionData, true);

          // DateTime formatlarını düzelt
          string fixedJson = FixDateTimeFormat(originalJson);

          return fixedJson;
      }*/
    // Test için API formatına uygun minimal veri gönder
    [ContextMenu("Test - Send API Compatible")]
    public void TestApiCompatible()
    {
        StartCoroutine(TestApiCompatibleCoroutine());
    }

    IEnumerator TestApiCompatibleCoroutine()
    {
        // API'nin beklediği tam formatta test verisi
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

        // Test sahne verisi ekle
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

        testData.sceneDataList.Add(new SceneData("PuzzleGame")
        {
            correctAnswers = 5,
            wrongAnswers = 1,
            score = 75,
            timeSpent = 150f,
            sceneStartTime = System.DateTime.UtcNow.AddMinutes(-2.5f),
            sceneEndTime = System.DateTime.UtcNow,
            completed = true
        });

        Debug.Log("🧪 API uyumlu test verisi gönderiliyor...");

        bool success = false;
        yield return StartCoroutine(SendSingleSessionData(testData, (result) => success = result));

        if (success)
        {
            Debug.Log("✅ TEST BAŞARILI! API çalışıyor");
        }
        else
        {
            Debug.LogError("❌ TEST BAŞARISIZ! API sorunu var");
        }
    }
    // DateTime'ları API uyumlu formata çevir
    private string FixDateTimeFormat(string jsonString)
    {
        // Unity'nin DateTime serializasyonu şu formatta: "2025-06-19T16:26:38.1234567+03:00"
        // API'ler genelde şu formatı tercih eder: "2025-06-19T16:26:38Z"

        // Regex ile DateTime'ları bul ve değiştir
        System.Text.RegularExpressions.Regex dateTimeRegex =
            new System.Text.RegularExpressions.Regex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?([\+\-]\d{2}:\d{2})?");

        string fixedJson = dateTimeRegex.Replace(jsonString, (match) => {
            // DateTime'ı parse et ve basit formata çevir
            if (System.DateTime.TryParse(match.Value, out System.DateTime dateTime))
            {
                return dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            }
            return match.Value; // Parse edilemezse orijinal değeri bırak
        });

        return fixedJson;
    }

    // Test için basit JSON gönderme metodu
    [ContextMenu("Test - Send Simple JSON")]
    public void SendSimpleTestJson()
    {
        StartCoroutine(SendSimpleTestJsonCoroutine());
    }

    IEnumerator SendSimpleTestJsonCoroutine()
    {
        string simpleJson = "{\"test\":\"value\",\"timestamp\":\"2025-06-19T10:30:00Z\"}";
        string targetUrl = useLocalTest ? localTestUrl : apiUrl;

        Debug.Log($"🧪 Test JSON gönderiliyor: {simpleJson}");

        using (UnityWebRequest request = new UnityWebRequest(targetUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(simpleJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.timeout = connectionTimeout;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Test JSON başarıyla gönderildi!");
                Debug.Log($"Server response: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"❌ Test JSON başarısız: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    Debug.LogError($"Server Message: {request.downloadHandler.text}");
                }
            }
        }
    }

    // 400 hatası için özel çözüm önerileri
    void Debug400Solutions(long responseCode, string serverMessage)
    {
        if (responseCode == 400)
        {
            Debug.LogWarning("🔧 400 BAD REQUEST ÇÖZÜMLERİ:");
            Debug.LogWarning("1. JSON formatını kontrol edin - DateTime formatı sorunlu olabilir");
            Debug.LogWarning("2. API'nizin beklediği field adlarını kontrol edin");
            Debug.LogWarning("3. Content-Type header'ı doğru mu kontrol edin");
            Debug.LogWarning("4. URL'in doğru olduğundan emin olun");

            if (!string.IsNullOrEmpty(serverMessage))
            {
                Debug.LogWarning($"5. Server mesajını kontrol edin: {serverMessage}");

                // Yaygın hatalar için özel kontroller
                if (serverMessage.Contains("JSON") || serverMessage.Contains("json"))
                {
                    Debug.LogWarning("💡 JSON format sorunu olabilir - DateTime formatını kontrol edin");
                }

                if (serverMessage.Contains("field") || serverMessage.Contains("required"))
                {
                    Debug.LogWarning("💡 Eksik veya hatalı field adları olabilir");
                }
            }

            Debug.LogWarning("6. Test için basit JSON gönderin:");
            Debug.LogWarning("   { \"test\": \"value\" }");
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

    [ContextMenu("Log JSON Verisi")]
    void LogCurrentGameData()
    {
        if (currentSceneData != null)
            UpdateCurrentSceneData();

        currentSession.CalculateTotals();
        string json = JsonUtility.ToJson(currentSession, true); // Pretty print
        Debug.Log("🧾 JSON VERİSİ:\n" + json);
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