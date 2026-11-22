using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Kecepatan Gerak")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6.0f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;
    private bool isRunning;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(inputX, inputY).normalized;
        isRunning = Input.GetKey(KeyCode.LeftShift) && moveInput.sqrMagnitude > 0.01f;

        if (moveInput.sqrMagnitude > 0.01f)
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
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        rb.linearVelocity = moveInput * currentSpeed;
    }
}