using UnityEngine;
using UnityEngine.SceneManagement;

public class MiniGameTrigger : MonoBehaviour
{
    [SerializeField] private string miniGameSceneName;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private GameObject interactionUI;

    private bool playerNearby = false;

    void Update()
    {
        if (playerNearby && Input.GetKeyDown(interactionKey))
        {
            LoadMiniGame();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
            if (interactionUI != null)
                interactionUI.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            if (interactionUI != null)
                interactionUI.SetActive(false);
        }
    }

    void LoadMiniGame()
    {
        // Player'ý bul ve pozisyonunu kaydet
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerPositionManager posManager = player.GetComponent<PlayerPositionManager>();
            if (posManager != null)
            {
                posManager.SavePlayerPosition();
            }
        }

        // Sahne bilgilerini kaydet
        PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.SetString("ReturnFromMiniGame", "true");
        PlayerPrefs.Save();

        // Mini oyun sahnesini yükle
        SceneManager.LoadScene(miniGameSceneName);
    }
}

