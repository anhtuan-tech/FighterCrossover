using UnityEngine;
using UnityEngine.UI;

public class ChangeSupportAvatarSprite : MonoBehaviour
{
    [Header("--- Input ---")]
    public GameObject player1;
    public GameObject player2;

    void Start()
    {
        ExecuteSpriteChange();
    }

    /// <summary>
    /// Hàm thay đổi Sprite cho một GameObject UI dựa trên đường dẫn từ thư mục Resources
    /// </summary>
    /// <param name="spritePath">Đường dẫn ảnh (Ví dụ: "Avatars/Goku_Icon")</param>
    public void ExecuteSpriteChange()
    {
        // 1. Kiểm tra xem bạn đã kéo GameObject mục tiêu vào chưa
        if (player1 == null || player2 == null)
        {
            Debug.LogWarning("Vui lòng kéo đầy đủ Target GameObject vào bảng Inspector!");
            return;
        }

        // 3. Lấy thành phần Image (UI) từ GameObject đó ra để xử lý ảnh
        Image uiImage1 = player1.GetComponent<Image>();
        Image uiImage2 = player2.GetComponent<Image>();

        if (uiImage1 == null || uiImage2 == null)
        {
            Debug.LogError($"GameObject không phải là một UI Image hợp lệ (Thiếu component Image)! Hãy kiểm tra lại Canvas.");
            return;
        }

        // 4. Load Sprite từ thư mục Resources dựa trên đường dẫn string
        Sprite loadedSprite1 = Resources.Load<Sprite>(SelectionData.supportImageUrl1);
        Sprite loadedSprite2 = Resources.Load<Sprite>(SelectionData.supportImageUrl2);

        // 5. Nếu tìm thấy ảnh thì tiến hành gán vào UI Image
        if (loadedSprite1 != null && loadedSprite2 != null)
        {
            uiImage1.sprite = loadedSprite1;
            uiImage2.sprite = loadedSprite2;
            Debug.Log($"Đã đổi ảnh UI của GameObject thành công!");
        }
        else
        {
            Debug.LogError($"Không tìm thấy file Sprite nào tại đường dẫn Resources");
        }
    }
}
