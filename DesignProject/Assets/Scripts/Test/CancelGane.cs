using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class ReturnToMainScene : MonoBehaviour
{
    public void ReturnToMain()
    {
        string previousScene = PlayerPrefs.GetString("PreviousScene", "SampleScene");
        PlayerPrefs.SetString("ReturnFromMiniGame", "true");
        SceneManager.LoadScene(previousScene);
    }
}
