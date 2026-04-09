using System.Threading.Tasks;
using Unity.Jobs;

namespace AbyssEditor.Scripts.Util
{
    public static class AsyncUtils
    {
        public static async Task WaitForJob(JobHandle handle)
        {
            while (!handle.IsCompleted)
            {
                await Task.Yield();
            }
            handle.Complete();
        }
    }
}
