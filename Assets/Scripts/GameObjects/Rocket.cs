using UnityEngine;

public class Rocket : MonoBehaviour
{
    
    [Header("Flight Settings")]
    [SerializeField] private float speed = 12f; // horizontal speed (faster than player)

    [Header("Sinus Flight Settings")]
    [SerializeField] private float waveAmplitude = 2f;   // how high up/down
    [SerializeField] private float waveFrequency = 2f;   // how fast it waves
    [SerializeField] private float centerTransitionSpeed = 1f; // how fast to transition to Y=0

    [Header("Hit Rise Settings")]
    [SerializeField] private float hitRiseSpeed = 5f; // how fast the rocket rises after being hit

    [Header("Enemy Rocket Settings")]
    [SerializeField] private GameObject enemyRocketPrefab;
    [SerializeField] private float enemySpawnDelay = 5f; // wait before first enemy spawns
    [SerializeField] private float maxSpawnInterval = 4f; // slow start (seconds between spawns)
    [SerializeField] private float minSpawnInterval = 0.5f; // fastest spawn rate
    [SerializeField] private float spawnRampUpTime = 30f; // seconds to go from slowest to fastest

    private PlayerController playerController;
    private Rigidbody2D rb;
    private bool hasPlayer = false;
    private float sineTime = 0f;
    private float centerY = 0f; // vertical center for wave motion
    private float enemySpawnTimer = 0f;
    private float enemyDelayTimer = 0f;
    private bool enemySpawningActive = false;
    private bool isHit = false;
    private float hitTargetY = 0f;

    private Transform player; // attached player reference

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // Spawn enemy rockets if player is attached (but not if already hit)
        if (hasPlayer && !isHit)
        {
            if (!enemySpawningActive)
            {
                enemyDelayTimer += Time.fixedDeltaTime;
                if (enemyDelayTimer >= enemySpawnDelay)
                    enemySpawningActive = true;
            }
            else
            {
                enemySpawnTimer += Time.fixedDeltaTime;
                float timeSinceActive = enemyDelayTimer - enemySpawnDelay;
                float t = Mathf.Clamp01(timeSinceActive / spawnRampUpTime);
                float currentInterval = Mathf.Lerp(maxSpawnInterval, minSpawnInterval, t);
                if (enemySpawnTimer >= currentInterval)
                {
                    SpawnEnemyRocket();
                    enemySpawnTimer = 0f;
                }
                enemyDelayTimer += Time.fixedDeltaTime;
            }
        }

        if (!hasPlayer)
        {
            // Normal state → straight horizontal flight
            rb.linearVelocity = new Vector2(speed, 0f);
        }
        else if (isHit)
        {
            // Hit by enemy → gradually rise to peak height, then explode
            float deltaY = hitTargetY - transform.position.y;

            if (deltaY < 0.1f)
            {
                DestroyRocket();
                return;
            }

            rb.linearVelocity = new Vector2(speed, hitRiseSpeed);

            // Keep player moving with rocket
            if (player != null)
            {
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                    playerRb.linearVelocity = rb.linearVelocity;
            }
        }
        else
        {
            // Smoothly transition centerY towards 0
            centerY = Mathf.Lerp(centerY, 0f, centerTransitionSpeed * Time.fixedDeltaTime);

            // Boosted state → sinusoidal flight around centerY
            sineTime += Time.fixedDeltaTime * waveFrequency;

            float currentSineValue = Mathf.Sin(sineTime);
            float targetY = centerY + currentSineValue * waveAmplitude;
            float deltaY = targetY - transform.position.y;

            // Y velocity to reach target smoothly
            rb.linearVelocity = new Vector2(speed, deltaY / Time.fixedDeltaTime);

            // Keep player moving with rocket
            if (player != null)
            {
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                    playerRb.linearVelocity = rb.linearVelocity;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            Debug.Log($"Rocket hit wall! hasPlayer: {hasPlayer}");
            Destroy(other.gameObject);
            
            // Award points if player is on the rocket
            if (hasPlayer && ScoreManager.Instance != null)
            {
                Debug.Log("Awarding points for rocket wall destruction!");
                ScoreManager.Instance.OnWallBroken();
            }
        }

        if (other.CompareTag("Player") && !hasPlayer)
            AttachPlayer(other.transform);
    }

    private void AttachPlayer(Transform playerTransform)
    {
        hasPlayer = true;
        player = playerTransform;

        playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
            playerController.enabled = false;

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Parent the player to the rocket so camera follows naturally
        player.SetParent(transform);
        player.localPosition = Vector3.up * 0.5f;

        // Start wave motion from current position, then smoothly transition to Y = 0
        centerY = transform.position.y;
        sineTime = 0f; // Reset sine wave phase

        // Start score multiplier for riding rocket
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnRocketRideStart();

        Debug.Log("Player attached to rocket!");
    }

    public void DetachPlayer()
    {
        if (player == null) return;

        player.SetParent(null);

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.bodyType = RigidbodyType2D.Dynamic;
        }

        if (playerController != null)
        {
            playerController.enabled = true;
            playerController.ResetToSlowFall(); // Reset to steady slow fall
        }

        // End score multiplier
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnRocketRideEnd();

        player = null;
        hasPlayer = false;
        sineTime = 0f;

        Debug.Log("Player detached from rocket!");
    }

    private void SpawnEnemyRocket()
    {
        if (enemyRocketPrefab == null) return;

        // Get camera position (center of screen)
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 cameraPos = cam.transform.position;

        // Randomly choose left or right side
        bool spawnOnLeft = Random.value > 0.5f;
        
        // Calculate spawn position at screen edges (-25 or +25 from camera center)
        Vector3 spawnPos;
        spawnPos.x = cameraPos.x + (spawnOnLeft ? -25f : 25f);
        spawnPos.y = Random.Range(-20f, 20f); // Random Y offset
        spawnPos.z = 0f;

        // Spawn enemy rocket
        GameObject enemy = Instantiate(enemyRocketPrefab, spawnPos, Quaternion.identity);
        
        // Pass reference to this main rocket and speed info
        EnemyRocket enemyScript = enemy.GetComponent<EnemyRocket>();
        if (enemyScript != null)
        {
            enemyScript.SetTarget(this.transform, speed, spawnOnLeft);
        }

        Debug.Log($"Enemy rocket spawned at {spawnPos}, side: {(spawnOnLeft ? "LEFT" : "RIGHT")}");
    }

    public void HitByEnemy()
    {
        if (isHit) return; // already hit, ignore subsequent hits
        isHit = true;
        hitTargetY = centerY + waveAmplitude; // peak height of the sine wave
        Debug.Log("Rocket hit by enemy! Rising to peak before exploding...");
    }

    public void DestroyRocket()
    {
        // Detach player first if attached
        if (hasPlayer)
            DetachPlayer();

        Debug.Log("Rocket destroyed after lifetime expired!");
        Destroy(gameObject);
    } 
}


