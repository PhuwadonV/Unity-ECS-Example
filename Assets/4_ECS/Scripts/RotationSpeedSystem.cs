using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

class RotationSpeedSystem : ComponentSystem
{
    private bool checkScene = false;

    protected override void OnUpdate()
    {
        if (!checkScene && !SceneManager.GetActiveScene().name.Equals("ECS"))
        {
            Enabled = false;
            checkScene = true;
        }
        else
        {
            OnUpdateWithCheckScene();
        }
    }

    private void OnUpdateWithCheckScene()
    {
        Entities.ForEach((ref Rotation rotation, ref RotationSpeed rotationSpeed) =>
            rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(math.up(), rotationSpeed.speed * Time.deltaTime))
        );
    }
}