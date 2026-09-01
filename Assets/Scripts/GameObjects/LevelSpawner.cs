using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class LevelSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject trampolinePrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject rocketPrefab;
    private PlayerController player;

    [Header("Trampoline Settings")]
    [SerializeField] private float trampolineInterval = 3f;

    [Header("Wall Settings")]
    [SerializeField] private float wallMinY = -6f;
    [SerializeField] private float wallInterval = 2f;

    [Header("Event Settings")]
    [SerializeField] private float eventInterval = 50f;

    [Header("Rocket Settings")]
    [SerializeField] private float rocketSpawnBehindX = 12f;
    [SerializeField] private float rocketSpawnBelowTop = 12f;

    [Header("Offsets")]
    [SerializeField] private float spawnAheadX = 10f;
    [SerializeField] private float despawnDistance = 15f;

    private float trampolineTimer = 0f;
    private float wallTimer = 0f;
    private float eventTimer = 0f;
    private float nextEventTime;
    private float nextTrampolineTime;
    private float nextWallTime;
    private float lastWallSpawnX = float.NegativeInfinity;
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
        nextTrampolineTime = RandomInterval(trampolineInterval);
        nextWallTime = RandomInterval(wallInterval);
        nextEventTime = RandomInterval(eventInterval);
    }

    private float RandomInterval(float interval)
    {
        return Random.Range(interval * 0.1f, interval * 2f);
    }

    private void Update()
    {
        if (player == null)
        {
            // Versuche den Spieler in der Szene zu finden
            player = FindAnyObjectByType<PlayerController>();
            if (player == null)
                return; // noch kein Spieler, nichts spawnen
        }

        trampolineTimer += Time.deltaTime;
        wallTimer += Time.deltaTime;
        eventTimer += Time.deltaTime;

        HandleTrampolineSpawn();
        HandleWallSpawn();
        HandleEventSpawn();
        DespawnObjects();
    }

    private void HandleTrampolineSpawn()
    {
        if (trampolineTimer >= nextTrampolineTime)
        {
            SpawnTrampoline();
            nextTrampolineTime = RandomInterval(trampolineInterval);
        }
    }

    private void HandleWallSpawn()
    {
        if (wallTimer >= nextWallTime)
        {
            SpawnWall();
            nextWallTime = RandomInterval(wallInterval);
        }
    }

    private void HandleEventSpawn()
    {
        if (eventTimer >= nextEventTime)
        {
            SpawnEvent();
            nextEventTime = RandomInterval(eventInterval);
        }
    }

    private void SpawnTrampoline()
    {
        Vector3 spawnPos = cam.transform.position;
        spawnPos.x += spawnAheadX;
        spawnPos.y = -10f; // Bodenhöhe fix
        spawnPos.z = 0f;

        SpawnObject(trampolinePrefab, spawnPos, Quaternion.identity);
        trampolineTimer = 0f;
    }

    private void SpawnWall()
    {
        // Max Y is always 6 units below the top of the screen
        float screenTop = cam.transform.position.y + cam.orthographicSize;
        float maxY = screenTop - 4f;

        if (maxY < wallMinY) return; // no valid range

        Vector3 spawnPos = cam.transform.position;
        spawnPos.x += spawnAheadX;

        // Ensure at least 12 units apart from the last wall
        if (spawnPos.x - lastWallSpawnX < 2f) return;

        spawnPos.y = Random.Range(wallMinY, maxY);
        spawnPos.z = 0f;

        float[] rotations = { 0f, 45f, -45f };
        float zRot = rotations[Random.Range(0, rotations.Length)];
        Quaternion rot = Quaternion.Euler(0, 0, zRot);

        SpawnObject(wallPrefab, spawnPos, rot);
        lastWallSpawnX = spawnPos.x;
        wallTimer = 0f;
    }

    private void SpawnEvent()
    {
        SpawnRocket();
        eventTimer = 0f;
    }

    private void SpawnRocket()
    {
        // Spawn behind the left edge of the screen so the rocket flies into view
        float screenLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;
        float screenTop = cam.transform.position.y + cam.orthographicSize;

        Vector3 spawnPos = new Vector3(screenLeft - rocketSpawnBehindX, screenTop - rocketSpawnBelowTop, 0f);

        SpawnObject(rocketPrefab, spawnPos, Quaternion.identity);
    }

    private void SpawnObject(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        GameObject obj = Instantiate(prefab, pos, rot);
        spawnedObjects.Add(obj);
    }

    private void DespawnObjects()
    {
        float camX = cam.transform.position.x;
        float camY = cam.transform.position.y;

        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = spawnedObjects[i];
            if (obj == null)
            {
                spawnedObjects.RemoveAt(i);
                continue;
            }

            if (obj.transform.position.x < camX - despawnDistance ||
                obj.transform.position.y < camY - despawnDistance)
            {
                Destroy(obj);
                spawnedObjects.RemoveAt(i);
            }
        }
    }
}
