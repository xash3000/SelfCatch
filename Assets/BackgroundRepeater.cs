using UnityEngine;

public class BackgroundScrollAndClone : MonoBehaviour
{
    public float scrollSpeed = 2f;
    public int repeatCount = 20;

    private float spriteWidth;
    private bool hasSpawned = false;

    void Start()
    {
        spriteWidth = GetComponent<SpriteRenderer>().bounds.size.x;

        // Clone only from the original object
        if (!hasSpawned)
        {
            hasSpawned = true;

            for (int i = 1; i <= repeatCount; i++)
            {
                Vector3 newPosition = transform.position + new Vector3(spriteWidth * i, 0, 0);
                GameObject clone = Instantiate(gameObject, newPosition, Quaternion.identity, transform.parent);

                // Prevent clones from cloning again
                BackgroundScrollAndClone script = clone.GetComponent<BackgroundScrollAndClone>();
                if (script != null)
                {
                    script.hasSpawned = true;
                }
            }
        }
    }

    void Update()
    {
        // Move to the right
        transform.position += Vector3.left * scrollSpeed * Time.deltaTime;
    }
}
