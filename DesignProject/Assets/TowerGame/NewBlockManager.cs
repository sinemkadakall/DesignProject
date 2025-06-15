using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Scene y�netimi i�in eklendi

public class NewBlockManager : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private Transform blockHolder;
    [SerializeField] private TMPro.TextMeshProUGUI livesText;
    [SerializeField] private GameObject gameOverPanel; // Game Over UI'si i�in (opsiyonel)

    private GameObject currentBlock = null;
    private Rigidbody currentRigidbody;
    private Renderer currentBlockRenderer;
    private BlockFallDetector currentBlockDetector; // Eklendi

    // World Space Canvas koordinatlar� i�in ayarland�
    private Vector3 blockStartPosition = new Vector3(0f, 100f, 0f);
    private float blockSpeed = 50f;
    private float gravityScale = 2f;
    private float blockSpeedIncrement = 10f;
    private int blockDirection = 1;
    private float xLimit = 100f;
    private float timeBetweenRounds = 1f;
    private float restartDelay = 2f; // Yeniden ba�lama gecikmesi

    // Variables to handle the game state.
    private int startingLives = 3;
    private int livesRemaining;
    private bool playing = true;
    private bool gameOver = false; // Eklendi

    void Start()
    {
        // Can say�s�n� PlayerPrefs'ten y�kle (e�er varsa)
        if (PlayerPrefs.HasKey("CurrentLives"))
        {
            livesRemaining = PlayerPrefs.GetInt("CurrentLives");
        }
        else
        {
            livesRemaining = startingLives;
        }

        UpdateLivesUI();

        Debug.Log($"Oyun ba�lad� - Can: {livesRemaining}");

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

        // BlockFallDetector ekle (e�er prefab'da yoksa)
        currentBlockDetector = currentBlock.GetComponent<BlockFallDetector>();
        if (currentBlockDetector == null)
        {
            currentBlockDetector = currentBlock.AddComponent<BlockFallDetector>();
        }

        Debug.Log($"Block created at position: {currentBlock.transform.localPosition}");

        // Renderer componentini al ve renk de�i�tir
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

        // Rigidbody'yi ba�lang��ta devre d��� b�rak
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

        // Manual restart key (test i�in)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PlayerPrefs.DeleteKey("CurrentLives");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void DropCurrentBlock()
    {
        if (currentBlock == null) return;

        // Block fall detector'� bilgilendir
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

        // Can say�s�n� azalt
        livesRemaining--;
        Debug.Log($"Blok d��t�! Can azald�: {livesRemaining}");

        // Can say�s�n� kaydet
        PlayerPrefs.SetInt("CurrentLives", livesRemaining);
        PlayerPrefs.Save();

        // Can kontrol�
        if (livesRemaining <= 0)
        {
            // Canlar bitti - Game Over
            Debug.Log("GAME OVER!");
            PlayerPrefs.DeleteKey("CurrentLives"); // Can say�s�n� temizle
            PlayerPrefs.Save();

            // Game Over ekran� g�ster veya mesaj ver
            if (livesText != null)
            {
                livesText.text = "GAME OVER!";
            }

            gameOver = true;
            playing = false;

            // 3 saniye sonra yeni oyun ba�lat
            Invoke(nameof(StartNewGame), 3f);
        }
        else
        {
            // Hala can var - Oyunu yeniden ba�lat
            Debug.Log("Oyun yeniden ba�lat�l�yor...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // Yeni oyun ba�lat (3 canla)
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

        Debug.Log("GAME OVER! T�m canlar bitti!");

        // Can say�s�n� s�f�rla
        PlayerPrefs.DeleteKey("CurrentLives");
        PlayerPrefs.Save();

        // Game Over UI'sini g�ster (e�er varsa)
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Otomatik yeniden ba�latma (t�m canlarla)
        StartCoroutine(AutoRestart());
    }

    private IEnumerator AutoRestart()
    {
        yield return new WaitForSeconds(restartDelay);

        // Yeni oyun ba�lat (t�m canlarla)
        livesRemaining = startingLives;
        PlayerPrefs.DeleteKey("CurrentLives");
        RestartGame();
    }

    public void RestartGame()
    {
        Debug.Log("Oyun yeniden ba�lat�l�yor...");

        // Can say�s�n� PlayerPrefs ile kaydet (sahne yeniden y�klendi�inde korunsun)
        PlayerPrefs.SetInt("CurrentLives", livesRemaining);
        PlayerPrefs.Save();

        // Farkl� restart y�ntemleri dene
        try
        {
            // Y�ntem 1: Mevcut sahneyi yeniden y�kle
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Scene reload hatas�: " + e.Message);

            // Y�ntem 2: Manuel restart
            ManualRestart();
        }
    }

    // Manual restart - t�m de�i�kenleri s�f�rla
    private void ManualRestart()
    {
        Debug.Log("Manuel restart ba�lat�l�yor...");

        // T�m bloklar� yok et
        DestroyAllBlocks();

        // De�i�kenleri s�f�rla
        livesRemaining = startingLives;
        playing = true;
        gameOver = false;
        blockSpeed = 50f; // Ba�lang�� h�z�na d�nd�r
        blockDirection = 1;

        // UI'� g�ncelle
        UpdateLivesUI();

        // Game over panelini gizle
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Yeni blok spawn et
        SpawnNewBlock();

        Debug.Log("Manuel restart tamamland�!");
    }

    // T�m bloklar� yok et
    private void DestroyAllBlocks()
    {
        // BlockHolder alt�ndaki t�m bloklar� yok et
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

        // Current block referans�n� temizle
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