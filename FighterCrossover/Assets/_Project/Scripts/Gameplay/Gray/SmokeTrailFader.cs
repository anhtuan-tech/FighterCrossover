using UnityEngine;

public class SmokeTrailFader : MonoBehaviour
{
    public float fadeSpeed = 3f;
    private SpriteRenderer sr;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        Destroy(gameObject, 1f / fadeSpeed);
    }

    private void Update()
    {
        if (sr != null)
        {
            Color c = sr.color;
            c.a -= fadeSpeed * Time.deltaTime;
            sr.color = c;
        }
        if (transform.localScale.x > 0)
        {
            transform.localScale -= Vector3.one * (fadeSpeed * 0.8f * Time.deltaTime);
        }
    }
}
