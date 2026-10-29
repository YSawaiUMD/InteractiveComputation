using UnityEngine;

public class DomainSphereLines : MonoBehaviour
{
    [Header("Dome")]
    public float radius = 16f;

    [Range(24, 160)]
    public int segments = 96;

    public int meridians = 12;
    public float lineWidth = 0.045f;

    public Color lineColor =
        new Color(0.35f, 0.35f, 0.40f, 0.40f);

    private Material lineMaterial;

    private void Start()
    {
        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogError(
                "Cannot find Sprites/Default shader.");
            return;
        }

        lineMaterial = new Material(shader);

        // horizontal rings
        DrawLatitude(0f);
        DrawLatitude(radius * 0.20f);
        DrawLatitude(radius * 0.40f);
        DrawLatitude(radius * 0.60f);
        DrawLatitude(radius * 0.80f);

        // dome arcs
        for (int i = 0; i < meridians; i++)
        {
            float angle =
                i * Mathf.PI / meridians;

            DrawMeridian(angle);
        }
    }

    private LineRenderer CreateLine(string lineName)
    {
        GameObject child = new GameObject(lineName);

        child.transform.SetParent(transform, false);

        LineRenderer line =
            child.AddComponent<LineRenderer>();

        line.sharedMaterial = lineMaterial;
        line.useWorldSpace = false;

        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        line.startColor = lineColor;
        line.endColor = lineColor;

        line.numCornerVertices = 2;

        return line;
    }

    private void DrawLatitude(float height)
    {
        LineRenderer line =
            CreateLine("Dome Latitude");

        line.positionCount = segments;
        line.loop = true;

        float ringRadius = Mathf.Sqrt(
            radius * radius - height * height);

        for (int i = 0; i < segments; i++)
        {
            float angle =
                i * Mathf.PI * 2f / segments;

            Vector3 point = new Vector3(
                Mathf.Cos(angle) * ringRadius,
                height,
                Mathf.Sin(angle) * ringRadius);

            line.SetPosition(i, point);
        }
    }

    private void DrawMeridian(float angle)
    {
        LineRenderer line =
            CreateLine("Dome Meridian");

        line.positionCount = segments;
        line.loop = false;

        for (int i = 0; i < segments; i++)
        {
            float t =
                i * Mathf.PI / (segments - 1);

            float horizontal = radius * Mathf.Cos(t);

            Vector3 point = new Vector3(
                Mathf.Cos(angle) * horizontal,
                radius * Mathf.Sin(t),
                Mathf.Sin(angle) * horizontal);

            line.SetPosition(i, point);
        }
    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
            Destroy(lineMaterial);
    }
}
