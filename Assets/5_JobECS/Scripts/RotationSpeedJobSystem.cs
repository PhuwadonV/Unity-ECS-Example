using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
struct RotationSpeedJob : IJobForEach<Rotation, RotationSpeed>
{
    public float deltaTime;

    public void Execute(ref Rotation rotation, [ReadOnly] ref RotationSpeed rotationSpeed)
    {
        rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(new float3(1, 0, 0), rotationSpeed.speed * deltaTime));
    }
}

class RotationSpeedJobSystem : JobComponentSystem
{
    private bool checkScene = false;

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        if (!checkScene && !SceneManager.GetActiveScene().name.Equals("JobECS"))
        {
            Enabled = false;
            checkScene = true;
            return new JobHandle();
        }
        else
        {
            return OnUpdateWithCheckScene(inputDependencies);
        }
    }

    private JobHandle OnUpdateWithCheckScene(JobHandle inputDependencies)
    {
        var job = new RotationSpeedJob()
        {
            deltaTime = Time.deltaTime
        };

        return job.Schedule(this, inputDependencies);
    }
}