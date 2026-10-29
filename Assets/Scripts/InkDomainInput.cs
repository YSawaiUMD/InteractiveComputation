using UnityEngine;

public class InkDomainInput : MonoBehaviour
{
    [Header("References")]
    public InkDomainSwarm swarm;
    public Camera viewCamera;

    [Header("Painting")]
    public float holdThreshold = 0.18f;
    public float paintDistance = 9f;

    private bool wasHolding;
    private bool usedPaint;
    private float pressStarted;

    private void Start()
    {
        if (swarm != null && viewCamera != null)
            swarm.viewCamera = viewCamera.transform;
    }

    private void Update()
    {
        if (swarm == null || viewCamera == null)
            return;

        // no input when cursor is free
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (usedPaint)
                swarm.EndPaint();

            wasHolding = false;
            usedPaint = false;
            return;
        }

        // mouse or space
        bool holding =
            Input.GetMouseButton(0) ||
            Input.GetKey(KeyCode.Space);

        if (holding && !wasHolding)
        {
            pressStarted = Time.time;
            usedPaint = false;
        }

        // holding starts painting
        if (holding && !usedPaint &&
            Time.time - pressStarted >= holdThreshold)
        {
            usedPaint = swarm.TryBeginPaint(
                GetPaintPoint());
        }

        // update paint point
        if (holding && usedPaint && swarm.IsPainting)
        {
            swarm.SetPaintPoint(GetPaintPoint());
        }

        if (!holding && wasHolding)
        {
            if (usedPaint)
            {
                swarm.EndPaint();
            }
            else
            {
                // tap to attack
                swarm.RequestAttack();
            }

            usedPaint = false;
        }

        wasHolding = holding;
    }

    private Vector3 GetPaintPoint()
    {
        // aim from screen center
        Ray ray = viewCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f));

        if (swarm.attackTarget != null)
        {
            Collider targetCollider =
                swarm.attackTarget.GetComponent<Collider>();

            // hit target collider
            if (targetCollider != null &&
                targetCollider.Raycast(
                    ray, out RaycastHit hit, 100f))
            {
                return hit.point;
            }

            // otherwise use target
            // depth on ray
            Vector3 toTarget =
                swarm.attackTarget.position -
                viewCamera.transform.position;

            float distance = Mathf.Clamp(
                Vector3.Dot(toTarget, ray.direction),
                4f,
                Mathf.Max(5f, toTarget.magnitude + 4f));

            return ray.GetPoint(distance);
        }

        // no target fallback
        return ray.GetPoint(paintDistance);
    }
}
