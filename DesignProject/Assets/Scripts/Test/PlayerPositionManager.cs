using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPositionManager : MonoBehaviour
{
    // Singleton kaldýrýldý - daha güvenli yaklaþým
    void Start()
    {
        // Sahne yüklendiðinde pozisyonu kontrol et
        LoadPlayerPosition();
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

                // Flag'i temizle
                PlayerPrefs.SetString("ReturnFromMiniGame", "false");

                Debug.Log("Player position loaded: " + position);
            }
        }
    }
}