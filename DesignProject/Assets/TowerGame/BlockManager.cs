using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    [SerializeField] private Transform blockPrefab;
    [SerializeField] private Transform blockHolder;

    [SerializeField] private TMPro.TextMeshProUGUI livesText;

    private Transform currentBlock = null;
    private Rigidbody currentRigidbody;

    private Vector2 blockStartPosition = new Vector2(0f, 4f);

    private float blockSpeed = 8f;
    private float blockSpeedIncrement = 0.5f;
    private int blockDirection = 1;
    private float xLimit = 5;

    private float timeBetweenRounds = 1f;

    // Variables to handle the game state.
    private int startingLives = 3;
    private int livesRemaining;
    private bool playing = true;

    // Start is called before the first frame update
    void Start()
    {
        livesRemaining = startingLives;
        livesText.text = $"{livesRemaining}";
        SpawnNewBlock();
    }

    /* private void SpawnNewBlock()
     {
         // Create a block with the desired properties.
         currentBlock = Instantiate(blockPrefab, blockHolder);
         currentBlock.position = blockStartPosition;
        // currentBlock.GetComponent<MeshRenderer>().material.color = Random.ColorHSV();
         currentBlock.GetComponent<MeshRenderer>().material.color = Random.ColorHSV();

         currentRigidbody = currentBlock.GetComponent<Rigidbody>();
         // Increase the block speed each time to make it harder.
         blockSpeed += blockSpeedIncrement;
     }*/
    private void SpawnNewBlock()
    {
        currentBlock = Instantiate(blockPrefab, blockHolder);
        currentBlock.position = blockStartPosition;
        currentBlock.GetComponent<MeshRenderer>().material.color = Random.ColorHSV();

        currentBlock.tag = "Block"; // <-- ETÝKET EKLENDÝ

        currentRigidbody = currentBlock.GetComponent<Rigidbody>();
        blockSpeed += blockSpeedIncrement;
    }


    private IEnumerator DelayedSpawn()
    {
        yield return new WaitForSeconds(timeBetweenRounds);
        SpawnNewBlock();

    }

    // Update is called once per frame
    void Update()
    {
        // If we have a waiting block, move it about.
        if (currentBlock && playing)
        {
            float moveAmount = Time.deltaTime * blockSpeed * blockDirection;
            currentBlock.position += new Vector3(moveAmount, 0, 0);
            // If we've gone as far as we want, reverse direction.
            if (Mathf.Abs(currentBlock.position.x) > xLimit)
            {
                // Set it to the limit so it doesn't go further.
                currentBlock.position = new Vector3(blockDirection * xLimit, currentBlock.position.y, 0);
                blockDirection = -blockDirection;
            }

            // If we press space drop the block.
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Stop it moving.
                currentBlock = null;
                // Activate the RigidBody to enable gravity to drop it.
                currentRigidbody.isKinematic = true;
                // Spawn the next block.
                StartCoroutine(DelayedSpawn());
            }
        }

        // Temporarily assign a key to restart the game.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }

    // Called from LoseLife whenever it detects a block has fallen off.
    public void RemoveLife()
    {
        // Update the lives remaining UI element.
        livesRemaining = Mathf.Max(livesRemaining - 1, 0);
        livesText.text = $"{livesRemaining}";
        // Check for end of game.
        if (livesRemaining == 0)
        {
            playing = false;
        }
    }
}
