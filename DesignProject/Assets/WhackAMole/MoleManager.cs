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

    private float startingTime = 30f;

    private float timeRemaining;
    private HashSet<Mole> currentMoles = new HashSet<Mole>();
    private int score;
    private bool playing = false;

    private float gameStartTime;
    private int totalHits = 0;        // Toplam vuru� say�s� (do�ru cevap)
    private int totalMisses = 0;      // Toplam ka��rma say�s� (yanl�� cevap)
    private int bombHits = 0;         // Bomba vuru�lar�
    private int gamesPlayed = 0;      // Oynanan oyun say�s�
    private bool gameFinished = false;

    public void StartGame()
    {
        playButton.SetActive(false);
        outOfTimeText.SetActive(false);
        bombText.SetActive(false);
        gameUI.SetActive(true);

        for (int i = 0; i < moles.Count; i++)
        {
            moles[i].Hide();
            moles[i].SetIndex(i);
        }

        currentMoles.Clear();

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

    // Bu metot sadece k�stebek ka��r�ld���nda �a�r�lmal�
    public void Missed(int moleIndex, bool isMole)
    {
        if (isMole)
        {
            // K�stebek ka��r�ld� (s�re doldu, vurulmad�)
            timeRemaining -= 2;
            totalMisses++;
            Debug.Log($"K�stebek ka��r�ld�! Toplam ka��rma: {totalMisses}");
        }
        // Bomba i�in burada bir �ey yapm�yoruz - HitBomb metodu kullan�lacak

        // Remove from active moles.
        currentMoles.Remove(moles[moleIndex]);
    }

    // Bomba vuruldu�unda �a�r�lan metot - sadece bomba ger�ekten vuruldu�unda
    public void HitBomb(int moleIndex)
    {
        bombHits++;
        currentMoles.Remove(moles[moleIndex]);
        Debug.Log($"Bomba vuruldu! Toplam bomba: {bombHits}");
        GameOver(1); // Bomba patlad���nda oyunu bitir
    }

    // K�stebek vuruldu�unda �a�r�lan metot - sadece k�stebek vuruldu�unda
    public void HitMole(int moleIndex)
    {
        AddScore(moleIndex);
    }

    // Zaman doldu�unda veya k�stebek ka��r�ld���nda �a�r�lan metot
    public void MoleMissed(int moleIndex)
    {
        Missed(moleIndex, true);
    }

    // Bomba zaman doldu�unda �a�r�lan metot (bomba patlamamal�, sadece kaybolmal�)
    public void BombMissed(int moleIndex)
    {
        // Bomba ka��r�ld� (zaman doldu) - bu iyi bir �ey, patlamamal�
        currentMoles.Remove(moles[moleIndex]);
        Debug.Log($"Bomba ka��r�ld� (iyi!) - �ndeks: {moleIndex}");
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