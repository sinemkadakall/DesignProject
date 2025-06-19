using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPositionManager : MonoBehaviour
{
    // Inspector'dan ayarlamak yerine, mevcut pozisyonu baþlangýç pozisyonu olarak kullan
    private Vector3 defaultStartPosition;
    private Quaternion defaultStartRotation;

    void Awake()
    {
        // Baþlangýç pozisyonunu kaydet (Inspector'daki mevcut pozisyon)
        defaultStartPosition = transform.position;
        defaultStartRotation = transform.rotation;
    }

    void Start()
    {
        // Oyun ilk kez baþlatýldýðýnda pozisyon verilerini temizle
        if (!PlayerPrefs.HasKey("GameStarted"))
        {
            ClearPositionData();
            PlayerPrefs.SetInt("GameStarted", 1);
            PlayerPrefs.Save();
        }

        LoadPlayerPosition();
    }

    public void ClearPositionData()
    {
        PlayerPrefs.DeleteKey("PlayerPosX");
        PlayerPrefs.DeleteKey("PlayerPosY");
        PlayerPrefs.DeleteKey("PlayerPosZ");
        PlayerPrefs.DeleteKey("PlayerRotX");
        PlayerPrefs.DeleteKey("PlayerRotY");
        PlayerPrefs.DeleteKey("PlayerRotZ");
        PlayerPrefs.DeleteKey("PlayerRotW");
        PlayerPrefs.SetString("ReturnFromMiniGame", "false");
        PlayerPrefs.Save();

        // Karakteri baþlangýç pozisyonuna getir
        transform.position = defaultStartPosition;
        transform.rotation = defaultStartRotation;

        Debug.Log("Position data cleared and reset to default: " + defaultStartPosition);
    }

    public void SavePlayerPosition()
    {
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        PlayerPrefs.SetFloat("PlayerPosX", position.x);
        PlayerPrefs.SetFloat("PlayerPosY", position.y);
        PlayerPrefs.SetFloat("PlayerPosZ", position.z);
        PlayerPrefs.SetFloat("PlayerRotX", rotation.x);
        PlayerPrefs.SetFloat("PlayerRotY", rotation.y);
        PlayerPrefs.SetFloat("PlayerRotZ", rotation.z);
        PlayerPrefs.SetFloat("PlayerRotW", rotation.w);
        PlayerPrefs.Save();

        Debug.Log("Player position saved: " + position);
    }

    public void LoadPlayerPosition()
    {
        // Mini oyundan dönüp dönmediðini kontrol et
        if (PlayerPrefs.GetString("ReturnFromMiniGame") == "true")
        {
            if (PlayerPrefs.HasKey("PlayerPosX"))
            {
                Vector3 position = new Vector3(
                    PlayerPrefs.GetFloat("PlayerPosX"),
                    PlayerPrefs.GetFloat("PlayerPosY"),
                    PlayerPrefs.GetFloat("PlayerPosZ")
                );
                Quaternion rotation = new Quaternion(
                    PlayerPrefs.GetFloat("PlayerRotX"),
                    PlayerPrefs.GetFloat("PlayerRotY"),
                    PlayerPrefs.GetFloat("PlayerRotZ"),
                    PlayerPrefs.GetFloat("PlayerRotW")
                );

                transform.position = position;
                transform.rotation = rotation;

                Debug.Log("Player position loaded: " + position);
            }
            // Flag'i temizle
            PlayerPrefs.SetString("ReturnFromMiniGame", "false");
        }
        else
        {
            // Mini oyundan dönmüyorsa baþlangýç pozisyonunu kullan
            transform.position = defaultStartPosition;
            transform.rotation = defaultStartRotation;
            Debug.Log("Player reset to start position: " + defaultStartPosition);
        }
    }

    // Oyunu tamamen yeniden baþlatmak için bu fonksiyonu çaðýrýn
    public void ResetGame()
    {
        ClearPositionData();
        PlayerPrefs.DeleteKey("GameStarted");
        PlayerPrefs.Save();
    }
}