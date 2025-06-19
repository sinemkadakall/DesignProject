using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PuzzleGameManager : MonoBehaviour, IGameDataProvider
{
    [SerializeField] private Transform gameTransform;
    [SerializeField] private Transform piecePrefab;

    // Level sistemi için yeni deðiþkenler
    [SerializeField] private Material level1Material;  // Ýlk level için materyal
    [SerializeField] private Material level2Material;  // Ýkinci level için materyal
    [SerializeField] private TextMeshProUGUI levelText; // Level göstergesi

    private List<Transform> pieces;
    private int emptyLocation;
    private int size;
    private bool shuffling = false;

    // Level sistemi
    private int currentLevel = 1;
    private const int maxLevel = 2;

    [SerializeField] private TextMeshProUGUI timerText;
    private float timer = 60f;
    private const float levelTime = 60f; // Her level için süre
    private float gameStartTime; // Oyun baþlangýç zamaný

    // GameDataSender için veri deðiþkenleri
    private int levelsCompleted = 0;
    private int totalMoves = 0;
    private int score = 0;
    private bool gameFinished = false;

    // Create the game setup with size x size pieces.
    private void CreateGamePieces(float gapThickness)
    {
        // Önceki parçalarý temizle
        ClearPieces();

        // This is the width of each tile.
        float width = 1 / (float)size;
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                Transform piece = Instantiate(piecePrefab, gameTransform);
                pieces.Add(piece);

                // Mevcut seviyeye göre materyali ata
                Material currentMaterial = (currentLevel == 1) ? level1Material : level2Material;
                if (currentMaterial != null)
                {
                    piece.GetComponent<MeshRenderer>().material = currentMaterial;
                }

                // Pieces will be in a game board going from -1 to +1.
                piece.localPosition = new Vector3(-1 + (2 * width * col) + width,
                                                  +1 - (2 * width * row) - width,
                                                  0);
                piece.localScale = ((2 * width) - gapThickness) * Vector3.one;
                piece.name = $"{(row * size) + col}";
                // We want an empty space in the bottom right.
                if ((row == size - 1) && (col == size - 1))
                {
                    emptyLocation = (size * size) - 1;
                    piece.gameObject.SetActive(false);
                }
                else
                {
                    // We want to map the UV coordinates appropriately, they are 0->1.
                    float gap = gapThickness / 2;
                    Mesh mesh = piece.GetComponent<MeshFilter>().mesh;
                    Vector2[] uv = new Vector2[4];
                    // UV coord order: (0, 1), (1, 1), (0, 0), (1, 0)
                    uv[0] = new Vector2((width * col) + gap, 1 - ((width * (row + 1)) - gap));
                    uv[1] = new Vector2((width * (col + 1)) - gap, 1 - ((width * (row + 1)) - gap));
                    uv[2] = new Vector2((width * col) + gap, 1 - ((width * row) + gap));
                    uv[3] = new Vector2((width * (col + 1)) - gap, 1 - ((width * row) + gap));
                    // Assign our new UVs to the mesh.
                    mesh.uv = uv;
                }
            }
        }
    }

    // Önceki parçalarý temizle
    private void ClearPieces()
    {
        if (pieces != null)
        {
            foreach (Transform piece in pieces)
            {
                if (piece != null)
                {
                    DestroyImmediate(piece.gameObject);
                }
            }
            pieces.Clear();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        pieces = new List<Transform>();
        size = 3;
        currentLevel = 1;
        timer = levelTime;
        gameStartTime = Time.time;
        levelsCompleted = 0;
        totalMoves = 0;
        score = 0;
        gameFinished = false;

        UpdateUI();
        CreateGamePieces(0.01f);

        // Ýlk baþta oyunu karýþtýr
        shuffling = true;
        StartCoroutine(WaitShuffle(0.5f));
    }

    // UI güncellemesi
    private void UpdateUI()
    {
        if (levelText != null)
        {
            levelText.text = "Level: " + currentLevel;
        }
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timerText != null)
        {
            timerText.text = "Süre: " + Mathf.CeilToInt(timer).ToString() + "s";
        }

        // Süre bittiðinde sahneyi yeniden yükle
        if (timer <= 0f)
        {
            gameFinished = true;
            // GameDataSender'a veri gönder
            if (GameDataSender.Instance != null)
            {
                GameDataSender.Instance.SendSessionData();
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // Oyun tamamlandý mý kontrol et (sadece karýþtýrma bitmiþse)
        if (!shuffling && CheckCompletion())
        {
            OnLevelComplete();
        }

        // Sol týk algýlama (3D Raycast)
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject clickedObject = hit.collider.gameObject;

                for (int i = 0; i < pieces.Count; i++)
                {
                    if (pieces[i].gameObject == clickedObject)
                    {
                        // Hamle sayýsýný artýr
                        bool moveSuccessful = false;

                        // Komþu yönleri kontrol et (yukarý, aþaðý, sol, sað)
                        if (SwapIfValid(i, -size, size)) { moveSuccessful = true; }
                        else if (SwapIfValid(i, +size, size)) { moveSuccessful = true; }
                        else if (SwapIfValid(i, -1, 0)) { moveSuccessful = true; }
                        else if (SwapIfValid(i, +1, size - 1)) { moveSuccessful = true; }

                        if (moveSuccessful)
                        {
                            totalMoves++;
                            break;
                        }
                    }
                }
            }
        }
    }

    // Level tamamlandýðýnda çaðrýlýr
    private void OnLevelComplete()
    {
        levelsCompleted++;

        // Skor hesapla (level tamamlama + kalan süre bonusu)
        int levelScore = 100 + Mathf.RoundToInt(timer * 2); // Kalan süre baþýna 2 puan
        score += levelScore;

        Debug.Log($"Level {currentLevel} tamamlandý! Skor: {levelScore}, Toplam: {score}");

        if (currentLevel < maxLevel)
        {
            // Sonraki levela geç
            currentLevel++;
            timer = levelTime; // Süreyi sýfýrla
            UpdateUI();
            shuffling = true;
            StartCoroutine(NextLevelTransition());
        }
        else
        {
            // Oyun tamamlandý
            gameFinished = true;

            // Oyun tamamlama bonusu
            score += 200;

            Debug.Log($"Oyun tamamlandý! Final skoru: {score}");

            // GameDataSender'a veri gönder
            if (GameDataSender.Instance != null)
            {
                GameDataSender.Instance.SendSessionData();
            }

            StartCoroutine(GameCompleted());
        }
    }

    // Sonraki level geçiþi
    private IEnumerator NextLevelTransition()
    {
        yield return new WaitForSeconds(1f); // Kýsa bir bekleme
        CreateGamePieces(0.01f); // Yeni seviye parçalarýný oluþtur
        yield return new WaitForSeconds(0.5f);
        Shuffle(); // Karýþtýr
        shuffling = false;
    }

    // Oyun tamamlandýðýnda
    private IEnumerator GameCompleted()
    {
        if (timerText != null)
        {
            timerText.text = "Tebrikler! Oyun Tamamlandý!";
        }
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // colCheck is used to stop horizontal moves wrapping.
    private bool SwapIfValid(int i, int offset, int colCheck)
    {
        if (((i % size) != colCheck) && ((i + offset) == emptyLocation))
        {
            // Swap them in game state.
            (pieces[i], pieces[i + offset]) = (pieces[i + offset], pieces[i]);
            // Swap their transforms.
            (pieces[i].localPosition, pieces[i + offset].localPosition) = ((pieces[i + offset].localPosition, pieces[i].localPosition));
            // Update empty location.
            emptyLocation = i;
            return true;
        }
        return false;
    }

    // We name the pieces in order so we can use this to check completion.
    private bool CheckCompletion()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].name != $"{i}")
            {
                return false;
            }
        }
        return true;
    }

    private IEnumerator WaitShuffle(float duration)
    {
        yield return new WaitForSeconds(duration);
        Shuffle();
        shuffling = false;
    }

    private void Shuffle()
    {
        int count = 0;
        int last = 0;
        while (count < (size * size * size))
        {
            int rnd = Random.Range(0, size * size);
            if (rnd == last) { continue; }
            last = emptyLocation;
            if (SwapIfValid(rnd, -size, size))
            {
                count++;
            }
            else if (SwapIfValid(rnd, +size, size))
            {
                count++;
            }
            else if (SwapIfValid(rnd, -1, 0))
            {
                count++;
            }
            else if (SwapIfValid(rnd, +1, size - 1))
            {
                count++;
            }
        }
    }

    // IGameDataProvider implementasyonu
    public int GetCorrectAnswers()
    {
        return levelsCompleted; // Tamamlanan level sayýsý = doðru cevap
    }

    public int GetWrongAnswers()
    {
        // Puzzle oyununda yanlýþ cevap kavramý yok, 
        // ancak eðer süre biterse 1 yanlýþ sayabiliriz
        return gameFinished && timer <= 0 ? 1 : 0;
    }

    public int GetScore()
    {
        return score;
    }

    public float GetTimeSpent()
    {
        return Time.time - gameStartTime;
    }

    // Ek bilgi metotlarý (opsiyonel)
    public int GetTotalMoves()
    {
        return totalMoves;
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    public int GetLevelsCompleted()
    {
        return levelsCompleted;
    }

    public bool IsGameFinished()
    {
        return gameFinished;
    }
}