using Unity.Collections;
namespace AbyssEditor.Scripts.Util
{
    public static class NativeArrayExtensions
    {
        public static bool IsIdenticalTo(this NativeArray<byte> array1, NativeArray<byte> array2)
        {
            if (array1.Length != array2.Length) return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if(array1[i] != array2[i]) return false;
            }

            return true;
        }
    }
}
