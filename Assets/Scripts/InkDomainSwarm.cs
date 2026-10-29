
using System.Collections.Generic;
using UnityEngine;

// based on the class InteractiveBody / PianoBoids examples
// position velocity acceleration + neighbor forces
// added flock slots and paint controls

public class InkDomainSwarm : MonoBehaviour
{
    [Header("References")]
    public Transform birdPrefab;
    public Transform attackTarget;
    public InkTargetFeedback targetFeedback;
    public Transform viewCamera;

    [Header("Formation")]
    [Range(4, 12)] public int flockCount = 8;
    [Range(3, 20)] public int birdsPerFlock = 8;
    public float domainRadius = 16f;
    public float orbitRadius = 11f;
    public float orbitHeight = 5.5f;
    public float orbitAngularSpeed = 0.08f;

    [Header("Boids Forces")]
    public float neighborRadius = 2.6f;
    public float separationRadius = 1f;
    public float separationWeight = 5f;
    public float alignmentWeight = 1.2f;
    public float cohesionWeight = 1.3f;
    public float interFlockAvoidance = 9f;
    public float maxAcceleration = 100f;

    [Header("Movement")]
    public float orbitSpeed = 5f;
    public float diveSpeed = 23f;
    public float paintSpeed = 17f;
    public float returnSpeed = 19f;

    [Header("Attacks")]
    public float minAttackInterval = 0.12f;
    public int maxQueuedAttacks = 12;
    public float maxPaintDuration = 2.5f;
    public float targetHitRadius = 2.1f;

    private enum Mode { Orbit, Dive, Paint, Exit, Recover }

    private class Flock
    {
        public int slot;
        public Mode mode = Mode.Orbit;
        public float elapsed;
        public float intensity = 1f;
        public Vector3 direction;
        public Vector3 paintPoint;
        public bool canHit = true;
    }

    // same idea as BodyProperty from class
    private struct Bird
    {
        public int flock;
        public Vector3 position, velocity, acceleration, offset;
        public Transform visual;
    }

    private Bird[] birds;
    private Flock[] flocks;
    private int[] slotOwners;
    private Vector3[] oldPositions, oldVelocities;

    private readonly Queue<float> attackQueue =
        new Queue<float>();

    private int paintingFlock = -1;
    private int lastAttackSide;
    private float lastAttackTime = -100f;

    public bool IsPainting => paintingFlock >= 0 &&
        flocks != null &&
        flocks[paintingFlock].mode == Mode.Paint;

    public int QueuedAttackCount => attackQueue.Count;

    public int AvailableFlockCount
    {
        get
        {
            if (flocks == null) return 0;

            int count = 0;
            foreach (Flock flock in flocks)
                if (flock.mode == Mode.Orbit) count++;

            return count;
        }
    }

    // setup

    private void Start()
    {
        if (birdPrefab == null || attackTarget == null)
        {
            Debug.LogError(
                "Assign Bird Prefab and Attack Target.", this);
            enabled = false;
            return;
        }

        flockCount = Mathf.Clamp(flockCount, 4, 12);

        // need even number of slots
        if (flockCount % 2 != 0) flockCount++;

        birdsPerFlock = Mathf.Clamp(birdsPerFlock, 3, 20);
        orbitRadius = Mathf.Min(orbitRadius, domainRadius - 3f);

        flocks = new Flock[flockCount];
        slotOwners = new int[flockCount];

        birds = new Bird[flockCount * birdsPerFlock];
        oldPositions = new Vector3[birds.Length];
        oldVelocities = new Vector3[birds.Length];

        for (int g = 0; g < flockCount; g++)
        {
            flocks[g] = new Flock { slot = g };
            slotOwners[g] = g;

            for (int b = 0; b < birdsPerFlock; b++)
            {
                int i = g * birdsPerFlock + b;
                Bird bird = new Bird();

                bird.flock = g;
                bird.offset = Random.insideUnitSphere * 0.65f;
                bird.position = GetSlotPosition(g) + bird.offset;
                bird.velocity = GetSlotTangent(g) * orbitSpeed;

                // prefab has the model,
                // anim and trail
                bird.visual = Instantiate(birdPrefab, transform);
                bird.visual.position = bird.position;
                bird.visual.rotation =
                    Quaternion.LookRotation(bird.velocity);

                birds[i] = bird;
            }
        }
    }

    // flock formation

