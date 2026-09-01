using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    [Header("Follow Player Settings")]
    public Transform target;
    [SerializeField] private float baseFollowSpeed = 20f; // Horizontal follow speed
    [SerializeField] private float rocketCenterSpeed = 5f; // Speed to center on rocket (smooth transition)
    [SerializeField] private float xOffset = 0f;         // Horizontal offset
    [SerializeField] private float zOffset = -10f;       // Camera Z position

    [Header("Vertical Zoom Settings")]
    [SerializeField] private float baseSize = 12f;       // Original camera size
    [SerializeField] private float buffer = 2f;          // Extra space above player
    [SerializeField] private float zoomOutSpeed = 5f;       // Smooth zoom speed
    [SerializeField] private float zoomInSpeed = 5f;

    private float lastX;         // Track furthest-right position
    private Camera cam;
    private bool isExpanded = false;
    private bool wasOnRocket = false; // Track if player was on rocket

    private void Start()
    {
        SetStartPosition();
        SetBaseSize();
    }

    private void LateUpdate()
    {
        FollowPlayer();
    }
    private void SetStartPosition()
    {
        if (target != null)
            lastX = transform.position.x;
    }

    private void SetBaseSize()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
            cam.orthographicSize = baseSize;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
            lastX = transform.position.x;
    }

    private void FollowPlayer()
    {
         if (target == null) return;

        // -------------------------
        // Horizontal Follow
        // -------------------------
        // Check if player is riding rocket (player is parented to rocket)
        bool isOnRocket = (target.parent != null && target.parent.GetComponent<Rocket>() != null);
        
        Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
        float targetX;
        
        if (isOnRocket)
        {
            // Center camera on the ROCKET's X position (not player's local offset)
            Transform rocket = target.parent;
            targetX = rocket.position.x;
            Vector3 desiredPos = new Vector3(targetX, transform.position.y, zOffset);
            
            // Use MoveTowards for exact centering
            float moveStep = rocketCenterSpeed * 10f * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, desiredPos, moveStep);
            
            wasOnRocket = true;
        }
        else
        {
            // If just got off rocket, transition back smoothly
            if (wasOnRocket)
            {
                // Calculate where camera should be for normal left-side view
                // Camera should be ahead of player so player appears on left
                targetX = target.position.x + cam.orthographicSize * cam.aspect * 0.6f; // Player on left third
                targetX = Mathf.Max(targetX, lastX); // Still respect right-only rule
                
                Vector3 desiredPos = new Vector3(targetX, transform.position.y, zOffset);
                
                // Smoothly transition back
                float moveStep = rocketCenterSpeed * 10f * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, desiredPos, moveStep);
                
                // Once close enough, switch back to normal mode
                if (Mathf.Abs(transform.position.x - targetX) < 0.5f)
                {
                    wasOnRocket = false;
                    lastX = targetX;
                }
            }
            else
            {
                // Normal right-only following
                targetX = Mathf.Max(target.position.x + xOffset, lastX);
                Vector3 desiredPos = new Vector3(targetX, transform.position.y, zOffset);
                
                // Normal following with MoveTowards
                float playerSpeed = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;
                float moveStep = Mathf.Max(baseFollowSpeed, playerSpeed) * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, desiredPos, moveStep);
                
                lastX = targetX;
            }
        }

        // -------------------------
        // Vertical Dynamic Zoom
        // -------------------------
        if (cam != null && rb != null)
        {
            float playerTop = target.position.y + buffer;
            float bottom = -baseSize; // Fixed bottom of camera

            if (playerTop > baseSize && rb.linearVelocity.y > 0) // Player rising above top
            {
                // Calculate new size and center
                float newSize = (playerTop - bottom) / 2f;
                float newCenterY = bottom + newSize;

                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, newSize, zoomOutSpeed * Time.deltaTime);
                transform.position = new Vector3(transform.position.x, newCenterY, transform.position.z);

                isExpanded = true;
            }
            else if (isExpanded && playerTop <= baseSize) // Shrink back when player falls low enough
            {
                // Smoothly shrink size
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, baseSize, zoomInSpeed * Time.deltaTime);

                // Smoothly move Y position back to base center (0)
                float smoothedY = Mathf.Lerp(transform.position.y, 0f, zoomInSpeed * Time.deltaTime);
                transform.position = new Vector3(transform.position.x, smoothedY, transform.position.z);

                if (Mathf.Abs(cam.orthographicSize - baseSize) < 0.01f &&
                    Mathf.Abs(transform.position.y - 0f) < 0.01f)
                {
                    cam.orthographicSize = baseSize;
                    transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
                    isExpanded = false;
                }
            }
        }
    }
}
