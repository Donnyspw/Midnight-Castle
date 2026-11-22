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

    [Header("Deteksi & Penangkapan")]
    [SerializeField] private float detectionRadius = 4.5f;
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

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        isChasing = !playerMovement.IsHiding && distanceToPlayer <= detectionRadius;

        // Kirim status kejar ke Animator (Walk_Tree <-> Run_Tree)
        animator.SetBool(IsChasingHash, isChasing);
    }

    private void FixedUpdate()
    {
        if (isChasing && playerTransform != null)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    private void ChasePlayer()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        rb.MovePosition(rb.position + direction * chaseSpeed * Time.fixedDeltaTime);
        UpdateAnimationDirection(direction);
    }

    private void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform targetPoint = waypoints[currentWaypointIndex];
        if (targetPoint == null) return;

        Vector2 direction = ((Vector2)targetPoint.position - rb.position);

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
        // Kirim arah gerak ke Blend Tree (Up, Down, Left, Right)
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}