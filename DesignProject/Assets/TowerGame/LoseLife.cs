using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoseLife : MonoBehaviour
{
    // [SerializeField] private BlockManager gameManager;

    /* private void OnTriggerEnter2D(Collider2D collision)
     {
         gameManager.RemoveLife();
     }*/

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Block"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
