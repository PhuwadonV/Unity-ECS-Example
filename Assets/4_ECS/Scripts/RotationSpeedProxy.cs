using UnityEngine;
using Unity.Entities;

[AddComponentMenu("ECS/RotationSpeed")]
[RequireComponent(typeof(ConvertToEntity))]
[RequiresEntityConversion]
public class RotationSpeedProxy : MonoBehaviour, IConvertGameObjectToEntity
{
    public float speed;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new RotationSpeed { speed = speed });
    }
}