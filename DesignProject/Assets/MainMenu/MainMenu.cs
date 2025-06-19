using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider volumeSlider;  // Inspector'da s�r�kleyeceksin

    void Start()
    {
        // MusicPlayer varsa, slider ba�lang�� de�erini ayarla
        if (volumeSlider != null && MusicPlayer.instance != null)
        {
            volumeSlider.value = MusicPlayer.instance.GetComponent<AudioSource>().volume;
            volumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void SetMusicVolume(float volume)
    {
        if (MusicPlayer.instance != null)
        {
            MusicPlayer.instance.SetVolume(volume);
        }
    }
}
