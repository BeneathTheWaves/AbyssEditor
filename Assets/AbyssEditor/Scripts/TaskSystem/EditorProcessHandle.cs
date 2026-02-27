using AbyssEditor.Scripts.UI;
using NUnit.Framework;
using UnityEngine;

namespace AbyssEditor.Scripts.TaskSystem
{
    public class EditorProcessHandle
    {
        private readonly TaskStatusDisplay displayObject;

        private readonly int processPhases;
        private int currentPhase = 1;

        internal EditorProcessHandle(TaskStatusDisplay displayObject, int processPhases)
        {
            this.processPhases = processPhases;
            this.displayObject = displayObject;
        }

        /// <summary>
        /// Set this phases progress bar amount filled.
        /// </summary>
        /// <param name="progress">between 0.0 - 1.0</param>
        public void SetProgress(float progress)
        {
            float phaseOffset = (float)(currentPhase - 1) / processPhases;
            float phaseScale = 1f / processPhases;
            displayObject.SetProgress(phaseOffset + (progress * phaseScale));
        }

        /// <summary>
        /// Set process status
        /// </summary>
        /// <param name="status">text status</param>
        public void SetStatus(string status)
        {
            displayObject.SetStatusText(status);
        }
        
        /// <summary>
        /// MUST be explicitly called to advance to the next phase.
        /// totalTasks & tasksComplete will be reset to 0, if changed.
        /// </summary>
        public void CompletePhase()
        {
            currentPhase++;
                
            if (currentPhase - 1 >= processPhases)
            {
                TaskManager.main.CompleteEditorProcess(this);
                Object.Destroy(displayObject.gameObject);
            }

            tasksCount = 0;
            tasksCompleted = 0;
        }
        
        private int tasksCount;
        private int tasksCompleted;
        private string phaseTaskMessage;

        /// <summary>
        /// Initialize the current phases total tasks and status message.
        /// </summary>
        /// <param name="taskCount">The total "tasks" for this phase. A task is arbitrary and can be incremented to form a percentage with IncrementTasksComplete()</param>
        public void SetTasksToCompleteForPhase(int taskCount)
        {
            tasksCount = taskCount;
        }

        /// <summary>
        /// Sets the prefix for the current phase. Note, that SetTasksToCompleteForPhase should be called before this.
        /// </summary>
        /// <param name="phasePrefix">Prefix to be used when incrementing. the keys %completedTasks% and %totalTasks% will be
        /// replaced within the phasePrefix if found with their respective values</param>
        public void SetPhasePrefix(string phasePrefix)
        {
            if (tasksCount == 0)
            {
                Debug.LogError("Task count not set! you should not set phase prefix without setting the task count first");
                return;
            }
            
            phaseTaskMessage = phasePrefix.Replace("%totalTasks%", $"{tasksCount}");
            SetStatus(phaseTaskMessage.Replace("%completedTasks%", "0"));
        }

        /// <summary>
        /// Increment the amount of tasks completed for this phase, updating the progress and status accordingly.
        /// The phasePrefix set by SetTasksToCompleteForPhase() is used to set the status.
        /// The sequence %completedTasks% that *may* be in phasePrefix is replaced with the updated value when setting the status.
        /// </summary>
        public void IncrementTasksComplete(int i = 1)
        {
            tasksCompleted+= i;
            SetProgress((float) tasksCompleted / tasksCount);
            SetStatus(phaseTaskMessage.Replace("%completedTasks%", $"{tasksCompleted}"));
        }
    }
}
