namespace AbyssEditor.Scripts.Mesh_Gen.Datas
{
    public class QuadFaceGroup
    {
        public readonly QuadFace[] faces;
        public int faceCount;

        public QuadFaceGroup(int faceCountCapacity)
        {
            faces = new QuadFace[faceCountCapacity];
            faceCount = 0;
        }

        public void AddFace(ref QuadFace face)
        {
            faces[faceCount] = face;
            faceCount++;
        }
            

        public void ResetDataNoAlloc()
        {
            faceCount = 0;
        }
    }
}
