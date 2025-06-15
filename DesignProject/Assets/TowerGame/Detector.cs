// 1. BlockFallDetector.cs - Bu scripti blok prefabýnýza ekleyin
using UnityEngine;

public class BlockFallDetector : MonoBehaviour
{
    [SerializeField] private float fallThreshold = -20f; // Bu Y deðerinin altýna düþtüðünde algýla
    [SerializeField] private float checkDelay = 1f; // Býrakýldýktan sonne ne kadar bekleyeceðiz

    private bool hasBeenDropped = false;
    private bool hasFallen = false;
    private NewBlockManager gameManager;

    void Start()
    {
        // GameManager'ý bul
        gameManager = FindObjectOfType<NewBlockManager>();
        if (gameManager == null)
        {
            Debug.LogError("NewBlockManager bulunamadý!");
        }
    }

    void Update()
    {
        // Eðer blok býrakýlmýþsa ve henüz düþme kontrolü yapýlmamýþsa
        if (hasBeenDropped && !hasFallen)
        {
            // Y pozisyonu threshold'un altýna düþtüyse
            if (transform.position.y < fallThreshold)
            {
                OnBlockFell();
            }
        }
    }

    // Blok býrakýldýðýnda çaðrýlacak
    public void OnBlockDropped()
    {
        hasBeenDropped = true;
        // Belirli bir süre sonra kontrol et
        Invoke(nameof(CheckIfStable), checkDelay);
    }

    // Blok stabil mi kontrol et
    private void CheckIfStable()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && rb.velocity.magnitude < 0.1f && transform.position.y > fallThreshold)
        {
            // Blok duraðan ve düþmemiþ - güvenli
            hasBeenDropped = false;
        }
    }

    // Blok düþtüðünde çaðrýlacak
    private void OnBlockFell()
    {
        if (!hasFallen)
        {
            hasFallen = true;
            Debug.Log("Blok düþtü! Can azaltýlýyor.");

            // GameManager'dan can azalt
            if (gameManager != null)
            {
                gameManager.RemoveLife();
            }

            // Bloðu yok et (opsiyonel)
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
        // Eðer düþen bir blok varsa
        BlockFallDetector blockDetector = other.GetComponent<BlockFallDetector>();
        if (blockDetector != null)
        {
            // Blok zemine çarptý, can azalt
            NewBlockManager gameManager = FindObjectOfType<NewBlockManager>();
            if (gameManager != null)
            {
                gameManager.RemoveLife();
            }

            // Bloðu yok et
            Destroy(other.gameObject, 0.5f);
        }
    }
}