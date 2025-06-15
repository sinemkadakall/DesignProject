using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Scene yönetimi için eklendi

public class NewBlockManager : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private Transform blockHolder;
    [SerializeField] private TMPro.TextMeshProUGUI livesText;
    [SerializeField] private GameObject gameOverPanel; // Game Over UI'si için (opsiyonel)

    private GameObject currentBlock = null;
    private Rigidbody currentRigidbody;
    private Renderer currentBlockRenderer;
    private BlockFallDetector currentBlockDetector; // Eklendi

    // World Space Canvas koordinatlarý için ayarlandý
    private Vector3 blockStartPosition = new Vector3(0f, 100f, 0f);
    private float blockSpeed = 50f;
    private float gravityScale = 2f;
    private float blockSpeedIncrement = 10f;
    private int blockDirection = 1;
    private float xLimit = 100f;
    private float timeBetweenRounds = 1f;
    private float restartDelay = 2f; // Yeniden baþlama gecikmesi

    // Variables to handle the game state.
    private int startingLives = 3;
    private int livesRemaining;
    private bool playing = true;
    private bool gameOver = false; // Eklendi

    void Start()
    {
        // Can sayýsýný PlayerPrefs'ten yükle (eðer varsa)
        if (PlayerPrefs.HasKey("CurrentLives"))
        {
            livesRemaining = PlayerPrefs.GetInt("CurrentLives");
        }
        else
        {
            livesRemaining = startingLives;
        }

        UpdateLivesUI();

        Debug.Log($"Oyun baþladý - Can: {livesRemaining}");

        SpawnNewBlock();
    }

    private void SpawnNewBlock()
    {
        if (gameOver) return; // Game over durumunda yeni blok yaratma

        Debug.Log("SpawnNewBlock called");

        if (blockPrefab == null)
        {
            Debug.LogError("Block prefab is null! Please assign it in the inspector.");
            return;
        }

        if (blockHolder == null)
        {
            Debug.LogError("Block holder is null! Please assign it in the inspector.");
            return;
        }

        // Create a block with the desired properties.
        currentBlock = Instantiate(blockPrefab, blockHolder);
        currentBlock.transform.localPosition = blockStartPosition;

        // BlockFallDetector ekle (eðer prefab'da yoksa)
        currentBlockDetector = currentBlock.GetComponent<BlockFallDetector>();
        if (currentBlockDetector == null)
        {
            currentBlockDetector = currentBlock.AddComponent<BlockFallDetector>();
        }

        Debug.Log($"Block created at position: {currentBlock.transform.localPosition}");

        // Renderer componentini al ve renk deðiþtir
        currentBlockRenderer = currentBlock.GetComponent<Renderer>();
        if (currentBlockRenderer != null)
        {
            Material newMaterial = new Material(currentBlockRenderer.material);
            newMaterial.color = Random.ColorHSV();
            currentBlockRenderer.material = newMaterial;
            Debug.Log("Block color changed");
        }
        else
        {
            Debug.LogWarning("No Renderer found on block!");
        }

        currentRigidbody = currentBlock.GetComponent<Rigidbody>();

        // Rigidbody'yi baþlangýçta devre dýþý býrak
        if (currentRigidbody != null)
        {
            currentRigidbody.isKinematic = true;
            currentRigidbody.useGravity = false;
            currentRigidbody.mass = 1f;
            currentRigidbody.drag = 0f;
            currentRigidbody.angularDrag = 0.05f;
            Debug.Log("Rigidbody configured");
        }
        else
        {
            Debug.LogWarning("No Rigidbody found on block!");
        }

        BoxCollider boxCollider = currentBlock.GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = false;
            Debug.Log("BoxCollider configured");
        }
        else
        {
            Debug.LogWarning("No BoxCollider found on block!");
        }

        // Increase the block speed each time to make it harder.
        blockSpeed += blockSpeedIncrement;

        Debug.Log($"New block speed: {blockSpeed}");
    }

    private IEnumerator DelayedSpawn()
    {
        yield return new WaitForSeconds(timeBetweenRounds);
        SpawnNewBlock();
    }

    void Update()
    {
        // Game over durumunda oyun kontrollerini durdur
        if (gameOver) return;

        // If we have a waiting block, move it about.
        if (currentBlock && playing)
        {
            float moveAmount = Time.deltaTime * blockSpeed * blockDirection;
            Vector3 currentPos = currentBlock.transform.localPosition;
            currentBlock.transform.localPosition = new Vector3(currentPos.x + moveAmount, currentPos.y, currentPos.z);

            // If we've gone as far as we want, reverse direction.
            if (Mathf.Abs(currentBlock.transform.localPosition.x) > xLimit)
            {
                Vector3 limitedPos = currentBlock.transform.localPosition;
                limitedPos.x = blockDirection * xLimit;
                currentBlock.transform.localPosition = limitedPos;
                blockDirection = -blockDirection;
            }

            // If we press space drop the block.
            if (Input.GetKeyDown(KeyCode.Space))
            {
                DropCurrentBlock();
            }
        }

        // Manual restart key (test için)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PlayerPrefs.DeleteKey("CurrentLives");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void DropCurrentBlock()
    {
        if (currentBlock == null) return;

        // Block fall detector'ý bilgilendir
        if (currentBlockDetector != null)
        {
            currentBlockDetector.OnBlockDropped();
        }

        // Stop it moving.
        GameObject droppedBlock = currentBlock;
        currentBlock = null;

        // Activate the RigidBody to enable gravity to drop it.
        if (currentRigidbody != null)
        {
            currentRigidbody.isKinematic = false;
            currentRigidbody.useGravity = true;
            currentRigidbody.AddForce(Vector3.down * gravityScale, ForceMode.Impulse);
        }

        // Spawn the next block.
        StartCoroutine(DelayedSpawn());
    }

    // Called from BlockFallDetector whenever it detects a block has fallen off.
    public void RemoveLife()
    {
        if (gameOver) return;

        // Can sayýsýný azalt
        livesRemaining--;
        Debug.Log($"Blok düþtü! Can azaldý: {livesRemaining}");

        // Can sayýsýný kaydet
        PlayerPrefs.SetInt("CurrentLives", livesRemaining);
        PlayerPrefs.Save();

        // Can kontrolü
        if (livesRemaining <= 0)
        {
            // Canlar bitti - Game Over
            Debug.Log("GAME OVER!");
            PlayerPrefs.DeleteKey("CurrentLives"); // Can sayýsýný temizle
            PlayerPrefs.Save();

            // Game Over ekraný göster veya mesaj ver
            if (livesText != null)
            {
                livesText.text = "GAME OVER!";
            }

            gameOver = true;
            playing = false;

            // 3 saniye sonra yeni oyun baþlat
            Invoke(nameof(StartNewGame), 3f);
        }
        else
        {
            // Hala can var - Oyunu yeniden baþlat
            Debug.Log("Oyun yeniden baþlatýlýyor...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // Yeni oyun baþlat (3 canla)
    private void StartNewGame()
    {
        PlayerPrefs.DeleteKey("CurrentLives");
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void UpdateLivesUI()
    {
        if (livesText != null)
        {
            livesText.text = $"Can: {livesRemaining}";
        }
    }

    private void GameOver()
    {
        playing = false;
        gameOver = true;

        Debug.Log("GAME OVER! Tüm canlar bitti!");

        // Can sayýsýný sýfýrla
        PlayerPrefs.DeleteKey("CurrentLives");
        PlayerPrefs.Save();

        // Game Over UI'sini göster (eðer varsa)
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Otomatik yeniden baþlatma (tüm canlarla)
        StartCoroutine(AutoRestart());
    }

    private IEnumerator AutoRestart()
    {
        yield return new WaitForSeconds(restartDelay);

        // Yeni oyun baþlat (tüm canlarla)
        livesRemaining = startingLives;
        PlayerPrefs.DeleteKey("CurrentLives");
        RestartGame();
    }

    public void RestartGame()
    {
        Debug.Log("Oyun yeniden baþlatýlýyor...");

        // Can sayýsýný PlayerPrefs ile kaydet (sahne yeniden yüklendiðinde korunsun)
        PlayerPrefs.SetInt("CurrentLives", livesRemaining);
        PlayerPrefs.Save();

        // Farklý restart yöntemleri dene
        try
        {
            // Yöntem 1: Mevcut sahneyi yeniden yükle
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Scene reload hatasý: " + e.Message);

            // Yöntem 2: Manuel restart
            ManualRestart();
        }
    }

    // Manual restart - tüm deðiþkenleri sýfýrla
    private void ManualRestart()
    {
        Debug.Log("Manuel restart baþlatýlýyor...");

        // Tüm bloklarý yok et
        DestroyAllBlocks();

        // Deðiþkenleri sýfýrla
        livesRemaining = startingLives;
        playing = true;
        gameOver = false;
        blockSpeed = 50f; // Baþlangýç hýzýna döndür
        blockDirection = 1;

        // UI'ý güncelle
        UpdateLivesUI();

        // Game over panelini gizle
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Yeni blok spawn et
        SpawnNewBlock();

        Debug.Log("Manuel restart tamamlandý!");
    }

    // Tüm bloklarý yok et
    private void DestroyAllBlocks()
    {
        // BlockHolder altýndaki tüm bloklarý yok et
        if (blockHolder != null)
        {
            foreach (Transform child in blockHolder)
            {
                Destroy(child.gameObject);
            }
        }

        // Alternatif: Tag ile bul ve yok et
        GameObject[] allBlocks = GameObject.FindGameObjectsWithTag("Block");
        foreach (GameObject block in allBlocks)
        {
            Destroy(block);
        }

        // Current block referansýný temizle
        currentBlock = null;
        currentRigidbody = null;
        currentBlockRenderer = null;
        currentBlockDetector = null;
    }

    // Public method to check if game is over (other scripts can use this)
    public bool IsGameOver()
    {
        return gameOver;
    }
}