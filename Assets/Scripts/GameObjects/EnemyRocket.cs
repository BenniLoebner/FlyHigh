using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyRocket : MonoBehaviour
{
     [Header("Movement Settings")]
    [SerializeField] private float closingSpeed = 2f; // Speed at which to close in on target
    [SerializeField] private float accelerationSpeed = 20f; // How fast to reach target velocity (increased from 10)
    [SerializeField] private float rotationSpeed = 5f; // How fast it rotates to face target

    private Transform target; // Main rocket to chase
    private Rigidbody2D rb;
    private PlayerInputActions inputActions;
    private Camera mainCamera;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.FireFromCannon.performed += OnTap;
    }

    private void OnDisable()
    {
        inputActions.Player.FireFromCannon.performed -= OnTap;
        inputActions.Disable();
    }

    public void SetTarget(Transform mainRocket, float mainRocketSpeed, bool isFromLeft)
    {
        target = mainRocket;
        Debug.Log($"Enemy rocket tracking main rocket with closing speed: {closingSpeed}");
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            // Main rocket destroyed, destroy this enemy too
            Destroy(gameObject);
            return;
        }

        // Get main rocket's X velocity only
        float targetVelocityX = 0f;
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb != null)
            targetVelocityX = targetRb.linearVelocity.x;

        // Calculate direction to main rocket
        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        Vector2 directionToTarget = toTarget.normalized;

        // Calculate total speed needed: match rocket X speed + closing speed in direction of target
        float totalSpeed = Mathf.Sqrt(targetVelocityX * targetVelocityX + closingSpeed * closingSpeed);
        
        // Calculate velocity to reach target at that speed
        Vector2 desiredVelocity = directionToTarget * totalSpeed;
        
        // Blend in the main rocket's X velocity to keep up
        desiredVelocity.x = targetVelocityX + (directionToTarget.x * closingSpeed);
        desiredVelocity.y = directionToTarget.y * closingSpeed;

        // Set velocity directly (no lerp) for more reliable closing
        rb.linearVelocity = desiredVelocity;

        // Rotate to face target
        float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
    }

    private void OnTap(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // Check if player tapped on this enemy rocket
        Vector2 tapPosition = inputActions.Player.TouchPosition.ReadValue<Vector2>();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(tapPosition);
        worldPos.z = 0f;

        // Check if tap is within this rocket's collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && col.OverlapPoint(worldPos))
        {
            DestroyEnemy();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // If enemy hits the main rocket
        if (other.transform == target)
        {
            // Mark main rocket as hit — it will rise to peak then explode
            Rocket mainRocket = target.GetComponent<Rocket>();
            if (mainRocket != null)
            {
                mainRocket.HitByEnemy();
            }

            // Destroy this enemy
            Destroy(gameObject);
        }
    }

    private void DestroyEnemy()
    {
        Debug.Log("Enemy rocket destroyed by tap!");
        
        // Award points
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnEnemyRocketDestroyed();
        }

        Destroy(gameObject);
    }

    private void OnBecameInvisible()
    {
        // Destroy if it goes off screen
        Destroy(gameObject);
    }
}
