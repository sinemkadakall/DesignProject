using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PuzzleGameManager : MonoBehaviour
{
    [SerializeField] private Transform gameTransform;
    [SerializeField] private Transform piecePrefab;

    // Level sistemi i�in yeni de�i�kenler
    [SerializeField] private Material level1Material;  // �lk level i�in materyal
    [SerializeField] private Material level2Material;  // �kinci level i�in materyal
    [SerializeField] private TextMeshProUGUI levelText; // Level g�stergesi

    private List<Transform> pieces;
    private int emptyLocation;
    private int size;
    private bool shuffling = false;

    // Level sistemi
    private int currentLevel = 1;
    private const int maxLevel = 2;

    [SerializeField] private TextMeshProUGUI timerText;
    private float timer = 60f;
    private const float levelTime = 60f; // Her level i�in s�re

    // Create the game setup with size x size pieces.
    private void CreateGamePieces(float gapThickness)
    {
        // �nceki par�alar� temizle
        ClearPieces();

        // This is the width of each tile.
        float width = 1 / (float)size;
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                Transform piece = Instantiate(piecePrefab, gameTransform);
                pieces.Add(piece);

                // Mevcut seviyeye g�re materyali ata
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

    // �nceki par�alar� temizle
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
        UpdateUI();
        CreateGamePieces(0.01f);

        // �lk ba�ta oyunu kar��t�r
        shuffling = true;
        StartCoroutine(WaitShuffle(0.5f));
    }

    // UI g�ncellemesi
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
            timerText.text = "S�re: " + Mathf.CeilToInt(timer).ToString() + "s";
        }

        // S�re bitti�inde sahneyi yeniden y�kle
        if (timer <= 0f)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // Oyun tamamland� m� kontrol et (sadece kar��t�rma bitmi�se)
        if (!shuffling && CheckCompletion())
        {
            OnLevelComplete();
        }

        // Sol t�k alg�lama (3D Raycast)
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
                        // Kom�u y�nleri kontrol et (yukar�, a�a��, sol, sa�)
                        if (SwapIfValid(i, -size, size)) break; // Yukar�
                        if (SwapIfValid(i, +size, size)) break; // A�a��
                        if (SwapIfValid(i, -1, 0)) break;       // Sol
                        if (SwapIfValid(i, +1, size - 1)) break; // Sa�
                    }
                }
            }
        }
    }

    // Level tamamland���nda �a�r�l�r
    private void OnLevelComplete()
    {
        if (currentLevel < maxLevel)
        {
            // Sonraki levela ge�
            currentLevel++;
            timer = levelTime; // S�reyi s�f�rla
            UpdateUI();
            shuffling = true;
            StartCoroutine(NextLevelTransition());
        }
        else
        {
            // Oyun tamamland� - sahneyi yeniden y�kle veya kazanma ekran� g�ster
            StartCoroutine(GameCompleted());
        }
    }

    // Sonraki level ge�i�i
    private IEnumerator NextLevelTransition()
    {
        yield return new WaitForSeconds(1f); // K�sa bir bekleme
        CreateGamePieces(0.01f); // Yeni seviye par�alar�n� olu�tur
        yield return new WaitForSeconds(0.5f);
        Shuffle(); // Kar��t�r
        shuffling = false;
    }

    // Oyun tamamland���nda
    private IEnumerator GameCompleted()
    {
        if (timerText != null)
        {
            timerText.text = "Tebrikler! Oyun Tamamland�!";
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
}