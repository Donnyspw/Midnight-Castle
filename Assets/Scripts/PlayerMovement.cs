using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Kecepatan Gerak")]
    [SerializeField] private float sneakSpeed = 1.8f;
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6.0f;

    [Header("Kontrol & Radius Kebisingan (Stealth)")]
    [SerializeField] private KeyCode sneakKey = KeyCode.LeftControl;
    [SerializeField] private float sneakNoiseRadius = 0.4f;
    [SerializeField] private float walkNoiseRadius = 1.8f;
    [SerializeField] private float runNoiseRadius = 5.0f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;
    private bool isSneaking;
    private bool isRunning;
    private bool isHiding;

    public bool IsHiding => isHiding;
    public bool IsSneaking => isSneaking;
    public float CurrentNoiseRadius { get; private set; }

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsHidingHash = Animator.StringToHash("IsHiding");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (isHiding)
        {
            moveInput = Vector2.zero;
            isSneaking = false;
            isRunning = false;
            CurrentNoiseRadius = 0f;
            return;
        }

        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(inputX, inputY).normalized;
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        // Mengendap (Left Ctrl) otomatis memakai animasi Walk biasa karena IsRunning = false
        isSneaking = isMoving && Input.GetKey(sneakKey);
        isRunning = isMoving && !isSneaking && Input.GetKey(KeyCode.LeftShift);

        if (!isMoving)
            CurrentNoiseRadius = 0f;
        else if (isSneaking)
            CurrentNoiseRadius = sneakNoiseRadius;
        else if (isRunning)
            CurrentNoiseRadius = runNoiseRadius;
        else
            CurrentNoiseRadius = walkNoiseRadius;

        if (isMoving)
        {
            lastMoveDirection = moveInput;

            // Gambar walk & run bawaan menghadap kiri: kiri = false, kanan = true
            if (inputX < -0.01f)
                spriteRenderer.flipX = false;
            else if (inputX > 0.01f)
                spriteRenderer.flipX = true;
        }

        animator.SetFloat(MoveXHash, lastMoveDirection.x);
        animator.SetFloat(MoveYHash, lastMoveDirection.y);
        animator.SetFloat(SpeedHash, moveInput.sqrMagnitude);
        animator.SetBool(IsRunningHash, isRunning);
    }

    private void FixedUpdate()
    {
        if (isHiding)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float currentSpeed = walkSpeed;
        if (isSneaking) currentSpeed = sneakSpeed;
        else if (isRunning) currentSpeed = runSpeed;

        rb.linearVelocity = moveInput * currentSpeed;
    }

    public void SetHiding(bool hiding)
    {
        isHiding = hiding;
        isSneaking = false;
        isRunning = false;
        CurrentNoiseRadius = 0f;
        rb.linearVelocity = Vector2.zero;
        animator.SetFloat(SpeedHash, 0f);
        animator.SetBool(IsRunningHash, false);
        animator.SetBool(IsHidingHash, isHiding);
    }

    // Visualisasi lingkaran suara pemain di jendela Scene (lingkaran kuning)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, CurrentNoiseRadius);
    }
}