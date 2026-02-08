using UnityEngine;

namespace AbyssEditor.Scripts.EntityHandling
{
    public class BasicEntityPreview : EntityPreviewBase
    {
        public BasicEntityPreview(EntityData entity) : base(entity)
        {
        }

        public override GameObject InstantiatePreview(EntityInstance entityInstance)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SetTransformFromEntityInstance(cube.transform, entityInstance);
            return cube;
        }
    }
}
