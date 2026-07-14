using System.Collections;
using UnityEngine;

/// <summary>
/// Controls the Support Summon behavior for Giorno Giovanna.
/// Giorno xuất hiện trước mặt nhân vật chính, phát hết hoạt ảnh hỗ trợ (Giorno_Support),
/// sau đó hồi lại 15% lượng máu tối đa cho nhân vật chính, rồi fade out và biến mất.
/// </summary>
public class GiornoGiovannaController : MonoBehaviour
{
    [Header("--- Visual References ---")]
    public GameObject giornoVisual;

    [Header("--- Cài Đặt Giorno ---")]
    [SerializeField] private float healPercentage   = 0.15f;  // hồi 15% máu tối đa
    [SerializeField] private float activeDuration   = 0.75f;  // thời gian diễn hoạt ảnh (9 frames ở 12 FPS)
    [SerializeField] private float fadeDuration     = 0.3f;   // thời gian fade out
    [SerializeField] private float spawnOffsetX     = 1.0f;   // khoảng cách xuất hiện trước mặt chủ thể

    private FighterBase owner;
    private float direction;
    private SpriteRenderer giornoSR;
    private Animator giornoAnim;

    public void Setup(FighterBase owner, int playerNum, LayerMask targetLayer, float facingDirection)
    {
        this.owner     = owner;
        this.direction = facingDirection;

        // Cache components
        if (giornoVisual != null)
        {
            giornoSR   = giornoVisual.GetComponent<SpriteRenderer>();
            giornoAnim = giornoVisual.GetComponent<Animator>();
        }

        StartCoroutine(SummonRoutine());
    }

    private IEnumerator SummonRoutine()
    {
        // 1. Đặt vị trí xuất hiện trước mặt chủ thể
        Vector3 spawnPos = owner.transform.position + new Vector3(direction * spawnOffsetX, 0.1f, 0f);
        transform.position = spawnPos;

        // 2. Quay hướng, phát hoạt ảnh
        if (giornoVisual != null)
        {
            giornoVisual.transform.localScale = new Vector3(direction, 1f, 1f);
            giornoVisual.SetActive(true);

            if (giornoAnim != null)
                giornoAnim.Play("Giorno_Support");
        }

        // 3. Đợi cho hoạt ảnh kết thúc
        yield return new WaitForSeconds(activeDuration);

        // 4. Thực hiện hồi máu 15% cho nhân vật chính (owner)
        if (owner != null && owner.stats.maxHp > 0f)
        {
            float healAmount = owner.stats.maxHp * healPercentage;
            owner.stats.currentHp += healAmount;
            
            // Giới hạn không vượt quá máu tối đa
            if (owner.stats.currentHp > owner.stats.maxHp)
            {
                owner.stats.currentHp = owner.stats.maxHp;
            }
            
            Debug.Log($"[Support Giorno] Healed {owner.gameObject.name} for {healAmount} HP (15% of max HP). Current HP: {owner.stats.currentHp}");
        }

        // 5. Fade out Giorno
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDuration);
            if (giornoSR != null)
                giornoSR.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        // 6. Tự hủy
        Destroy(gameObject);
    }
}
