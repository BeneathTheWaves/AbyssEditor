using UnityEngine;

namespace AbyssEditor.Scripts.BatchOutline
{
    public class BatchOutline : MonoBehaviour
    {
        private MeshRenderer meshRenderer;
        public MeshRenderer GetMeshRenderer()
        {
            if (!meshRenderer)
            {
                return GetComponent<MeshRenderer>();
            }
            return meshRenderer;
        }
    }
}
