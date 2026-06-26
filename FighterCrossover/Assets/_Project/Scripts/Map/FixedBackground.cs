using UnityEngine;

[ExecuteAlways]
public class FixedBackground : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        if (targetCamera == null) return;
        
        // Lock position to camera
        transform.position = new Vector3(targetCamera.transform.position.x, targetCamera.transform.position.y, transform.position.z);

        // Scale background to fill the screen (Aspect Fill)
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null && spriteRenderer.sprite != null && targetCamera.orthographic)
        {
            float camHeight = targetCamera.orthographicSize * 2f;
            float camWidth = camHeight * targetCamera.aspect;

            float spriteWidth = spriteRenderer.sprite.rect.width / spriteRenderer.sprite.pixelsPerUnit;
            float spriteHeight = spriteRenderer.sprite.rect.height / spriteRenderer.sprite.pixelsPerUnit;

            if (spriteWidth > 0 && spriteHeight > 0)
            {
                float scaleX = camWidth / spriteWidth;
                float scaleY = camHeight / spriteHeight;
                float scale = Mathf.Max(scaleX, scaleY);

                transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
