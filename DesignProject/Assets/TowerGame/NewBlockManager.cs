using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NewBlockManager : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private Transform blockHolder;
    [SerializeField] private TMPro.TextMeshProUGUI livesText;
    [SerializeField] private TMPro.TextMeshProUGUI stackCountText; // Yeni: Stack sayısını göstermek için
    [SerializeField] private GameObject gameOverPanel;

    private GameObject currentBlock = null;
    private Rigidbody currentRigidbody;
    private Renderer currentBlockRenderer;
    private BlockFallDetector currentBlockDetector;

    // World Space Canvas koordinatları için ayarlandı
    private Vector3 blockStartPosition = new Vector3(0f, 100f, 0f);
    private float blockSpeed = 50f;
    private float gravityScale = 2f;
    private float blockSpeedIncrement = 10f;
    private int blockDirection = 1;
    private float xLimit = 100f;
    private float timeBetweenRounds = 1f;
    private float restartDelay = 2f;

    // Game state variables
    private int startingLives = 3;
    private int livesRemaining;
    private bool playing = true;
    private bool gameOver = false;

    // Stack tracking variables
    private int currentStackCount = 0;
    private const int MAX_STACK_HEIGHT = 7; // 8 blok üst üste gelince game over
    private List<GameObject> stackedBlocks = new List<GameObject>(); // Stack'teki blokları takip et

    void Start()
    {
        // Can sayısını PlayerPrefs'ten yükle (eğer varsa)
        if (PlayerPrefs.HasKey("CurrentLives"))
        {
            livesRemaining = PlayerPrefs.GetInt("CurrentLives");
        }
        else
        {
            livesRemaining = startingLives;
        }

        // Stack sayısını sıfırla (yeni oyun başlarken)
        currentStackCount = 0;
        stackedBlocks.Clear();

        UpdateUI();
        Debug.Log($"Oyun başladı - Can: {livesRemaining}, Stack: {currentStackCount}");

        SpawnNewBlock();
    }

    private void SpawnNewBlock()
    {
        if (gameOver) return;

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

        // BlockFallDetector ekle (eğer prefab'da yoksa)
        currentBlockDetector = currentBlock.GetComponent<BlockFallDetector>();
        if (currentBlockDetector == null)
        {
            currentBlockDetector = currentBlock.AddComponent<BlockFallDetector>();
        }

        Debug.Log($"Block created at position: {currentBlock.transform.localPosition}");

        // Renderer componentini al ve renk değiştir
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

        // Rigidbody'yi başlangıçta devre dışı bırak
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
            RestartCompleteGame();
        }
    }

    private void DropCurrentBlock()
    {
        if (currentBlock == null) return;

        // Block fall detector'ı bilgilendir
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

        // Blok düştüğünde stack kontrolü yap (1 saniye sonra)
        StartCoroutine(CheckBlockLanding(droppedBlock));

        // Spawn the next block.
        StartCoroutine(DelayedSpawn());
    }

    // Blok yere indikten sonra stack durumunu kontrol et
    private IEnumerator CheckBlockLanding(GameObject block)
    {
        // Bloğun yere inmesi için bekle
        yield return new WaitForSeconds(1.5f);

        if (block != null && !gameOver)
        {
            // Blok hala varsa ve yere inmiş ise stack'e ekle
            Rigidbody rb = block.GetComponent<Rigidbody>();
            if (rb != null && rb.velocity.magnitude < 0.1f) // Blok durmuş
            {
                // Stack'e ekle
                AddBlockToStack(block);

                // Stack yüksekliğini kontrol et
                CheckStackHeight();
            }
        }
    }

    // Bloku stack'e ekle
    private void AddBlockToStack(GameObject block)
    {
        if (!stackedBlocks.Contains(block))
        {
            stackedBlocks.Add(block);
            currentStackCount = stackedBlocks.Count;

            Debug.Log($"Blok stack'e eklendi. Mevcut stack: {currentStackCount}");
            UpdateUI();
        }
    }

    // Stack yüksekliğini kontrol et
    private void CheckStackHeight()
    {
        // Null blokları temizle
        stackedBlocks.RemoveAll(block => block == null);
        currentStackCount = stackedBlocks.Count;

        Debug.Log($"Stack kontrolü: {currentStackCount}/{MAX_STACK_HEIGHT}");

        // 8 blok üst üste geldi mi?
        if (currentStackCount >= MAX_STACK_HEIGHT)
        {
            Debug.Log("Kule Tamamlandı!");
            TriggerGameOver("Kule Tamamlandı!");
        }
    }

    // Called from BlockFallDetector whenever it detects a block has fallen off.
    public void RemoveLife()
    {
        if (gameOver) return;

        // Can sayısını azalt
        livesRemaining--;
        Debug.Log($"Blok düştü! Can azaldı: {livesRemaining}");

        // Can sayısını kaydet
        PlayerPrefs.SetInt("CurrentLives", livesRemaining);
        PlayerPrefs.Save();

        UpdateUI();

        // Can kontrolü
        if (livesRemaining <= 0)
        {
            TriggerGameOver("CANLAR BİTTİ!");
        }
        else
        {
            // Hala can var - Oyuna devam et (sahneyi yeniden yükleme!)
            Debug.Log("Can azaldı ama oyun devam ediyor...");
        }
    }

    // Game Over'ı tetikle
    private void TriggerGameOver(string reason)
    {
        Debug.Log($"GAME OVER! Sebep: {reason}");

        gameOver = true;
        playing = false;

        // Can sayısını temizle
        PlayerPrefs.DeleteKey("CurrentLives");
        PlayerPrefs.Save();

        // UI'ı güncelle
        if (livesText != null)
        {
            livesText.text = $"GAME OVER! {reason}";
        }

        // Game Over panelini göster (eğer varsa)
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // 3 saniye sonra yeni oyun başlat
        StartCoroutine(AutoRestart());
    }

    private IEnumerator AutoRestart()
    {
        yield return new WaitForSeconds(3f);
        RestartCompleteGame();
    }

    // Tamamen yeni oyun başlat
    private void RestartCompleteGame()
    {
        Debug.Log("Tamamen yeni oyun başlatılıyor...");

        PlayerPrefs.DeleteKey("CurrentLives");
        PlayerPrefs.Save();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // UI'ları güncelle
    private void UpdateUI()
    {
        if (livesText != null)
        {
            livesText.text = $"Can: {livesRemaining}";
        }

        if (stackCountText != null)
        {
           // stackCountText.text = $"Kule:";
        }
    }

    // Public method to check if game is over (other scripts can use this)
    public bool IsGameOver()
    {
        return gameOver;
    }

    // Stack sayısını dışarıdan almak için
    public int GetCurrentStackCount()
    {
        return currentStackCount;
    }

    // Manuel olarak stack'ten blok çıkar (blok düştüğünde)
    public void RemoveBlockFromStack(GameObject block)
    {
        if (stackedBlocks.Contains(block))
        {
            stackedBlocks.Remove(block);
            currentStackCount = stackedBlocks.Count;
            Debug.Log($"Blok stack'ten çıkarıldı. Mevcut kule: {currentStackCount}");
            UpdateUI();
        }
    }
}