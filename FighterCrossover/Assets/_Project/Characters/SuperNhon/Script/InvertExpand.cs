using UnityEngine;

public class InvertExpand : MonoBehaviour
{
    [Header("Settings")]
    public float expandDuration = 2f; // Thời gian để skill full màn hình (giây)
    public AnimationCurve expandCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Hiệu ứng bung mượt mà

    private float timeElapsed = 0f;
    private float targetScale;
    private bool isExpanding = true;

    void Start()
    {
        // Đặt kích thước ban đầu bằng 0
        transform.localScale = Vector3.zero;

        // Tính toán độ lớn đường chéo của màn hình để vòng tròn che phủ 100%
        float screenHeight = Camera.main.orthographicSize * 2f;
        float screenWidth = screenHeight * Camera.main.aspect;
        // Định lý Pythagoras để tính đường chéo: √(width² + height²)
        targetScale = Mathf.Sqrt((screenWidth * screenWidth) + (screenHeight * screenHeight));
    }

    void Update()
    {
        if (!isExpanding) return;

        timeElapsed += Time.deltaTime;
        float percent = timeElapsed / expandDuration;

        // Dùng Animation Curve để tạo cảm giác "slowly expand" đẹp mắt hơn
        float curvePercent = expandCurve.Evaluate(percent);
        float currentScale = Mathf.Lerp(0, targetScale, curvePercent);

        transform.localScale = new Vector3(currentScale, currentScale, 1f);

        // Dừng lại khi đã full màn hình
        if (percent >= 1f)
        {
            transform.localScale = new Vector3(targetScale, targetScale, 1f);
            isExpanding = false;
            OnExpandComplete();
        }
    }

    void OnExpandComplete()
    {
        // Code xử lý khi skill đã bao phủ toàn màn hình
        Debug.Log("Chaos Control: Đóng băng thời gian / Full màn hình!");

        // Bạn có thể thêm Coroutine ở đây để giữ hiệu ứng trong vài giây, 
        // sau đó thu nhỏ lại hoặc Destroy(gameObject).
    }
}