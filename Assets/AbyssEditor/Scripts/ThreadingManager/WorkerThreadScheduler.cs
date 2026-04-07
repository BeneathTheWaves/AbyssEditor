using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AbyssEditor.Scripts.Octrees;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace AbyssEditor.Scripts.ThreadingManager
{
    public class WorkerThreadScheduler : IDisposable
    {
        public static WorkerThreadScheduler main;
        
        private readonly BlockingCollection<Action> taskQueue = new();
        private readonly Thread[] workers;
        public int workersCount => workers.Length;
        private bool threadsShouldRun = true;

        public WorkerThreadScheduler()
        {
            main = this;

            int worker_count = 6;/*SystemInfo.processorCount - 1*/;
            workers = new Thread[worker_count];
            
            for (int i = 0; i < worker_count; i++)
            {
                workers[i] = new Thread(WorkerLoop);
                workers[i].Start();
            }
        }

        public void ScheduleParallelManualLocking(Action syncTask)
        {
            if (syncTask == null) throw new ArgumentNullException(nameof(syncTask));
            taskQueue.Add(syncTask);
        }

        public Task ScheduleParallel(Action syncTask)
        {
            TaskCompletionSource<bool> tcs = new();
            if (syncTask == null) throw new ArgumentNullException(nameof(syncTask));
            taskQueue.Add(() =>
            {
                syncTask.Invoke();
                tcs.SetResult(true);
            });
            return tcs.Task;
        }
        
        public Task<T> ScheduleParallel<T>(Func<T> syncTask)
        {
            TaskCompletionSource<T> tcs = new();
            if (syncTask == null) throw new ArgumentNullException(nameof(syncTask));
            
            taskQueue.Add(() =>
            {
                T result = syncTask.Invoke();
                tcs.SetResult(result);
            });
            
            return tcs.Task;
        }
        
        
        //This is the separate thread,
        private void WorkerLoop()
        {
            Profiler.BeginThreadProfiling("AsyncMeshBuilders", "Worker");
            while (threadsShouldRun)
            {
                try
                {
                    //NOTE: this does not make it busy wait, it will be awoken when queue has something for it
                    Action taskToExecute = taskQueue.Take();
                    taskToExecute.Invoke();
                }
                catch (InvalidOperationException)
                {
                    // happens if the thread is started with no value for Take()
                    // when we are trying to dispose the thread
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
        
        public void Dispose()
        {
            threadsShouldRun = false;
            taskQueue.CompleteAdding();
            
            foreach (var worker in workers)
            {
                if (worker != null && worker.IsAlive)
                {
                    worker.Join();
                }
            }
        }
    }
}
