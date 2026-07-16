using UnityEngine;

/// <summary>
/// Plays a sequence of sprites as a one-shot explosion animation and destroys itself
/// </summary>
public class SpriteExplosionEffect : MonoBehaviour
{
    public Sprite[] frames;
    public float frameRate = 12f;
    private SpriteRenderer sr;
    private int currentFrame = 0;
    private float timer = 0f;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (frames == null || frames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }
        if (sr != null) sr.sprite = frames[0];
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 1f / frameRate)
        {
            timer -= 1f / frameRate;
            currentFrame++;
            if (currentFrame >= frames.Length)
            {
                Destroy(gameObject);
            }
            else
            {
                if (sr != null) sr.sprite = frames[currentFrame];
            }
        }
    }
}
