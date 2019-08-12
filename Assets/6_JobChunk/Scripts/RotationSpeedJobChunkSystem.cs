using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
struct RotationSpeedJobChunk : IJobChunk
{
    [ReadOnly]
    public float deltaTime;

    public ArchetypeChunkComponentType<Rotation> rotationType;

    [ReadOnly]
    public ArchetypeChunkComponentType<RotationSpeed> rotationSpeedType;

    public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
    {
        var chunkRotations = chunk.GetNativeArray(rotationType);
        var chunkRotationSpeeds = chunk.GetNativeArray(rotationSpeedType);

        for (var i = 0; i < chunk.Count; i++)
        {
            var rotation = chunkRotations[i];
            var rotationSpeed = chunkRotationSpeeds[i];

            chunkRotations[i] = new Rotation
            {
                Value = math.mul(math.normalize(rotation.Value),
                quaternion.AxisAngle(
                    axis: new float3(0, 0, 1),
                    angle: rotationSpeed.speed * deltaTime))
            };
        }
    }
}

class RotationSpeedJobChunkSystem : JobComponentSystem
{
    private EntityQuery entityQuery;

    protected override void OnCreate()
    {
        if (!SceneManager.GetActiveScene().name.Equals("JobChunk"))
        {
            Enabled = false;
        }
        else
        {
            entityQuery = GetEntityQuery(typeof(Rotation), ComponentType.ReadOnly<RotationSpeed>());
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var rotationType = GetArchetypeChunkComponentType<Rotation>(isReadOnly: false);
        var rotationSpeedType = GetArchetypeChunkComponentType<RotationSpeed>(isReadOnly: true);

        var job = new RotationSpeedJobChunk()
        {
            rotationType = rotationType,
            rotationSpeedType = rotationSpeedType,
            deltaTime = Time.deltaTime
        };

        return job.Schedule(entityQuery, inputDependencies);
    }
}