    private Vector3 GetSlotPosition(int slot)
    {
        float angle = slot * 2f * Mathf.PI / flockCount +
            Time.time * orbitAngularSpeed;

        float height = orbitHeight + Mathf.Sin(angle * 2f);

        return transform.position + new Vector3(
            Mathf.Cos(angle) * orbitRadius,
            height,
            Mathf.Sin(angle) * orbitRadius);
    }

    private Vector3 GetSlotTangent(int slot)
    {
        float angle = slot * 2f * Mathf.PI / flockCount +
            Time.time * orbitAngularSpeed;

        return new Vector3(
            -Mathf.Sin(angle), 0f, Mathf.Cos(angle));
    }

    private Vector3 GetFlockCenter(int group)
    {
        Vector3 center = Vector3.zero;
        int start = group * birdsPerFlock;

        for (int i = start; i < start + birdsPerFlock; i++)
            center += birds[i].position;

        return center / birdsPerFlock;
    }

    // move around to its slot
    private Vector3 GetOrbitVelocity(Vector3 position, int slot)
    {
        Vector3 radial = position - transform.position;
        radial.y = 0f;

        Vector3 home = GetSlotPosition(slot);
        Vector3 homeRadial = home - transform.position;
        homeRadial.y = 0f;

        if (radial.sqrMagnitude < 0.25f)
            return (home - position).normalized * orbitSpeed;

        Vector3 outward = radial.normalized;

        Vector3 forward = new Vector3(
            -outward.z, 0f, outward.x);

        float angleAhead = -Vector3.SignedAngle(
            radial, homeRadial, Vector3.up) * Mathf.Deg2Rad;

        float speed = Mathf.Clamp(
            Mathf.Abs(orbitAngularSpeed) * orbitRadius +
            angleAhead * orbitRadius * 0.65f,
            0.3f,
            orbitSpeed * 2f);

        Vector3 radiusCorrection = outward * Mathf.Clamp(
            (orbitRadius - radial.magnitude) * 2f,
            -orbitSpeed, orbitSpeed);

        Vector3 heightCorrection = Vector3.up * Mathf.Clamp(
            (home.y - position.y) * 2f,
            -orbitSpeed, orbitSpeed);

        return forward * speed +
               radiusCorrection + heightCorrection;
    }

    // swap attack slot
    // other flocks move up
    private void ShiftSlotsForAttack(int attacker)
    {
        int source = flocks[attacker].slot;
        int half = flockCount / 2;

        for (int k = 0; k < half; k++)
        {
            int current = (source - k + flockCount) % flockCount;
            int previous = (current - 1 + flockCount) % flockCount;

            int moving = slotOwners[previous];

            slotOwners[current] = moving;
            flocks[moving].slot = current;
        }

        int opposite = (source - half + flockCount) % flockCount;

        slotOwners[opposite] = attacker;
        flocks[attacker].slot = opposite;
    }

    // attack input

    public void RequestAttack(float intensity = 1f)
    {
        if (attackQueue.Count < maxQueuedAttacks)
            attackQueue.Enqueue(
                Mathf.Clamp(intensity, 0.4f, 2f));
    }

    public bool TryBeginPaint(Vector3 point)
    {
        if (flocks == null || IsPainting ||
            Time.time - lastAttackTime < minAttackInterval)
            return false;

        return LaunchFlock(FindAvailableFlock(), true, 1f, point);
    }

    public void SetPaintPoint(Vector3 point)
    {
        if (IsPainting)
            flocks[paintingFlock].paintPoint = ClampToDome(point);
    }

    public void EndPaint()
    {
        if (!IsPainting) return;

        flocks[paintingFlock].mode = Mode.Exit;
        flocks[paintingFlock].elapsed = 0f;
        paintingFlock = -1;
    }

    // try left/right side first
    private int FindAvailableFlock()
    {
        int best = -1;
        float bestScore = -1f;

        Vector3 right = viewCamera != null
            ? viewCamera.right : Vector3.right;

        right.y = 0f;
        right.Normalize();

        for (int g = 0; g < flockCount; g++)
        {
            if (flocks[g].mode != Mode.Orbit) continue;

            Vector3 direction =
                GetFlockCenter(g) - attackTarget.position;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) continue;

            float side = Vector3.Dot(
                direction.normalized, right);

            float score = Mathf.Abs(side);

            // switch sides if possible
            if (lastAttackSide != 0 &&
                Mathf.Sign(side) != lastAttackSide)
                score += 0.2f;

            if (score > bestScore)
            {
                best = g;
                bestScore = score;
            }
        }

