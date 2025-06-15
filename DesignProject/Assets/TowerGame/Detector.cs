// 1. BlockFallDetector.cs - Bu scripti blok prefab�n�za ekleyin
using UnityEngine;

public class BlockFallDetector : MonoBehaviour
{
    [SerializeField] private float fallThreshold = -20f; // Bu Y de�erinin alt�na d��t���nde alg�la
    [SerializeField] private float checkDelay = 1f; // B�rak�ld�ktan sonne ne kadar bekleyece�iz

    private bool hasBeenDropped = false;
    private bool hasFallen = false;
    private NewBlockManager gameManager;

    void Start()
    {
        // GameManager'� bul
        gameManager = FindObjectOfType<NewBlockManager>();
        if (gameManager == null)
        {
            Debug.LogError("NewBlockManager bulunamad�!");
        }
    }

    void Update()
    {
        // E�er blok b�rak�lm��sa ve hen�z d��me kontrol� yap�lmam��sa
        if (hasBeenDropped && !hasFallen)
        {
            // Y pozisyonu threshold'un alt�na d��t�yse
            if (transform.position.y < fallThreshold)
            {
                OnBlockFell();
            }
        }
    }

    // Blok b�rak�ld���nda �a�r�lacak
    public void OnBlockDropped()
    {
        hasBeenDropped = true;
        // Belirli bir s�re sonra kontrol et
        Invoke(nameof(CheckIfStable), checkDelay);
    }

    // Blok stabil mi kontrol et
    private void CheckIfStable()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && rb.velocity.magnitude < 0.1f && transform.position.y > fallThreshold)
        {
            // Blok dura�an ve d��memi� - g�venli
            hasBeenDropped = false;
        }
    }

    // Blok d��t���nde �a�r�lacak
    private void OnBlockFell()
    {
        if (!hasFallen)
        {
            hasFallen = true;
            Debug.Log("Blok d��t�! Can azalt�l�yor.");

            // GameManager'dan can azalt
            if (gameManager != null)
            {
                gameManager.RemoveLife();
            }

            // Blo�u yok et (opsiyonel)
            Destroy(gameObject, 0.5f);
        }
    }

    // Alternatif: Trigger ile tespit
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("FallZone") && hasBeenDropped && !hasFallen)
        {
            OnBlockFell();
        }
    }
}




public class GroundDetector : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // E�er d��en bir blok varsa
        BlockFallDetector blockDetector = other.GetComponent<BlockFallDetector>();
        if (blockDetector != null)
        {
            // Blok zemine �arpt�, can azalt
            NewBlockManager gameManager = FindObjectOfType<NewBlockManager>();
            if (gameManager != null)
            {
                gameManager.RemoveLife();
            }

            // Blo�u yok et
            Destroy(other.gameObject, 0.5f);
        }
    }
}