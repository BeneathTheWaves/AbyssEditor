namespace Editor
{
// BarycentricBaker.cs
// Place anywhere under an Editor/ folder.
//
// Automatically bakes barycentric coordinates into UV2 (TEXCOORD1) for every
// mesh processed by Unity's import pipeline that has the name suffix "_Wire"
// (e.g. "Cube_Wire.fbx"). Remove the name filter if you want it applied to all meshes.
//
// For quad meshes (_QUADS_ON):
//   Each quad is two triangles sharing a diagonal edge. We detect the diagonal by
//   finding the longest edge in the triangle, then encode its opposite vertex index
//   (1, 2, or 3) into the w component of the barycentric UV4.
//
// UV layout written to uv2 (Vector4):
//   xyz = barycentric coordinate for this vertex within its triangle
//   w   = 0 (regular vertex) | 1/2/3 (diagonal vertex, component index 1-based)
    #if UNITY_EDITOR
    using UnityEngine;
    using UnityEditor;

    public static class BarycentricBaker
    {
        private static void BakeBarycentrics(Mesh mesh)
        {
            int[] tris     = mesh.triangles;
            Vector3[] verts = mesh.vertices;
            int triCount   = tris.Length / 3;

            // We must un-share vertices so each triangle corner has a unique entry.
            Vector3[]  newVerts = new Vector3[tris.Length];
            Vector2[]  newUV    = mesh.uv.Length == verts.Length
                                  ? new Vector2[tris.Length] : null;
            Vector4[]  bary     = new Vector4[tris.Length];

            Vector3[] baryBasis = { new Vector3(1,0,0), new Vector3(0,1,0), new Vector3(0,0,1) };

            for (int t = 0; t < triCount; t++)
            {
                int i0 = tris[t * 3 + 0];
                int i1 = tris[t * 3 + 1];
                int i2 = tris[t * 3 + 2];

                newVerts[t*3+0] = verts[i0];
                newVerts[t*3+1] = verts[i1];
                newVerts[t*3+2] = verts[i2];

                if (newUV != null)
                {
                    var srcUV = mesh.uv;
                    newUV[t*3+0] = srcUV[i0];
                    newUV[t*3+1] = srcUV[i1];
                    newUV[t*3+2] = srcUV[i2];
                }

                // Detect the diagonal (longest edge) for quad suppression.
                // Edge opposite vertex k has length dist(v[(k+1)%3], v[(k+2)%3]).
                float e0 = Vector3.Distance(newVerts[t*3+1], newVerts[t*3+2]);
                float e1 = Vector3.Distance(newVerts[t*3+0], newVerts[t*3+2]);
                float e2 = Vector3.Distance(newVerts[t*3+0], newVerts[t*3+1]);

                int diagOpposite = -1; // vertex index (0,1,2) opposite the diagonal edge
                if (e0 > e1 && e0 > e2) diagOpposite = 0;
                else if (e1 > e0 && e1 > e2) diagOpposite = 1;
                else if (e2 > e0 && e2 > e1) diagOpposite = 2;

                for (int k = 0; k < 3; k++)
                {
                    Vector3 b = baryBasis[k];
                    float w = (diagOpposite >= 0) ? (diagOpposite + 1) : 0;// 1-based index or 0
                    bary[t*3+k] = new Vector4(b.x, b.y, b.z, w);
                }
            }

            // Rebuild the mesh with un-shared vertices.
            mesh.Clear();
            mesh.vertices = newVerts;
            if (newUV != null) mesh.uv = newUV;

            int[] newTris = new int[tris.Length];
            for (int i = 0; i < tris.Length; i++) newTris[i] = i;
            mesh.triangles = newTris;

            mesh.SetUVs(1, bary);   // TEXCOORD1 in shader
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Debug.Log($"[BarycentricBaker] Baked {triCount} triangles on '{mesh.name}'");
        }
        
        [MenuItem("Tools/Bake WireFrame Mesh/From Selected MeshFilter")]
        static void BakeSelected()
        {
            var mf = Selection.activeGameObject?.GetComponent<MeshFilter>();
            if (mf == null)
            {
                EditorUtility.DisplayDialog("WireFrame Mesh", "Select a GameObject with a MeshFilter first.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "Save WireFrame Mesh", mf.sharedMesh.name + "_WireFrame", "asset", "Save baked mesh as...");
            if (string.IsNullOrEmpty(path)) return;

            var mesh = Object.Instantiate(mf.sharedMesh);
            BakeBarycentrics(mesh);
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();

            mf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            Debug.Log($"WireFrame mesh saved to {path} and assigned to {mf.gameObject.name}");
        }
    }
    
    #endif
}
