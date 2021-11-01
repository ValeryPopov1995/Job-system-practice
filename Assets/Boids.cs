using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Burst;

public class Boids : MonoBehaviour
{
    [SerializeField] int arrayLength;
    [SerializeField] float destinationThreashold;
    [SerializeField] int velocityLimit;
    [SerializeField] Vector3 areaSize, weights;
    [SerializeField] GameObject prefab;

    NativeArray<Vector3> positions;
    NativeArray<Vector3> velocities;
    NativeArray<Vector3> accelerations;

    TransformAccessArray access;

    private void Start()
    {
        positions = new NativeArray<Vector3>(arrayLength, Allocator.Persistent);
        velocities = new NativeArray<Vector3>(arrayLength, Allocator.Persistent);
        accelerations = new NativeArray<Vector3>(arrayLength, Allocator.Persistent);

        var transforms = new Transform[arrayLength];
        for (int i = 0; i < arrayLength; i++)
        {
            transforms[i] = Instantiate(prefab).transform;
            velocities[i] = Random.insideUnitSphere;
        }
        access = new TransformAccessArray(transforms);
    }

    private void Update()
    {
        var bJob = new boundsJob
        {
            Positions = positions,
            Accelerations = accelerations,
            AreaSize = areaSize
        };
        var accJob = new accelerationJob
        {
            Positions = positions,
            Velocities = velocities,
            Accelerations = accelerations,
            Weights = weights,
            DestinationThreashold = destinationThreashold
        };
        var mJob = new moveJob
        {
            Positions = positions, 
            Velocities = velocities,
            Accelerations = accelerations,
            VelocityLimit = velocityLimit,
            DeltaTime = Time.deltaTime
        };

        var bHandle = bJob.Schedule(arrayLength, 0);
        var accHandle = accJob.Schedule(arrayLength, 0, bHandle);
        var moveHandle = mJob.Schedule(access, accHandle);
        moveHandle.Complete();
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(Vector3.zero, areaSize);
    }

    private void OnDestroy()
    {
        positions.Dispose();
        velocities.Dispose();
        accelerations.Dispose();
        access.Dispose();
    }
}
[BurstCompile]
struct moveJob : IJobParallelForTransform
{
    [WriteOnly] public NativeArray<Vector3> Positions;
    public NativeArray<Vector3> Velocities;
    public NativeArray<Vector3> Accelerations;
    public int VelocityLimit;

    public float DeltaTime;

    public void Execute(int index, TransformAccess transform)
    {
        var velocity = Velocities[index] + Accelerations[index] * DeltaTime;
        var direction = velocity.normalized;
        velocity = direction * Mathf.Clamp(velocity.magnitude, 1, VelocityLimit);

        transform.position += velocity * DeltaTime;
        transform.rotation = Quaternion.LookRotation(direction);

        Positions[index] = transform.position;
        Velocities[index] = velocity;
        Accelerations[index] = Vector3.zero;
    }
}
[BurstCompile]
struct accelerationJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Vector3> Positions;
    [ReadOnly] public NativeArray<Vector3> Velocities;
    public NativeArray<Vector3> Accelerations;
    [ReadOnly] public Vector3 Weights;
    [ReadOnly] public float DestinationThreashold;

    int count => Positions.Length - 1;

    public void Execute(int index)
    {
        Vector3 avereageSpread = Vector3.zero,
            averageVelocity = Vector3.zero,
            averagePosition = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            if (i == index) continue;
            var vectorDifferense = Positions[index] - Positions[i];
            if (vectorDifferense.magnitude > DestinationThreashold) continue;
            avereageSpread += vectorDifferense.normalized;
            averageVelocity += Velocities[i];
            averagePosition += Positions[i];
        }

        Accelerations[index] += (avereageSpread * Weights.x + averageVelocity * Weights .y + averagePosition * Weights.z) / count - Positions[index];
    }
}
[BurstCompile]
struct boundsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Vector3> Positions;
    public NativeArray<Vector3> Accelerations;
    public Vector3 AreaSize;

    public void Execute(int index)
    {
        var pos = Positions[index];
        var size = AreaSize * .5f;
        Accelerations[index] += compensate(-size.x - pos.x, Vector3.right) +
            compensate(size.x - pos.x, Vector3.left) +
            compensate(-size.y - pos.y, Vector3.up) +
            compensate(size.y - pos.y, Vector3.down) +
            compensate(-size.z - pos.z, Vector3.forward) +
            compensate(size.z - pos.x, Vector3.back);
    }

    Vector3 compensate(float delta, Vector3 direction)
    {
        const int threashold = 3, multiplayer = 100;
        delta = Mathf.Abs(delta);
        if (delta > threashold) return Vector3.zero;
        return direction * (1 - delta / threashold) * multiplayer;
    }
}
