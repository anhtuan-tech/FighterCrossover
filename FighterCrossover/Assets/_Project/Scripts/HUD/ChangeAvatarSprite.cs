using UnityEngine;
using UnityEngine.UI; // Bắt buộc phải có để làm việc với UI Canvas

public class ChangeAvatarSprite : MonoBehaviour
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
        if (player1 == null || player2 == null)
        {
            Debug.LogWarning("Vui lòng kéo đầy đủ Target GameObject vào bảng Inspector!");
            return;
        }

        Image uiImage1 = player1.GetComponent<Image>();
        Image uiImage2 = player2.GetComponent<Image>();

        if (uiImage1 == null || uiImage2 == null)
        {
            Debug.LogError($"GameObject không phải là một UI Image hợp lệ (Thiếu component Image)! Hãy kiểm tra lại Canvas.");
            return;
        }

        Sprite loadedSprite1 = LoadSpriteWithFallback(SelectionData.characterImageUrl1);
        Sprite loadedSprite2 = LoadSpriteWithFallback(SelectionData.characterImageUrl2);

        if (loadedSprite1 != null)
        {
            uiImage1.sprite = loadedSprite1;
            uiImage1.enabled = true;
        }
        else if (!string.IsNullOrEmpty(SelectionData.characterImageUrl1))
        {
            Debug.LogWarning($"[ChangeAvatarSprite] Không tìm thấy file Sprite P1 tại: {SelectionData.characterImageUrl1}");
        }

        if (loadedSprite2 != null)
        {
            uiImage2.sprite = loadedSprite2;
            uiImage2.enabled = true;
        }
        else if (!string.IsNullOrEmpty(SelectionData.characterImageUrl2))
        {
            Debug.LogWarning($"[ChangeAvatarSprite] Không tìm thấy file Sprite P2 tại: {SelectionData.characterImageUrl2}");
        }
    }

    private Sprite LoadSpriteWithFallback(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        // 1. Thử load trực tiếp
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null) return sprite;

        // 2. Thử load bằng tên file (bỏ folder)
        string nameOnly = System.IO.Path.GetFileName(path);
        Sprite[] allSprites = Resources.LoadAll<Sprite>("");
        foreach (var s in allSprites)
        {
            if (s.name == nameOnly)
            {
                Debug.Log($"[ChangeAvatarSprite] Dùng fallback tìm sprite '{nameOnly}' thành công.");
                return s;
            }
        }
        return null;
    }

}