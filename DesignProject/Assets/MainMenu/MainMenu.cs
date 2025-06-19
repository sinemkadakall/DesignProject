using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider volumeSlider;  // Inspector'da sürükleyeceksin

    void Start()
    {
        // MusicPlayer varsa, slider baþlangýç deðerini ayarla
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
