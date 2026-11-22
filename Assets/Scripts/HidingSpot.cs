using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class HidingSpot : MonoBehaviour
{
    [Header("Interaksi & Pengaturan Stage")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [Tooltip("Centang jika lemari ini terkunci dan tidak bisa dipakai sembunyi")]
    [SerializeField] private bool isLocked = false;
    [Tooltip("Centang jika di stage ini pintu lemari terbuka dari awal")]
    [SerializeField] private bool startsOpen = false;
    [SerializeField] private float doorTransitionDuration = 0.15f;

    [Header("3 Sprite Lemari")]
    [SerializeField] private Sprite closedSprite;  // lemari_tertutup
    [SerializeField] private Sprite openSprite;    // lemari_terbuka
    [SerializeField] private Sprite hidingSprite;  // lemari_berisi (mata mengintip)

    [Header("Audio (Opsional)")]
    [SerializeField] private AudioClip enterSound;
    [SerializeField] private AudioClip exitSound;
    [SerializeField] private AudioClip lockedSound;

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private PlayerMovement playerInRange;
    private SpriteRenderer playerSpriteRenderer;
    private Vector3 positionBeforeHiding;
    private Vector3 initialLocalPosition;
    private bool isTransitioning;

    public bool IsLocked => isLocked;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        initialLocalPosition = transform.localPosition;
        ResetToDefaultSprite();
    }

    private void Update()
    {
        if (playerInRange == null || isTransitioning) return;

        if (Input.GetKeyDown(interactKey))
        {
            // Jika pemain belum bersembunyi dan lemari terkunci
            if (!playerInRange.IsHiding && isLocked)
            {
                StartCoroutine(LockedFeedbackRoutine());
                return;
            }

            if (!playerInRange.IsHiding)
                StartCoroutine(EnterWardrobeRoutine());
            else
                StartCoroutine(ExitWardrobeRoutine());
        }
    }

    private IEnumerator LockedFeedbackRoutine()
    {
        isTransitioning = true;

        if (audioSource != null && lockedSound != null)
            audioSource.PlayOneShot(lockedSound);

        // Efek getar singkat pada lemari sebagai penanda visual pintu terkunci
        float elapsed = 0f;
        float duration = 0.18f;
        float magnitude = 0.05f;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            transform.localPosition = initialLocalPosition + new Vector3(offsetX, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = initialLocalPosition;
        isTransitioning = false;
    }

    private IEnumerator EnterWardrobeRoutine()
    {
        isTransitioning = true;

        positionBeforeHiding = playerInRange.transform.position;
        playerInRange.transform.position = transform.position;
        playerInRange.SetHiding(true);

        if (playerSpriteRenderer != null)
            playerSpriteRenderer.enabled = false;

        if (audioSource != null && enterSound != null)
            audioSource.PlayOneShot(enterSound);

        if (openSprite != null)
            spriteRenderer.sprite = openSprite;

        yield return new WaitForSeconds(doorTransitionDuration);

        if (hidingSprite != null)
            spriteRenderer.sprite = hidingSprite;

        isTransitioning = false;
    }

    private IEnumerator ExitWardrobeRoutine()
    {
        isTransitioning = true;

        if (audioSource != null && exitSound != null)
            audioSource.PlayOneShot(exitSound);

        if (openSprite != null)
            spriteRenderer.sprite = openSprite;

        yield return new WaitForSeconds(doorTransitionDuration);

        playerInRange.SetHiding(false);
        playerInRange.transform.position = positionBeforeHiding;

        if (playerSpriteRenderer != null)
            playerSpriteRenderer.enabled = true;

        ResetToDefaultSprite();
        isTransitioning = false;
    }

    private void ResetToDefaultSprite()
    {
        // Lemari yang terkunci selalu dalam kondisi tertutup
        if (!isLocked && startsOpen && openSprite != null)
            spriteRenderer.sprite = openSprite;
        else if (closedSprite != null)
            spriteRenderer.sprite = closedSprite;
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        ResetToDefaultSprite();
    }

    public void Unlock()
    {
        SetLocked(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = other.GetComponent<PlayerMovement>();
            playerSpriteRenderer = other.GetComponent<SpriteRenderer>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement exitingPlayer = other.GetComponent<PlayerMovement>();
            if (exitingPlayer != null && !exitingPlayer.IsHiding && exitingPlayer == playerInRange)
            {
                playerInRange = null;
                playerSpriteRenderer = null;
            }
        }
    }
}