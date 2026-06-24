using UnityEngine;
using UnityEngine.UI;

public class ChangeAvatarSprite : MonoBehaviour
{
    [Header("--- Input ---")]
    public Image targetImage;  // Ô Image trên Canvas bạn muốn thay đổi

    public void ExecuteSpriteChange(Sprite inputSprite)
    {
        // Kiểm tra xem bạn đã kéo thả đủ Object ngoài Inspector chưa để tránh lỗi NullReferenceException
        if (targetImage != null && inputSprite != null)
        {
            targetImage.sprite = inputSprite;
            Debug.Log($"Đã đổi ảnh của {targetImage.gameObject.name} thành {inputSprite.name} thành công!");
        }
        else
        {
            Debug.LogWarning("Vui lòng kéo đầy đủ Target Image và Input Sprite vào bảng Inspector!");
        }
    }
}
