using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class GhostAI : MonoBehaviour
{
    [Header("Kecepatan Gerak")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3.8f;

    [Header("Patroli (Titik Rute)")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waitTimeAtPoint = 1f;

    [Header("Stealth: Penglihatan & Pendengaran")]
    [SerializeField] private float viewDistance = 5.5f;
    [Range(30f, 180f)]
    [SerializeField] private float viewAngle = 95f;
    [SerializeField] private float hearingSensitivity = 1.0f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float searchDuration = 2f;
    [SerializeField] private bool defaultFacingRight = true;

    [Header("Audio (Opsional)")]
    [SerializeField] private AudioClip catchSound;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private AudioSource audioSource;
    private Transform playerTransform;
    private PlayerMovement playerMovement;

    private int currentWaypointIndex;
    private float waitTimer;
    private bool isChasing;
    private bool isInvestigating;
    private Vector2 lastKnownPosition;
    private float searchTimer;
    private Vector2 facingDirection = Vector2.down;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int IsChasingHash = Animator.StringToHash("IsChasing");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerMovement = playerObj.GetComponent<PlayerMovement>();
        }
    }

    private void Update()
    {
        if (playerTransform == null || playerMovement == null) return;

        bool spotted = CanSeePlayer() || CanHearPlayer();

        if (spotted)
        {
            isChasing = true;
            isInvestigating = false;
            lastKnownPosition = playerTransform.position;
            searchTimer = searchDuration;
        }
        else if (isChasing)
        {
            // Kehilangan kontak (masuk lemari / berbelok balik tembok) -> cek lokasi terakhir
            isChasing = false;
            isInvestigating = true;
        }

        animator.SetBool(IsChasingHash, isChasing);
    }

    private void FixedUpdate()
    {
        if (isChasing && playerTransform != null)
        {
            ChasePlayer();
        }
        else if (isInvestigating)
        {
            InvestigateLastPosition();
        }
        else
        {
            Patrol();
        }
    }

    private bool CanSeePlayer()
    {
        if (playerMovement.IsHiding) return false;

        Vector2 dirToPlayer = (Vector2)playerTransform.position - rb.position;
        float distance = dirToPlayer.magnitude;

        if (distance > viewDistance) return false;

        // Cek apakah pemain berada di dalam kerucut arah hadap hantu
        float angle = Vector2.Angle(facingDirection, dirToPlayer.normalized);
        if (angle > viewAngle * 0.5f) return false;

        // Cek apakah terhalang tembok / objek di obstacleLayer
        RaycastHit2D hit = Physics2D.Raycast(rb.position, dirToPlayer.normalized, distance, obstacleLayer);
        return hit.collider == null;
    }

    private bool CanHearPlayer()
    {
        if (playerMovement.IsHiding) return false;

        float effectiveHearingRange = playerMovement.CurrentNoiseRadius * hearingSensitivity;
        if (effectiveHearingRange <= 0.01f) return false;

        float distance = Vector2.Distance(rb.position, playerTransform.position);
        return distance <= effectiveHearingRange;
    }

    private void ChasePlayer()
    {
        Vector2 direction = ((Vector2)playerTransform.position - rb.position).normalized;
        rb.MovePosition(rb.position + direction * chaseSpeed * Time.fixedDeltaTime);
        UpdateAnimationDirection(direction);
    }

    private void InvestigateLastPosition()
    {
        Vector2 direction = lastKnownPosition - rb.position;

        if (direction.magnitude <= 0.2f)
        {
            searchTimer -= Time.fixedDeltaTime;
            if (searchTimer <= 0f)
                isInvestigating = false;
        }
        else
        {
            Vector2 moveDir = direction.normalized;
            rb.MovePosition(rb.position + moveDir * patrolSpeed * Time.fixedDeltaTime);
            UpdateAnimationDirection(moveDir);
        }
    }

    private void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform targetPoint = waypoints[currentWaypointIndex];
        if (targetPoint == null) return;

        Vector2 direction = (Vector2)targetPoint.position - rb.position;

        if (direction.magnitude <= 0.15f)
        {
            waitTimer += Time.fixedDeltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                waitTimer = 0f;
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }
        }
        else
        {
            Vector2 moveDir = direction.normalized;
            rb.MovePosition(rb.position + moveDir * patrolSpeed * Time.fixedDeltaTime);
            UpdateAnimationDirection(moveDir);
        }
    }

    private void UpdateAnimationDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.01f)
            facingDirection = dir.normalized;

        animator.SetFloat(MoveXHash, dir.x);
        animator.SetFloat(MoveYHash, dir.y);

        if (Mathf.Abs(dir.x) < 0.05f) return;

        if (defaultFacingRight)
            spriteRenderer.flipX = dir.x < 0;
        else
            spriteRenderer.flipX = dir.x > 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerMovement != null && playerMovement.IsHiding) return;

            if (audioSource != null && catchSound != null)
                audioSource.PlayOneShot(catchSound);

            Debug.Log("TERTANGKAP! Game Over.");
        }
    }

    // Visualisasi kerucut pandang hantu di jendela Scene
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 leftBoundary = Quaternion.Euler(0, 0, viewAngle * 0.5f) * facingDirection * viewDistance;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -viewAngle * 0.5f) * facingDirection * viewDistance;

        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        Gizmos.DrawWireSphere(transform.position, viewDistance);
    }
}