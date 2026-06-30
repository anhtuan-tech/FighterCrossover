using UnityEngine;

public class FightingCameraController : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private FighterBase[] fighters;
    [SerializeField] private float updateInterval = 0.5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 100f;
    [SerializeField] private float yOffset = 2f;
    [SerializeField] private float minY = -2f;
    [SerializeField] private float maxY = 3f;
    [SerializeField] private float minX = -1.17f;
    [SerializeField] private float maxX = 1.17f;

    [Header("Zoom")]
    [SerializeField] private float minSize = 3f;
    [SerializeField] private float maxSize = 8f;
    [SerializeField] private float zoomSpeed = 100f;
    [SerializeField] private float paddingX = 0.5f;
    [SerializeField] private float paddingY = 0.5f;

    private Camera cam;
    private float nextUpdateTime;

    private void Start()
    {
        cam = GetComponent<Camera>();
        FindFighters();
    }

    private void FindFighters()
    {
        fighters = FindObjectsOfType<FighterBase>();
    }

    private void LateUpdate()
    {
        if (Time.time > nextUpdateTime)
        {
            FindFighters();
            nextUpdateTime = Time.time + updateInterval;
        }

        if (fighters == null || fighters.Length == 0) return;

        float minPlayerX = float.MaxValue;
        float maxPlayerX = float.MinValue;
        float minPlayerY = float.MaxValue;
        float maxPlayerY = float.MinValue;
        int activeCount = 0;

        Vector3 sumPosition = Vector3.zero;

        for (int i = 0; i < fighters.Length; i++)
        {
            if (fighters[i] != null && fighters[i].gameObject.activeInHierarchy)
            {
                if (fighters[i].CurrentState == FighterState.Dead) continue;

                Vector3 pos = fighters[i].transform.position;
                if (pos.x < minPlayerX) minPlayerX = pos.x;
                if (pos.x > maxPlayerX) maxPlayerX = pos.x;
                if (pos.y < minPlayerY) minPlayerY = pos.y;
                if (pos.y > maxPlayerY) maxPlayerY = pos.y;

                sumPosition += pos;
                activeCount++;
            }
        }

        if (activeCount == 0) return;

        // 1. TÍNH TOÁN ZOOM TRƯỚC
        if (cam != null && cam.orthographic)
        {
            float targetSize = minSize;
            if (activeCount > 1)
            {
                // Chỉ lấy khoảng cách chiều ngang (như đã sửa ở bước trước)
                float distanceX = maxPlayerX - minPlayerX;
                float sizeX = (distanceX + paddingX) / (cam.aspect * 2f);
                targetSize = Mathf.Clamp(sizeX, minSize, maxSize);
            }

            // Thay đổi size của camera mượt mà
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, zoomSpeed * Time.deltaTime);
        }

        // 2. TÍNH TOÁN VỊ TRÍ VÀ BÙ TRỪ TRỤC Y
        Vector3 midpoint = sumPosition / activeCount;
        float targetX = Mathf.Clamp(midpoint.x, minX, maxX);

        // Tính độ chênh lệch zoom hiện tại so với lúc nhỏ nhất
        float zoomDifference = cam.orthographicSize - minSize;

        // Cộng phần chênh lệch này vào minY để đẩy camera lên trên khi zoom out
        float adjustedMinY = minY + zoomDifference;

        // Dùng adjustedMinY thay cho minY gốc
        float targetY = Mathf.Clamp(midpoint.y + yOffset, adjustedMinY, maxY);

        Vector3 targetPos = new Vector3(targetX, targetY, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, moveSpeed * Time.deltaTime);
    }
}
