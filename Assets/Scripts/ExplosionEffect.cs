using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    [Header("Animation")]
    public Sprite[] explosionSprites;
    public float frameRate = 15f;
    
    private SpriteRenderer spriteRenderer;
    private int currentFrame = 0;
    private float frameTimer = 0f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (explosionSprites.Length > 0)
        {
            spriteRenderer.sprite = explosionSprites[0];
        }
    }

    private void Update()
    {
        if (explosionSprites.Length == 0) return;

        frameTimer += Time.deltaTime;
        
        if (frameTimer >= 1f / frameRate)
        {
            frameTimer = 0f;
            currentFrame++;
            
            if (currentFrame >= explosionSprites.Length)
            {
                // Animation finished, destroy the effect
                Destroy(gameObject);
                return;
            }
            
            spriteRenderer.sprite = explosionSprites[currentFrame];
        }
    }
}
