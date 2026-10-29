using UnityEngine;

public class DomeInkStroke : MonoBehaviour
{
    private float radius;
    private float height;
    private float angle;
    private float speed;
    private float lifetime;

    private TrailRenderer trail;

    public void Begin(float domeRadius, float startHeight,
        float startAngle, float moveSpeed, float duration)
    {
        height = startHeight;
        angle = startAngle;
        speed = moveSpeed;
        lifetime = duration;

        // circle radius at this height
        radius = Mathf.Sqrt(
            domeRadius * domeRadius - height * height);

        trail = GetComponent<TrailRenderer>();

        // set position before
        // starting trail
        transform.localPosition = GetPosition();

        if (trail != null)
        {
            trail.Clear();
            trail.emitting = true;
        }
    }

    private Vector3 GetPosition()
    {
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        return new Vector3(x, height, z);
    }

    private void Update()
    {
        // move around dome
        angle += speed * Time.deltaTime;
        transform.localPosition = GetPosition();

        lifetime -= Time.deltaTime;

        if (lifetime <= 0f)
        {
            if (trail != null)
                trail.emitting = false;

            // let trail fade out
            Destroy(gameObject, trail != null ? trail.time : 0f);
            enabled = false;
        }
    }
}
