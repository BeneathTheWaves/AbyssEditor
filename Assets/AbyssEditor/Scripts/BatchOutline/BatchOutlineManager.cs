using System.Collections.Generic;
using AbyssEditor.Scripts.VoxelTech;
using UnityEngine;

namespace AbyssEditor.Scripts.BatchOutline
{
    public class BatchOutlineManager : MonoBehaviour
    {
        public static BatchOutlineManager main;
        
        [SerializeField] private GameObject batchOutlinePrefab;
        
        private GameObject[] batchLoadOutlines;
        
        private void Awake()
        {
            main = this;
        }

        public void ResetLoadOutlines()
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
                outlines.Add(DrawBatchOutline(batchIndex));
            }
            batchLoadOutlines = outlines.ToArray();
        }
        
        private GameObject DrawBatchOutline(Vector3Int batchIndex)
        {
            GameObject outline = Instantiate(batchOutlinePrefab, batchIndex * VoxelWorld.BATCH_WIDTH, Quaternion.identity);

            GameObject cube = outline.transform.GetChild(0).gameObject;
            
            if (VoxelMetaspace.metaspace.TryGetVoxelMesh(batchIndex) != null)
            {
                //batch exists to make it red
                cube.GetComponent<MeshRenderer>().material.SetColor("_WireframeColor", Color.red );
            }
            else
            {
                //batch doesn't exist to make it purple
                cube.GetComponent<MeshRenderer>().material.SetColor("_WireframeColor", Color.rebeccaPurple );
            }
            
            return outline;
        }
    }
}
