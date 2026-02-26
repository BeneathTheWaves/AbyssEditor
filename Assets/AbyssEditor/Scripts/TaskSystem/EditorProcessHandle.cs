using AbyssEditor.Scripts.UI;
using UnityEngine;

namespace AbyssEditor.Scripts.TaskSystem
{
    public class EditorProcessHandle
    {
        private TaskStatusDisplay displayObject;

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

        public void CompletePhase()
        {
            currentPhase++;
                
            if (currentPhase - 1 >= processPhases)
            {
                TaskManager.main.CompleteEditorProcess(this);
                GameObject.Destroy(displayObject.gameObject);
            }

            tasksCount = 0;
            tasksCompleted = 0;
        }
        
        /// <summary>
        /// Set process status
        /// </summary>
        /// <param name="status">text status</param>
        public void SetStatus(string status)
        {
            displayObject.SetStatusText(status);
        }

        private int tasksCount;
        private int tasksCompleted;
        
        public void SetTasksToCompleteForPhase(int taskCount)
        {
            tasksCount = taskCount;
        }

        public void IncrementTasksComplete()
        {
            tasksCompleted++;
            SetProgress((float) tasksCompleted / tasksCount);
            SetStatus("COMPLETED: " + tasksCompleted);
        }
    }
}
