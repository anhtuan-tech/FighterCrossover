using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the Support Summon behavior for Lucy.
/// Lucy xuất hiện trước mặt nhân vật, sau đó triệu hồi Loki lao vào đánh đối phương.
/// Animation Loki: loki_0→loki_1→loki_2 (lao tới), loki_4→loki_5 (rút lui rồi biến mất).
/// </summary>
public class SupportLucy : MonoBehaviour
{
    [Header("--- Visual References ---")]
    public GameObject lucyVisual;
    public GameObject lokiVisual;

    [Header("--- Loki Dash Frames (loki_0, loki_1, loki_2) ---")]
    public Sprite[] lokiDashFrames;

    [Header("--- Loki Exit Frames (loki_4, loki_5) ---")]
    public Sprite[] lokiExitFrames;

    private FighterBase owner;
    private int playerNumber;
    private LayerMask targetLayer;
    private float direction;

    private SpriteRenderer lucySR;
    private SpriteRenderer lokiSR;
    private Animator lucyAnim;
    private Animator lokiAnim;

    private bool lokiActive = false;
    private Vector3 lokiPosition;
    private List<IDamageable> hitTargets = new List<IDamageable>();

    [Header("--- Cài Đặt Loki ---")]
    [SerializeField] private float lokiDashSpeed   = 4f;
    [SerializeField] private float lokiDashDuration = 1.2f;
    [SerializeField] private float summonDelay      = 0.35f;
    [SerializeField] private float damage           = 35f;
    [SerializeField] private float exitFrameInterval = 0.15f; // giây mỗi frame exit
    [SerializeField] private float lucySpawnOffsetX = 1.2f;   // khoảng cách Lucy xuất hiện trước mặt nhân vật

    public void Setup(FighterBase owner, int playerNum, LayerMask targetLayer, float facingDirection)
    {
        this.owner       = owner;
        this.playerNumber = playerNum;
        this.targetLayer = targetLayer;
        this.direction   = facingDirection;

        // Cache components
        if (lucyVisual != null)
        {
            lucySR   = lucyVisual.GetComponent<SpriteRenderer>();
            lucyAnim = lucyVisual.GetComponent<Animator>();
        }
        if (lokiVisual != null)
        {
            lokiSR   = lokiVisual.GetComponent<SpriteRenderer>();
            lokiAnim = lokiVisual.GetComponent<Animator>();
        }

        StartCoroutine(SummonRoutine());
    }

    private IEnumerator SummonRoutine()
    {
        // 1. Lucy xuất hiện TRƯỚC MẶT nhân vật (direction dương = phía trước)
        Vector3 spawnPos = owner.transform.position + new Vector3(direction * lucySpawnOffsetX, 0f, 0f);
        transform.position = spawnPos;

        // Hiện Lucy, quay đúng hướng
        if (lucyVisual != null)
        {
            lucyVisual.transform.localScale = new Vector3(direction, 1f, 1f);
            lucyVisual.SetActive(true);
            if (lucyAnim != null) lucyAnim.Play("Lucy_Summon");
        }

        // Ẩn Loki trước
        if (lokiVisual != null) lokiVisual.SetActive(false);

        yield return new WaitForSeconds(summonDelay);

        // 2. Loki xuất hiện phía trước Lucy, chơi frames loki_0 → loki_1 → loki_2 trong khi lao tới
        Vector3 lokiSpawnPos = transform.position + new Vector3(direction * 1.0f, 0f, 0f);
        if (lokiVisual != null)
        {
            lokiVisual.transform.position = lokiSpawnPos;
            lokiVisual.transform.localScale = new Vector3(direction, 1f, 1f);
            lokiVisual.SetActive(true);
            // Tắt Animator để tự điều khiển sprite frame
            if (lokiAnim != null) lokiAnim.enabled = false;
        }

        lokiPosition = lokiSpawnPos;
        lokiActive   = true;

        // Tính thời gian mỗi frame để animation trải đều trong suốt thời gian lao tới
        float dashFrameInterval = (lokiDashFrames != null && lokiDashFrames.Length > 0)
            ? lokiDashDuration / lokiDashFrames.Length
            : lokiDashDuration;

        // Chơi frames 0→1→2 1 lần duy nhất, song song với di chuyển
        StartCoroutine(PlayFrameOnce(lokiSR, lokiDashFrames, dashFrameInterval));

        // --- Loki lao tới ---
        float elapsed = 0f;
        while (elapsed < lokiDashDuration)
        {
            elapsed += Time.deltaTime;

            // Di chuyển Loki về phía trước
            lokiPosition += new Vector3(direction * lokiDashSpeed * Time.deltaTime, 0f, 0f);
            if (lokiVisual != null)
                lokiVisual.transform.position = lokiPosition;

            // Phát hiện va chạm với đối thủ
            Collider2D[] hits = Physics2D.OverlapBoxAll(
                lokiPosition + new Vector3(0f, 0.5f, 0f),
                new Vector2(1.2f, 1.8f), 0f, targetLayer);

            foreach (var hit in hits)
            {
                if (hit.gameObject == owner.gameObject) continue;
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null && !hitTargets.Contains(damageable))
                {
                    hitTargets.Add(damageable);
                    damageable.TakeDamage(damage, lokiPosition.x, true);
                    Debug.Log($"[SupportLucy] Loki hit {hit.gameObject.name} dealing {damage} damage!");
                }
            }

            yield return null;
        }

        lokiActive = false;

        // 3. Chơi frames exit (loki_4, loki_5) sau khi đánh xong
        if (lokiExitFrames != null && lokiExitFrames.Length > 0)
        {
            yield return StartCoroutine(PlayFrameOnce(lokiSR, lokiExitFrames, exitFrameInterval));
        }

        // 4. Fade out Lucy và Loki cùng lúc
        float fadeDuration = 0.25f;
        float fadeElapsed  = 0f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDuration);
            if (lucySR != null) lucySR.color = new Color(1f, 1f, 1f, alpha);
            if (lokiSR != null) lokiSR.color  = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        // 5. Destroy toàn bộ
        Destroy(gameObject);
    }

    /// <summary>Chơi frames 1 lần rồi dừng.</summary>
    private IEnumerator PlayFrameOnce(SpriteRenderer sr, Sprite[] frames, float interval)
    {
        if (sr == null || frames == null || frames.Length == 0) yield break;
        foreach (var frame in frames)
        {
            if (frame != null) sr.sprite = frame;
            yield return new WaitForSeconds(interval);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (lokiActive)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(lokiPosition + new Vector3(0f, 0.5f, 0f), new Vector2(1.2f, 1.8f));
        }
    }
}
