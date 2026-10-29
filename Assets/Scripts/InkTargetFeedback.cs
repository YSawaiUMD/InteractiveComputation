using UnityEngine;

public class InkTargetFeedback : MonoBehaviour
{
    public Renderer targetRenderer;
    public Material normalMaterial;
    public Material hitMaterial;

    public ParticleSystem goldImpact;
    public ParticleSystem inkSplash;

    public float flashDuration = 0.12f;

    private float flashTimer;

    private void Start()
    {
        targetRenderer.sharedMaterial = normalMaterial;
    }

    public void Pulse()
    {
        // flash on hit
        targetRenderer.sharedMaterial = hitMaterial;
        flashTimer = flashDuration;

        // hit particles
        if (goldImpact != null)
        {
            goldImpact.Stop(true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
            goldImpact.Play();
        }

        if (inkSplash != null)
        {
            inkSplash.Stop(true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
            inkSplash.Play();
        }
    }

    private void Update()
    {
        if (flashTimer <= 0f) return;

        flashTimer -= Time.deltaTime;

        if (flashTimer <= 0f)
            targetRenderer.sharedMaterial = normalMaterial;
    }
}
