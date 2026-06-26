using UnityEngine;
using UnityEngine.UI;

public class FighterStatMonitor : MonoBehaviour
{
    [Header("--- UI Sliders ---")]
    public Slider healthSlider;
    public Slider manaSlider;
    public Slider staminaSlider;

    // Lưu trữ tham chiếu tới nhân vật cần theo dõi
    private FighterBase targetFighter;
    private bool isInitialized = false;

    /// <summary>
    /// Hàm này được gọi từ LoadCharacter để truyền nhân vật vào cho UI theo dõi
    /// </summary>
    public void Initialize(FighterBase fighter)
    {
        if (fighter == null) return;

        targetFighter = fighter;

        // Thiết lập giá trị tối đa (Max Value) cho các Slider dựa trên chỉ số gốc của nhân vật
        if (healthSlider != null) healthSlider.maxValue = targetFighter.stats.maxHp;
        if (manaSlider != null) manaSlider.maxValue = targetFighter.stats.mana;
        if (staminaSlider != null) staminaSlider.maxValue = targetFighter.stats.maxStamina;

        isInitialized = true;
    }

    void Update()
    {
        // Nếu chưa được gán nhân vật hoặc nhân vật bị hủy thì không chạy
        if (!isInitialized || targetFighter == null) return;

        // Tự động cập nhật thanh UI theo chỉ số thời gian thực của nhân vật
        if (healthSlider != null) healthSlider.value = targetFighter.stats.currentHp;
        if (manaSlider != null) manaSlider.value = targetFighter.stats.mana;
        if (staminaSlider != null) staminaSlider.value = targetFighter.stats.stamina;
    }
}