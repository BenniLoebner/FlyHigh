using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
public class PlayerController : MonoBehaviour
{
    [Header("Swipe Settings")]
    [SerializeField] private float minimumDistance = 50f; // in pixels
    [SerializeField] private float maximumTime = 1f;      // seconds
    [Range(0f, 1f)][SerializeField] private float directionThreshold = 0.95f;

    [Header("Force Fall Settings")]
    public float fallForce = -50f; // Fixed downward velocity

    [Header("Slow Fall Settings")]
    [SerializeField] private float fallSpeedY = -2f;
    [SerializeField] private float fallSpeedX = 10f;

    [Header("Speed Progression Settings")]
    [SerializeField] private float maxFallSpeedX = 20f;
    [SerializeField] private float speedAcceleration = 0.5f; // Einheiten/Sekunde²

    [Header("Wall Break Settings")]
    [SerializeField] private string breakableWallTag = "Wall";
    [SerializeField] private float orthogonalThreshold = 0.4f; // smaller = more strict orthogonal detection
    [SerializeField] private float trampolineWallBreakDuration = 1f; // Zeitfenster nach Trampolin-Kontakt, in dem Wände zerstört werden
    [SerializeField] private float wallSmashLookaheadPadding = 0.2f; // zusätzlicher Sicherheitsabstand für die Vorab-Erkennung

    [SerializeField] GameManager gameManager;

    private PlayerInputActions inputActions;
    private Rigidbody2D rb;
    private Collider2D playerCollider;

