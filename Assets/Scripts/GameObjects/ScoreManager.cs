using UnityEngine;
using TMPro; // Using TextMeshPro for better text rendering
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance; // Singleton pattern for easy access

    [Header("Score Settings")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private int currentScore = 0;
    
    [Header("Score Values")]
    [SerializeField] private float incrementScoreSpeed = .001f; // How often to increment score
    [SerializeField] private int wallBreakPoints = 500; // Points for breaking a wall (player or rocket)
    [SerializeField] private int trampolineBouncePoints = 100; // Points for bouncing on trampoline
    [SerializeField] private int enemyRocketPoints = 300; // Points for destroying enemy rocket
    [SerializeField] private float rocketScoreMultiplier = 3f; // Score multiplier when on rocket

    [Header("Visual Effects")]
    [SerializeField] private float scaleMultiplier = 0.002f; // How much to scale per point (e.g., 100 points = 0.2 scale increase)
    [SerializeField] private float maxScale = 2f; // Maximum scale
    [SerializeField] private float scaleSpeed = 5f; // How fast to return to normal size

    private float timer = 0f;
    private bool isPlaying = false;
    private float currentMultiplier = 1f; // Current score multiplier
    private Vector3 originalScale; // Store original text scale
    private float targetScale = 1f; // Target scale for animation
    private Coroutine scaleCoroutine; // Track active scale animation

    private void Awake()
    {
        // Singleton pattern - only one ScoreManager exists
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (scoreText != null)
        {
            // Automatically anchor text to top-left
            RectTransform rectTransform = scoreText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Set anchor to top-left corner of canvas
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(0, 1);
                
                // Set pivot to top-left (so it scales from this point)
                rectTransform.pivot = new Vector2(0, 1);
                
                // Set position with padding (adjust these values as needed)
                rectTransform.anchoredPosition = new Vector2(20, -20);
            }
            
            originalScale = scoreText.transform.localScale;
        }
        
        UpdateScoreUI();
    }

    private void Update()
    {
        // Award points over time while playing
        if (isPlaying)
        {
            timer += Time.deltaTime;
            if (timer >= incrementScoreSpeed)
            {
                // Add score with current multiplier (but don't animate continuous scoring)
                int pointsToAdd = Mathf.RoundToInt(1 * currentMultiplier);
                currentScore += pointsToAdd;
                UpdateScoreUI();
                timer = 0f;
            }
        }
    }

    public void StartScoring()
    {
        isPlaying = true;
    }

    public void StopScoring()
    {
        isPlaying = false;
    }

    public void AddScore(int points)
    {
        currentScore += points;
        UpdateScoreUI();
        
        // Trigger animation for bonus points
        AnimateScoreText(points);
    }

    public void ResetScore()
    {
        currentScore = 0;
        timer = 0f;
        currentMultiplier = 1f;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{currentScore}";
        }
    }

    public int GetScore()
    {
        return currentScore;
    }

    // Call this when player breaks a wall
    public void OnWallBroken()
    {
        AddScore(wallBreakPoints);
        Debug.Log($"Wall broken! +{wallBreakPoints} points");
    }

    // Call this when player bounces on trampoline
    public void OnTrampolineBounce()
    {
        AddScore(trampolineBouncePoints);
        Debug.Log($"Trampoline bounce! +{trampolineBouncePoints} points");
    }

    // Call this when player destroys enemy rocket
    public void OnEnemyRocketDestroyed()
    {
        AddScore(enemyRocketPoints);
        Debug.Log($"Enemy rocket destroyed! +{enemyRocketPoints} points");
    }

    // Call this when player gets on rocket - activates multiplier
    public void OnRocketRideStart()
    {
        currentMultiplier = rocketScoreMultiplier;
        Debug.Log($"Rocket ride started! Score multiplier: x{currentMultiplier}");
    }

    // Call this when player gets off rocket - deactivates multiplier
    public void OnRocketRideEnd()
    {
        currentMultiplier = 1f;
        Debug.Log("Rocket ride ended! Score multiplier reset to x1");
    }

    private void AnimateScoreText(int points)
    {
        if (scoreText == null) return;

        // Calculate scale based on points gained
        float scaleIncrease = points * scaleMultiplier;
        float newTargetScale = Mathf.Min(1f + scaleIncrease, maxScale);

        // If already animating, add to the target scale instead of replacing
        if (scaleCoroutine != null)
        {
            targetScale = Mathf.Min(targetScale + scaleIncrease, maxScale);
        }
        else
        {
            targetScale = newTargetScale;
        }

        // Stop any existing animation
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        // Start new animation
        scaleCoroutine = StartCoroutine(ScaleTextAnimation());
    }

    private IEnumerator ScaleTextAnimation()
    {
        if (scoreText == null) yield break;

        // Scale up quickly to target
        float elapsedTime = 0f;
        Vector3 startScale = scoreText.transform.localScale;
        Vector3 targetScaleVector = originalScale * targetScale;

        // Quick scale up (0.1 seconds)
        while (elapsedTime < 0.1f)
        {
            elapsedTime += Time.deltaTime;
            scoreText.transform.localScale = Vector3.Lerp(startScale, targetScaleVector, elapsedTime / 0.1f);
            yield return null;
        }

        scoreText.transform.localScale = targetScaleVector;

        // Hold briefly
        yield return new WaitForSeconds(0.15f);

        // Scale back down smoothly
        while (scoreText.transform.localScale.x > originalScale.x + 0.01f)
        {
            scoreText.transform.localScale = Vector3.Lerp(
                scoreText.transform.localScale,
                originalScale,
                Time.deltaTime * scaleSpeed
            );
            yield return null;
        }

        scoreText.transform.localScale = originalScale;
        targetScale = 1f;
        scaleCoroutine = null;
    }
}
