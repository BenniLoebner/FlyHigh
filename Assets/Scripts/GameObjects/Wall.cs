using UnityEngine;

public class Wall : MonoBehaviour
{
    [Header("Flying Motion")]
    [SerializeField] private float bobAmplitude = 0.3f;   // How far up/down it moves
    [SerializeField] private float bobSpeed = 2f;         // How fast it bobs
    
    private float startY;
    private float randomOffset;

    void Start()
    {
        startY = transform.position.y;
        // Random offset so walls don't all bob in sync
        randomOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        // Create bobbing motion using sine wave
        float newY = startY + Mathf.Sin(Time.time * bobSpeed + randomOffset) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}
