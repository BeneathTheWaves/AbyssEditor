using System.Threading.Tasks;
using Unity.Jobs;

namespace AbyssEditor.Scripts.Utils
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
