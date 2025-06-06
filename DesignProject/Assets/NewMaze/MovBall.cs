using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MovBall : MonoBehaviour
{
    private Rigidbody rb;
    public float speed = 1.9f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        float yatay = Input.GetAxis("Horizontal"); // sað-sol
        float dikey = Input.GetAxis("Vertical");   // ileri-geri

        Vector3 kuvvet = new Vector3(yatay, 0, dikey); // X,Z ekseninde hareket
        rb.AddForce(kuvvet * speed);
      
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Finish"))
        {
            // Sahneyi yeniden yükle
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