    private Vector2 startPos;
    private float startTime;
    private bool isTouching = false;
    private bool isForceFalling = false;
    private bool isStoppedAtWall = false;
    private bool fixedFallActive = false;
    private float currentFallSpeedX;
    private float trampolineWallBreakEndTime = -1f;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        transform.rotation = Quaternion.identity;
        currentFallSpeedX = fallSpeedX;
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.TouchPosition.performed += OnTouchMoved;
    }

    private void OnDisable()
    {
        inputActions.Player.TouchPosition.performed -= OnTouchMoved;
        inputActions.Disable();
    }

    private void Update()
    {
        CheckForSwipe();

        currentFallSpeedX = Mathf.Min(currentFallSpeedX + speedAcceleration * Time.deltaTime, maxFallSpeedX);

        // Nach einem Wandstopp bleibt die X-Geschwindigkeit gesperrt (gerader Fall), bis der Zustand endet
        float targetSpeedX = isStoppedAtWall ? 0f : currentFallSpeedX;
        rb.linearVelocity = new Vector2(targetSpeedX, rb.linearVelocity.y);

        if (rb.linearVelocity.y >= 15f)
        {
            isForceFalling = false;
            isStoppedAtWall = false;
        }
    }

    private void FixedUpdate()
    {
        if (!isForceFalling && !isStoppedAtWall)
            FallSlow();

        HandleWallSmashing();
    }

    // Erkennt Wände im Bewegungspfad und deaktiviert ihre Kollision, bevor die Physik-Simulation
    // diesen Schritt verarbeitet, damit Wände beim Force Fall / kurz nach Trampolin-Kontakt
    // keinerlei physikalischen Einfluss auf den Spieler haben.
    private void HandleWallSmashing()
    {
        bool canSmashWalls = isForceFalling || Time.time <= trampolineWallBreakEndTime;
        if (!canSmashWalls) return;

        float lookahead = rb.linearVelocity.magnitude * Time.fixedDeltaTime + wallSmashLookaheadPadding;
        float radius = playerCollider.bounds.extents.magnitude + lookahead;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag(breakableWallTag)) continue;

            // Kollision zwischen Spieler und Wand deaktivieren, bevor sie physikalisch aufeinandertreffen
            Physics2D.IgnoreCollision(playerCollider, hit, true);

            Debug.Log($"Wall {hit.gameObject.name} smashed without physical impact!");
            Destroy(hit.gameObject);

            if (ScoreManager.Instance != null)
                ScoreManager.Instance.OnWallBroken();
        }
    }

    private void CheckForSwipe()
    {
        isTouching = Touchscreen.current.primaryTouch.press.isPressed;

        if (isTouching && startPos == Vector2.zero)
        {
            startPos = inputActions.Player.TouchPosition.ReadValue<Vector2>();
            startTime = Time.time;
        }

        if (!isTouching && startPos != Vector2.zero)
            startPos = Vector2.zero;
    }

    private void OnTouchMoved(InputAction.CallbackContext ctx)
    {
        if (!isTouching) return;

        Vector2 currentPos = ctx.ReadValue<Vector2>();
        float distance = Vector2.Distance(startPos, currentPos);
        float duration = Time.time - startTime;

        if (distance >= minimumDistance && duration <= maximumTime)
        {
            Vector2 swipeDir = currentPos - startPos;
            DetectSwipe(swipeDir);
            startPos = Vector2.zero; // prevent multiple triggers
        }
    }

    private void DetectSwipe(Vector2 direction)
    {
        Vector2 screenDir = direction.normalized; // original swipe for ForceFall
        Vector2 worldStart = Camera.main.ScreenToWorldPoint(startPos);
        Vector2 worldEnd = Camera.main.ScreenToWorldPoint(startPos + direction);
        Vector2 worldDir = (worldEnd - worldStart).normalized; // for raycast
        float distance = (worldEnd - worldStart).magnitude;

        if (Vector2.Dot(Vector2.down, screenDir) > directionThreshold)
        ForceFall();

        RaycastHit2D hit = Physics2D.Raycast(worldStart, worldDir, distance);
        if (hit.collider != null && hit.collider.CompareTag(breakableWallTag))
        {
            Debug.Log("its happening");
            Vector2 wallNormal = hit.collider.transform.up; // wall's orientation
            float dot = Mathf.Abs(Vector2.Dot(worldDir, wallNormal));

            if (dot < orthogonalThreshold)
            {
                Debug.Log($"Wall {hit.collider.name} broken by swipe!");
                Destroy(hit.collider.gameObject);

                if (ScoreManager.Instance != null)
                    ScoreManager.Instance.OnWallBroken();
            }
            else
            {
                Debug.Log("Swipe not orthogonal enough to break wall.");
            }
        }
    }

    public void OnTrampolineBounce()
    {
        trampolineWallBreakEndTime = Time.time + trampolineWallBreakDuration;
        isStoppedAtWall = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag(breakableWallTag)) return;

        bool canBreakWall = isForceFalling || Time.time <= trampolineWallBreakEndTime;
        if (canBreakWall)
        {
            Debug.Log($"Wall {collision.gameObject.name} destroyed by collision!");
            Destroy(collision.gameObject);

            if (ScoreManager.Instance != null)
                ScoreManager.Instance.OnWallBroken();

            // Die Kollisionsauflösung hat die Geschwindigkeit bereits gebremst, bevor dieser Callback feuert.
            // Fall-Geschwindigkeit wiederherstellen, damit der Force Fall ungebremst weiterläuft.
            if (isForceFalling)
                rb.linearVelocity = new Vector2(currentFallSpeedX, fallForce);

            return;
        }

        // Normaler Modus: Wand stoppt den Spieler, danach fällt er durch echte Physik (Schwerkraft) nach unten.
        // Eigenes Flag statt isForceFalling, damit dieser Stopp keine weiteren Wände zerstörbar macht.
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 1f;
        isStoppedAtWall = true;
        fixedFallActive = false;
        gameManager.gameOver();
        Debug.Log($"Wall {collision.gameObject.name} stopped the player — falling via physics now.");
    }

    public void ForceFall()
    {
        if (isForceFalling) return;

        isForceFalling = true;
        isStoppedAtWall = false;
        rb.gravityScale = 1f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, fallForce);
        fixedFallActive = false; // reset slow fall
        Debug.Log($"[{Time.time:F2}] Force Fall triggered!");
    }

    private void FallSlow()
    {
        if (!fixedFallActive && rb.linearVelocity.y <= 0f)
        {
            fixedFallActive = true;
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fallSpeedY);
        }

        if (fixedFallActive)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fallSpeedY);
    }
    public void ResetToSlowFall()
    {
        isForceFalling = false;
        isStoppedAtWall = false;
        fixedFallActive = true;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(currentFallSpeedX, fallSpeedY);
    }
}
