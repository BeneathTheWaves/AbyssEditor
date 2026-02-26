using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

namespace AbyssEditor.Scripts.ThreadingManager
{
    public class AsyncThreadScheduler : IDisposable
    {
        public static AsyncThreadScheduler main;
        
        private readonly BlockingCollection<Action> taskQueue = new();
        private readonly Thread[] workers;
        public int workersCount => workers.Length;
        private bool threadsShouldRun = true;
        private IDisposable disposableImplementation;

        public AsyncThreadScheduler()
        {
            main = this;
            
            int worker_count = SystemInfo.processorCount - 1;
            workers = new Thread[worker_count];
            
            for (int i = 0; i < worker_count; i++)
            {
                workers[i] = new Thread(WorkerLoop);
                workers[i].Start();
            }
        }
        
        public void Enqueue(Action syncTask)
        {
            if (syncTask == null) throw new ArgumentNullException(nameof(syncTask));
            taskQueue.Add(syncTask);
        }
        
        //This is the seperate thread,
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
