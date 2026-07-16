using UnityEngine;

public class MagicCircleFader : MonoBehaviour
{
    public float duration = 0.5f;
    private float elapsed = 0f;
    private SpriteRenderer sr;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        Destroy(gameObject, duration);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        
        // Spin the magic circle
        transform.Rotate(0f, 0f, 180f * Time.deltaTime);

        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(0.85f, 0f, t);
            sr.color = c;
        }
    }
}
