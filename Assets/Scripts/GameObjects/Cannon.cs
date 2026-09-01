using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Cannon : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 45f; // degrees per second
    [SerializeField] float maxAngle = 90f;      // maximum rotation angle

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float launchForce = 10f;
    [SerializeField] private float fireDelay = 1f;
    [SerializeField] private Animator animator;

    [SerializeField] private CameraFollower cameraFollower; // Drag your Main Camera here

    private float startAngle;

    private PlayerInputActions inputActions;
    private bool hasFired = false; // ← flag to track if player was fired


    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    void Start()
    {
        // Store the starting angle (assuming local rotation around Z-axis)
        startAngle = transform.localEulerAngles.z;
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.FireFromCannon.performed += OnFire;
    }

    private void OnDisable()
    {
        inputActions.Player.FireFromCannon.performed -= OnFire;
        inputActions.Disable();
    }

    void Update()
    {
        Rotate();
    }

    private void Rotate()
    {
        if (!hasFired)
        {
            // PingPong gives a value that moves back and forth between 0 and maxAngle
            float angle = Mathf.PingPong(Time.time * rotationSpeed, maxAngle);

            // Apply the rotation relative to the starting angle
            transform.localRotation = Quaternion.Euler(0f, 0f, startAngle + angle);
        }
    }

    private void OnFire(InputAction.CallbackContext context)
    {
        if (hasFired) return; // prevent firing again

        CallSpawnAndLaunchPlayer();
        hasFired = true; // mark as fired
    }

    private void CallSpawnAndLaunchPlayer()
    {
        if (playerPrefab == null || spawnPoint == null) return;
        animator.SetBool("hasShot", true);
        StartCoroutine(SpawnAndLaunchPlayer());
    }

    private IEnumerator SpawnAndLaunchPlayer()
    {
        yield return new WaitForSeconds(fireDelay);

        GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Clear any existing velocity
            rb.AddForce(spawnPoint.right * launchForce, ForceMode2D.Impulse); // Launch in spawnPoint's right direction
            Debug.Log("Player launched from cannon!");
        }
        else
        {
            Debug.LogWarning("Player prefab has no Rigidbody2D!");
        }

        // Make the camera follow the spawned player
        if (cameraFollower != null)
        {
            cameraFollower.target = player.transform;
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
            ScoreManager.Instance.StartScoring();
        }
    }
}
