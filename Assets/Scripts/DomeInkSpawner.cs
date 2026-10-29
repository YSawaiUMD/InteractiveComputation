using UnityEngine;

public class DomeInkSpawner : MonoBehaviour
{
    public DomeInkStroke strokePrefab;

    [Header("Dome")]
    public float domeRadius = 16f;
    public float minHeight = 3f;
    public float maxHeight = 13f;

    [Header("Stroke Movement")]
    public float spawnInterval = 0.28f;
    public float strokeDuration = 2.2f;
    public float minSpeed = 0.5f;
    public float maxSpeed = 0.9f;
    public bool randomDirection = false;

    private float timer;

    private void Update()
    {
        if (strokePrefab == null) return;

        timer -= Time.deltaTime;

        if (timer > 0f) return;

        timer = Mathf.Max(0.05f, spawnInterval);
        SpawnStroke();
    }

    private void SpawnStroke()
    {
        // spawn just inside the dome
        float radius = Mathf.Max(1f, domeRadius - 0.2f);

        float low = Mathf.Clamp(minHeight, 0.1f, radius - 0.1f);
        float high = Mathf.Clamp(maxHeight, low, radius - 0.1f);

        float height = Random.Range(low, high);
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float speed = Random.Range(minSpeed, maxSpeed);

        if (randomDirection && Random.value < 0.5f)
            speed = -speed;

        DomeInkStroke stroke =
            Instantiate(strokePrefab, transform);

        stroke.Begin(
            radius,
            height,
            angle,
            speed,
            strokeDuration);
    }
}
