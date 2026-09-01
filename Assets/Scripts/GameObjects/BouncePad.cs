using UnityEngine;
using System.Collections;

public class BouncePad : MonoBehaviour
{
    /*[Header("Bounce Settings")]
    [SerializeField] private float maxBounceForce = 20f;   // At exact center
    [SerializeField] private float minBounceForce = 5f;    // At trampoline edges
    [SerializeField] private float fallVelocityEnhancer;

    private Collider2D trampolineCollider;
    [SerializeField] PlayerController playerController;

    private void Awake()
    {
        trampolineCollider = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
            PlayerController player = collision.collider.GetComponent<PlayerController>();

            // Check relative velocity first
            float impactSpeed = Mathf.Abs(collision.relativeVelocity.y);

            // Fallback if relative velocity is tiny
            if (impactSpeed < 0.01f)
            {
                impactSpeed = Mathf.Abs(rb.linearVelocity.y);
            }
            Debug.Log(impactSpeed);



            if (rb == null) return;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);

            // Find the hit point relative to trampoline center
            Vector2 contactPoint = collision.GetContact(0).point;
            float trampolineCenterX = trampolineCollider.bounds.center.x;
            float trampolineHalfWidth = trampolineCollider.bounds.extents.x;

            // Distance from center (0 = exact middle, 1 = edge)
            float normalizedDistance = Mathf.Abs(contactPoint.x - trampolineCenterX) / trampolineHalfWidth;

            // Invert distance so center = 1, edge = 0
            float strengthFactor = 1f - Mathf.Clamp01(normalizedDistance);

            // Calculate bounce force
            float bounceForce = Mathf.Lerp(minBounceForce, maxBounceForce, strengthFactor);

            fallVelocityEnhancer = impactSpeed + playerController.fallForce;

            // Apply upward velocity
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce + fallVelocityEnhancer);

            Debug.Log($"Trampoline bounce! Distance: {normalizedDistance}, Force: {bounceForce}");
        }
    }*/

    [Header("Bounce Settings")]
    [SerializeField] private float maxBounceForce = 20f;   // At exact center
    [SerializeField] private float minBounceForce = 5f;    // At trampoline edges
    [SerializeField] private float fallVelocityEnhancer;

    private Collider2D trampolineCollider;
    [SerializeField] PlayerController playerController;

    private void Awake()
    {
        trampolineCollider = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
            PlayerController player = collision.collider.GetComponent<PlayerController>();

            // Check relative velocity first
            float impactSpeed = Mathf.Abs(collision.relativeVelocity.y);

            // Fallback if relative velocity is tiny
            if (impactSpeed < 0.01f)
            {
                impactSpeed = Mathf.Abs(rb.linearVelocity.y);
            }
            Debug.Log(impactSpeed);



            if (rb == null) return;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);

            // Find the hit point relative to trampoline center
            Vector2 contactPoint = collision.GetContact(0).point;
            float trampolineCenterX = trampolineCollider.bounds.center.x;
            float trampolineHalfWidth = trampolineCollider.bounds.extents.x;

            // Distance from center (0 = exact middle, 1 = edge)
            float normalizedDistance = Mathf.Abs(contactPoint.x - trampolineCenterX) / trampolineHalfWidth;

            // Invert distance so center = 1, edge = 0
            float strengthFactor = 1f - Mathf.Clamp01(normalizedDistance);

            // Calculate bounce force
            float bounceForce = Mathf.Lerp(minBounceForce, maxBounceForce, strengthFactor);

            fallVelocityEnhancer = impactSpeed + playerController.fallForce;

            // Apply upward velocity
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce + fallVelocityEnhancer);

            // Award points for bouncing
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.OnTrampolineBounce();

            if (player != null)
                player.OnTrampolineBounce();

            Debug.Log($"Trampoline bounce! Distance: {normalizedDistance}, Force: {bounceForce}");
        }
    }
}
