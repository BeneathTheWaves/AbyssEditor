using System.Collections.Generic;
using AbyssEditor.Scripts.Util;
using AbyssEditor.Scripts.VoxelTech;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing;
using UnityEngine;

namespace AbyssEditor.Scripts.BatchOutline
{
    public class BatchOutlineManager : MonoBehaviour
    {
        public static BatchOutlineManager main;
        private static readonly int wireframeColor = Shader.PropertyToID("_WireframeColor");

        [SerializeField] private GameObject batchOutlinePrefab;
        
        private GameObject[] batchLoadOutlines;
        
        private BatchOutline hoveredOutline;

        private static readonly Color DefaultColor = new(0.88f, 0.95f, 0.95f);
        private static readonly Color DeleteColor = new(0.93f, 0, 0);
        private static readonly Color LoadColor = Color.rebeccaPurple;
        
        private void Awake()
        {
            main = this;
        }

        public void ResetOutlines()
        {
            DeleteOldOutlines(batchLoadOutlines);
        }

        //TODO: could be nice to pool these later and not destroy :/
        private static void DeleteOldOutlines(GameObject[] outlines)
        {
            if (outlines == null) return;
            
            for(int i = 0; i < outlines.Length; i++)
            {
                Destroy(outlines[i]);
                outlines[i] = null;
            }
        }

        public void DrawBatchOutline(Vector3Int startBatchIndex, Vector3Int endBatchIndex)
        {
            DeleteOldOutlines(batchLoadOutlines);
            
            List<GameObject> outlines = new List<GameObject>();
            foreach (Vector3Int batchIndex in startBatchIndex.IterateTo(endBatchIndex))
            {
                outlines.Add(DrawBatchLoadOutline(batchIndex));
            }
            batchLoadOutlines = outlines.ToArray();
        }

        public void DrawBatchOutline(List<Vector3Int> batchIndexes)
        {
            DeleteOldOutlines(batchLoadOutlines);
            
            List<GameObject> outlines = new List<GameObject>();            
            foreach (Vector3Int batchIndex in batchIndexes)
            {
                outlines.Add(DrawBatchLoadOutline(batchIndex));
            }
            batchLoadOutlines = outlines.ToArray();
        }
        
        public void DrawBatchRemoveOutlines(List<Vector3Int> batchIndexes)
        {
            DeleteOldOutlines(batchLoadOutlines);
            
            List<GameObject> outlines = new List<GameObject>();            
            foreach (Vector3Int batchIndex in batchIndexes)
            {
                outlines.Add(DrawBatchRemoveOutline(batchIndex));
            }
            batchLoadOutlines = outlines.ToArray();
        }

        public void HoverRemoveOutline(BatchOutline outline)
        {
            if (outline != null && outline == hoveredOutline)
                return;

            // Reset old
            if (hoveredOutline != null)
            {
                hoveredOutline.GetMeshRenderer().material.SetColor(wireframeColor, DefaultColor);
            }
            
            hoveredOutline = outline;

            if (hoveredOutline != null)
            {
                hoveredOutline.GetMeshRenderer().material.SetColor(wireframeColor, DeleteColor);
            }
        }
        
        private GameObject DrawBatchLoadOutline(Vector3Int batchIndex)
        {
            GameObject outline = Instantiate(batchOutlinePrefab, batchIndex * VoxelWorld.BATCH_WIDTH, Quaternion.identity);

            GameObject cube = outline.transform.GetChild(0).gameObject;
            cube.AddComponent<BatchOutline>();
            MeshRenderer meshRenderer = cube.GetComponent<MeshRenderer>();
            
            if (VoxelMetaspace.metaspace.TryGetVoxelBatch(batchIndex, out VoxelBatch _))
            {
                //batch exists to make it red
                meshRenderer.material.SetColor(wireframeColor, DeleteColor );
                return outline;
            }
            
            meshRenderer.material.SetColor(wireframeColor, LoadColor);
            return outline;
        }
        
        
        private GameObject DrawBatchRemoveOutline(Vector3Int batchIndex)
        {
            GameObject outline = Instantiate(batchOutlinePrefab, batchIndex * VoxelWorld.BATCH_WIDTH, Quaternion.identity);

            GameObject cube = outline.transform.GetChild(0).gameObject;
            cube.AddComponent<BatchOutline>();
            cube.GetComponent<MeshRenderer>().material.SetColor(wireframeColor, DefaultColor);
            
            return outline;
        }
    }
}
