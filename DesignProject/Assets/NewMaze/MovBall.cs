using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MovBall : MonoBehaviour
{

    private Rigidbody rb;
    public float speed = 1.9f;

    [Header("Timer Settings")]
    public float gameTime = 60f; // 60 saniye
    private float currentTime;
    public TextMeshProUGUI timerText; // UI TextMeshPro komponenti

    private bool gameActive = true;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentTime = gameTime;

        // E�er timerText atanmam��sa, otomatik olarak bul
        if (timerText == null)
        {
            timerText = GameObject.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
        }

        UpdateTimerDisplay();
    }

    private void Update()
    {
        if (gameActive)
        {
            // Zaman� azalt
            currentTime -= Time.deltaTime;
            UpdateTimerDisplay();

            // S�re biterse oyunu yeniden ba�lat
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
            float yatay = Input.GetAxis("Horizontal"); // sa�-sol
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

            // S�re azald�k�a renk de�i�tir (opsiyonel)
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
        Debug.Log("S�re doldu! Oyun yeniden ba�l�yor...");

        // K�sa bir bekleme s�resi ekle, sonra sahneyi yeniden y�kle
        StartCoroutine(RestartAfterDelay(1f));
    }

    private void LevelCompleted()
    {
        Debug.Log("Level tamamland�!");

        string currentSceneName = SceneManager.GetActiveScene().name;

        // Hangi levelda oldu�umuzu kontrol et ve uygun sahneye ge�
        if (currentSceneName.Contains("Level 1") || currentSceneName.Contains("level 1") || currentSceneName.Contains("Maze") && !currentSceneName.Contains("NewMaze 2"))
        {
            // Level 1 tamamland�, Level 2'ye ge�
            Debug.Log("Level 1 tamamland�! Level 2'ye ge�iliyor...");
            SceneManager.LoadScene("NewMaze 2");
        }
        else if (currentSceneName == "NewMaze 2")
        {
            // Level 2 tamamland�, ana sahneye d�n
            Debug.Log("Level 2 tamamland�! Ana sahneye d�n�l�yor...");
            SceneManager.LoadScene("SampleScene");
        }
        else
        {
            // Varsay�lan durumda ana sahneye d�n
            Debug.Log("Oyun tamamland�! Ana sahneye d�n�l�yor...");
            SceneManager.LoadScene("SampleScene");
        }
    }

    private IEnumerator RestartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
