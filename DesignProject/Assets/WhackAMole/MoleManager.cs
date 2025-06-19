using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoleManager : MonoBehaviour, IGameDataProvider
{
    [SerializeField] private List<Mole> moles;
    [Header("UI objects")]
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject outOfTimeText;
    [SerializeField] private GameObject bombText;
    [SerializeField] private TMPro.TextMeshProUGUI timeText;
    [SerializeField] private TMPro.TextMeshProUGUI scoreText;

    // Hardcoded variables you may want to tune.
    private float startingTime = 30f;

    // Global variables
    private float timeRemaining;
    private HashSet<Mole> currentMoles = new HashSet<Mole>();
    private int score;
    private bool playing = false;

    // GameDataSender i�in veri de�i�kenleri
    private float gameStartTime;
    private int totalHits = 0;        // Toplam vuru� say�s� (do�ru cevap)
    private int totalMisses = 0;      // Toplam ka��rma say�s� (yanl�� cevap)
    private int bombHits = 0;         // Bomba vuru�lar�
    private int gamesPlayed = 0;      // Oynanan oyun say�s�
    private bool gameFinished = false;

    // This is public so the play button can see it.
    public void StartGame()
    {
        // Hide/show the UI elements we don't/do want to see.
        playButton.SetActive(false);
        outOfTimeText.SetActive(false);
        bombText.SetActive(false);
        gameUI.SetActive(true);

        // Hide all the visible moles.
        for (int i = 0; i < moles.Count; i++)
        {
            moles[i].Hide();
            moles[i].SetIndex(i);
        }

        // Remove any old game state.
        currentMoles.Clear();

        // Start with 30 seconds.
        timeRemaining = startingTime;
        score = 0;
        scoreText.text = "0";
        playing = true;
        gameFinished = false;

        // Oyun ba�lang�� zaman�n� kaydet
        gameStartTime = Time.time;
        gamesPlayed++;

        Debug.Log($"WhackAMole oyunu ba�lad� - Oyun #{gamesPlayed}");
    }

    public void GameOver(int type)
    {
        gameFinished = true;

        // Show the message.
        if (type == 0)
        {
            outOfTimeText.SetActive(true);
            Debug.Log("Oyun bitti - S�re doldu!");
        }
        else
        {
            bombText.SetActive(true);
            Debug.Log("Oyun bitti - Bomba patlad�!");
        }

        // Hide all moles.
        foreach (Mole mole in moles)
        {
            mole.StopGame();
        }

        // Stop the game and show the start UI.
        playing = false;
        playButton.SetActive(true);

        // GameDataSender'a veri g�nder
        if (GameDataSender.Instance != null)
        {
            Debug.Log($"Oyun verileri g�nderiliyor - Skor: {score}, Vuru�lar: {totalHits}, Ka��rma: {totalMisses}");
            GameDataSender.Instance.SendSessionData();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (playing)
        {
            // Update time.
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                GameOver(0);
            }
            timeText.text = $"{(int)timeRemaining / 60}:{(int)timeRemaining % 60:D2}";

            // Check if we need to start any more moles.
            if (currentMoles.Count <= (score / 10))
            {
                // Choose a random mole.
                int index = Random.Range(0, moles.Count);
                // Doesn't matter if it's already doing something, we'll just try again next frame.
                if (!currentMoles.Contains(moles[index]))
                {
                    currentMoles.Add(moles[index]);
                    moles[index].Activate(score / 10);
                }
            }
        }
    }

    public void AddScore(int moleIndex)
    {
        // Add and update score.
        score += 1;
        totalHits++; // Ba�ar�l� vuru� say�s�n� art�r
        scoreText.text = $"{score}";

        // Increase time by a little bit.
        timeRemaining += 1;

        // Remove from active moles.
        currentMoles.Remove(moles[moleIndex]);

        Debug.Log($"K�stebek vuruldu! Skor: {score}, Toplam vuru�: {totalHits}");
    }

    public void Missed(int moleIndex, bool isMole)
    {
        if (isMole)
        {
            // Decrease time by a little bit.
            timeRemaining -= 2;
            totalMisses++; // Ka��rma say�s�n� art�r
            Debug.Log($"K�stebek ka��r�ld�! Toplam ka��rma: {totalMisses}");
        }
        else
        {
            // Bomba vuruldu
            bombHits++;
            Debug.Log($"Bomba vuruldu! Toplam bomba: {bombHits}");
            GameOver(1); // Bomba patlad���nda oyunu bitir
            return;
        }

        // Remove from active moles.
        currentMoles.Remove(moles[moleIndex]);
    }

    // Bomba vuruldu�unda �a�r�lan metot (e�er yoksa ekleyin)
    public void HitBomb(int moleIndex)
    {
        bombHits++;
        currentMoles.Remove(moles[moleIndex]);
        Debug.Log($"Bomba vuruldu! Toplam bomba: {bombHits}");
        GameOver(1);
    }

    // IGameDataProvider implementasyonu
    public int GetCorrectAnswers()
    {
        return totalHits; // Ba�ar�l� k�stebek vuru�lar�
    }

    public int GetWrongAnswers()
    {
        return totalMisses + bombHits; // Ka��rma + bomba vuru�lar�
    }

    public int GetScore()
    {
        return score;
    }

    public float GetTimeSpent()
    {
        if (gameStartTime > 0)
        {
            return gameFinished ? (startingTime - timeRemaining) : (Time.time - gameStartTime);
        }
        return 0f;
    }

    // Ek bilgi metotlar� (opsiyonel)
    public int GetTotalHits()
    {
        return totalHits;
    }

    public int GetTotalMisses()
    {
        return totalMisses;
    }

    public int GetBombHits()
    {
        return bombHits;
    }

    public int GetGamesPlayed()
    {
        return gamesPlayed;
    }

    public float GetRemainingTime()
    {
        return timeRemaining;
    }

    public bool IsPlaying()
    {
        return playing;
    }

    public bool IsGameFinished()
    {
        return gameFinished;
    }

    // Accuracy hesaplama
    public float GetAccuracy()
    {
        int totalAttempts = totalHits + totalMisses;
        return totalAttempts > 0 ? (float)totalHits / totalAttempts * 100f : 0f;
    }
}