        return best;
    }

    private bool LaunchFlock(int index, bool painting,
        float intensity, Vector3 paintPoint)
    {
        if (index < 0 || flocks[index].mode != Mode.Orbit)
            return false;

        Flock flock = flocks[index];

        Vector3 from =
            GetFlockCenter(index) - attackTarget.position;

        Vector3 direction = -from;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            direction = Vector3.right;

        Vector3 right = viewCamera != null
            ? viewCamera.right : Vector3.right;

        right.y = 0f;

        lastAttackSide =
            Vector3.Dot(from, right) >= 0f ? 1 : -1;

        flock.direction = direction.normalized;
        flock.intensity = Mathf.Clamp(intensity, 0.4f, 2f);
        flock.elapsed = 0f;
        flock.canHit = true;
        flock.paintPoint = ClampToDome(paintPoint);

        ShiftSlotsForAttack(index);

        flock.mode = painting ? Mode.Paint : Mode.Dive;

        if (painting) paintingFlock = index;

        lastAttackTime = Time.time;
        return true;
    }

    private void DispatchAttacks()
    {
        if (attackQueue.Count == 0 ||
            Time.time - lastAttackTime < minAttackInterval)
            return;

        int index = FindAvailableFlock();

        // wait for a free flock
        if (index < 0) return;

        LaunchFlock(index, false,
            attackQueue.Dequeue(), attackTarget.position);
    }

    // flock states

    private void UpdateFlocks(float dt)
    {
        for (int g = 0; g < flockCount; g++)
        {
            Flock flock = flocks[g];

            if (flock.mode == Mode.Orbit) continue;

            flock.elapsed += dt;

            Vector3 center = GetFlockCenter(g);

            float targetDistance = Vector3.Distance(
                center, attackTarget.position);

            float homeDistance = Vector3.Distance(
                center, GetSlotPosition(flock.slot));

            if (flock.mode == Mode.Paint)
            {
                // paint can hit again
                if (targetDistance < targetHitRadius &&
                    flock.canHit)
                {
                    if (targetFeedback != null)
                        targetFeedback.Pulse();

                    flock.canHit = false;
                }

                // reset hit after leaving
                if (targetDistance > targetHitRadius + 1.5f)
                    flock.canHit = true;

                if (flock.elapsed >= maxPaintDuration)
                {
                    if (paintingFlock == g)
                        EndPaint();
                    else
                    {
                        flock.mode = Mode.Exit;
                        flock.elapsed = 0f;
                    }
                }
            }
            else if (flock.mode == Mode.Dive)
            {
                bool crossed = Vector3.Dot(
                    center - attackTarget.position,
                    flock.direction) >= 0f;

                if (targetDistance < 1.5f ||
                    crossed || flock.elapsed > 2.5f)
                {
                    if (targetDistance < 3f &&
                        targetFeedback != null)
                        targetFeedback.Pulse();

                    flock.mode = Mode.Exit;
                    flock.elapsed = 0f;
                }
            }
            else if (flock.mode == Mode.Exit &&
                     (homeDistance < 2.2f || flock.elapsed > 3f))
            {
                flock.mode = Mode.Recover;
                flock.elapsed = 0f;
            }
            else if (flock.mode == Mode.Recover &&
                     ((flock.elapsed > 0.4f &&
                       homeDistance < 3f) ||
                      flock.elapsed > 1.6f))
            {
                flock.mode = Mode.Orbit;
                flock.elapsed = 0f;
            }
        }
    }

    // boids algorithm

    private Vector3 CalculateBoidsForce(int i)
    {
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        Vector3 otherAvoidance = Vector3.zero;

        int neighbors = 0;

        for (int j = 0; j < birds.Length; j++)
        {
            if (i == j) continue;

            Vector3 away = oldPositions[i] - oldPositions[j];
            float sq = away.sqrMagnitude;

            if (sq < 0.0001f) continue;

            if (birds[i].flock != birds[j].flock)
            {
                // avoid other flocks
                if (sq < 2.25f)
                    otherAvoidance += away / (sq + 0.08f);

                continue;
            }

            if (sq > neighborRadius * neighborRadius) continue;

            neighbors++;
            alignment += oldVelocities[j];
            cohesion += oldPositions[j];

            if (sq < separationRadius * separationRadius)
                separation += away / sq;
        }

        Vector3 force =
            Vector3.ClampMagnitude(otherAvoidance, 4f) *
            interFlockAvoidance;

        if (neighbors == 0) return force;

        // separation
        force += Vector3.ClampMagnitude(
            separation, 3f) * separationWeight;

        // alignment
        force += (alignment / neighbors - oldVelocities[i]) *
            alignmentWeight;

        // cohesion
        force += (cohesion / neighbors - oldPositions[i]) *
            cohesionWeight;

        return force;
    }

    // different movement for each state
    private Vector3 GetGoalVelocity(Bird bird, Flock flock,
        out float speedLimit, out float guidance)
    {
        Vector3 goal;

        switch (flock.mode)
        {
            case Mode.Dive:
                speedLimit = diveSpeed * flock.intensity;
                guidance = 6f;

                goal = attackTarget.position +
                    flock.direction * 0.7f +
                    bird.offset * 0.15f;

                return (goal - bird.position).normalized *
                    speedLimit;

            case Mode.Paint:
                speedLimit = paintSpeed;
                guidance = 7f;

                goal = flock.paintPoint + bird.offset * 0.5f;

                return (goal - bird.position).normalized *
                    speedLimit;

            case Mode.Exit:
                speedLimit = returnSpeed;
                guidance = 5f;

                goal = GetSlotPosition(flock.slot) + bird.offset;

                return (goal - bird.position).normalized *
                    speedLimit;

            case Mode.Recover:
                guidance = 5f;

                Vector3 recovery =
                    GetOrbitVelocity(bird.position, flock.slot);

                speedLimit = Mathf.Max(
                    returnSpeed, recovery.magnitude);

                return recovery;

            default:
                guidance = 4f;

                Vector3 orbit =
                    GetOrbitVelocity(bird.position, flock.slot);

                speedLimit = Mathf.Max(
                    orbitSpeed * 2f, orbit.magnitude);

                return orbit;
        }
    }

    // keep inside dome
    private Vector3 ClampToDome(Vector3 point)
    {
        Vector3 local = point - transform.position;

        float radius = Mathf.Max(3f, domainRadius - 1.3f);

        local.y = Mathf.Clamp(
            local.y, 1.2f, radius - 0.05f);

        float horizontalLimit = Mathf.Sqrt(Mathf.Max(
            0.01f, radius * radius - local.y * local.y));

        Vector2 flat = new Vector2(local.x, local.z);

        if (flat.magnitude > horizontalLimit)
        {
            flat = flat.normalized * horizontalLimit;
            local.x = flat.x;
            local.z = flat.y;
        }

        return transform.position + local;
    }

    // simulation

    private void FixedUpdate()
    {
        if (birds == null) return;

        float dt = Time.fixedDeltaTime;

        DispatchAttacks();
        UpdateFlocks(dt);

        // save positions first so every bird uses
        // the same neighbor data
        for (int i = 0; i < birds.Length; i++)
        {
            oldPositions[i] = birds[i].position;
            oldVelocities[i] = birds[i].velocity;
        }

        for (int i = 0; i < birds.Length; i++)
        {
            Bird bird = birds[i];
            Flock flock = flocks[bird.flock];

            Vector3 goalVelocity = GetGoalVelocity(
                bird, flock,
                out float speedLimit, out float guidance);

            // boids + goal force
            bird.acceleration = CalculateBoidsForce(i) +
                (goalVelocity - oldVelocities[i]) * guidance;

            // keep inside arena
            Vector3 fromCenter =
                bird.position - transform.position;

            float safeRadius = domainRadius - 1.8f;

            if (fromCenter.magnitude > safeRadius)
                bird.acceleration -= fromCenter.normalized *
                    (fromCenter.magnitude - safeRadius) * 30f;

            if (fromCenter.y < 1.5f)
                bird.acceleration += Vector3.up *
                    (1.5f - fromCenter.y) * 35f;

            bird.acceleration = Vector3.ClampMagnitude(
                bird.acceleration, maxAcceleration);

            // velocity then position
            bird.velocity = Vector3.ClampMagnitude(
                oldVelocities[i] + bird.acceleration * dt,
                speedLimit * 1.2f);

            bird.position = ClampToDome(
                bird.position + bird.velocity * dt);

            // update bird visual
            bird.visual.position = bird.position;

            if (bird.velocity.sqrMagnitude > 0.1f)
            {
                bird.visual.rotation = Quaternion.Slerp(
                    bird.visual.rotation,
                    Quaternion.LookRotation(
                        bird.velocity.normalized, Vector3.up),
                    dt * 12f);
            }

            birds[i] = bird;
        }
    }
}
