using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

class RotationSpeedSystem : ComponentSystem
{
    protected override void OnCreate()
    {
        if (!SceneManager.GetActiveScene().name.Equals("ECS"))
        {
            Enabled = false;
        }
    }

    protected override void OnUpdate()
    {
        Entities.ForEach((ref Rotation rotation, ref RotationSpeed rotationSpeed) =>
            rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(math.up(), rotationSpeed.speed * Time.deltaTime))
        );
    }
}