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
    private int totalHits = 0;        // Toplam vuruþ sayýsý (doðru cevap)
    private int totalMisses = 0;      // Toplam kaçýrma sayýsý (yanlýþ cevap)
    private int bombHits = 0;         // Bomba vuruþlarý
    private int gamesPlayed = 0;      // Oynanan oyun sayýsý
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

        // Oyun baþlangýç zamanýný kaydet
        gameStartTime = Time.time;
        gamesPlayed++;

        Debug.Log($"WhackAMole oyunu baþladý - Oyun #{gamesPlayed}");
    }

    public void GameOver(int type)
    {
        gameFinished = true;

        // Show the message.
        if (type == 0)
        {
            outOfTimeText.SetActive(true);
            Debug.Log("Oyun bitti - Süre doldu!");
        }
        else
        {
            bombText.SetActive(true);
            Debug.Log("Oyun bitti - Bomba patladý!");
        }

        // Hide all moles.
        foreach (Mole mole in moles)
        {
            mole.StopGame();
        }

        // Stop the game and show the start UI.
        playing = false;
        playButton.SetActive(true);

        // GameDataSender'a veri gönder
        if (GameDataSender.Instance != null)
        {
            Debug.Log($"Oyun verileri gönderiliyor - Skor: {score}, Vuruþlar: {totalHits}, Kaçýrma: {totalMisses}");
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
        totalHits++; // Baþarýlý vuruþ sayýsýný artýr
        scoreText.text = $"{score}";

        // Increase time by a little bit.
        timeRemaining += 1;

        // Remove from active moles.
        currentMoles.Remove(moles[moleIndex]);

        Debug.Log($"Köstebek vuruldu! Skor: {score}, Toplam vuruþ: {totalHits}");
    }

    // Bu metot sadece köstebek kaçýrýldýðýnda çaðrýlmalý
    public void Missed(int moleIndex, bool isMole)
    {
        if (isMole)
        {
            // Köstebek kaçýrýldý (süre doldu, vurulmadý)
            timeRemaining -= 2;
            totalMisses++;
            Debug.Log($"Köstebek kaçýrýldý! Toplam kaçýrma: {totalMisses}");
        }
        // Bomba için burada bir þey yapmýyoruz - HitBomb metodu kullanýlacak

        // Remove from active moles.
        currentMoles.Remove(moles[moleIndex]);
    }

    // Bomba vurulduðunda çaðrýlan metot - sadece bomba gerçekten vurulduðunda
    public void HitBomb(int moleIndex)
    {
        bombHits++;
        currentMoles.Remove(moles[moleIndex]);
        Debug.Log($"Bomba vuruldu! Toplam bomba: {bombHits}");
        GameOver(1); // Bomba patladýðýnda oyunu bitir
    }

    // Köstebek vurulduðunda çaðrýlan metot - sadece köstebek vurulduðunda
    public void HitMole(int moleIndex)
    {
        AddScore(moleIndex);
    }

    // Zaman dolduðunda veya köstebek kaçýrýldýðýnda çaðrýlan metot
    public void MoleMissed(int moleIndex)
    {
        Missed(moleIndex, true);
    }

    // Bomba zaman dolduðunda çaðrýlan metot (bomba patlamamalý, sadece kaybolmalý)
    public void BombMissed(int moleIndex)
    {
        // Bomba kaçýrýldý (zaman doldu) - bu iyi bir þey, patlamamalý
        currentMoles.Remove(moles[moleIndex]);
        Debug.Log($"Bomba kaçýrýldý (iyi!) - Ýndeks: {moleIndex}");
    }

    // IGameDataProvider implementasyonu
    public int GetCorrectAnswers()
    {
        return totalHits; // Baþarýlý köstebek vuruþlarý
    }

    public int GetWrongAnswers()
    {
        return totalMisses + bombHits; // Kaçýrma + bomba vuruþlarý
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