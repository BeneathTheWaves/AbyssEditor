using System.Collections.Generic;
using AbyssEditor.Scripts.UI;
using UnityEngine;

namespace AbyssEditor.Scripts.TaskSystem
{
    public class TaskManager : MonoBehaviour
    {
        public static TaskManager main;
        public Transform processDisplayHolder;
        
        private List<EditorProcessHandle> runningProcesses = new List<EditorProcessHandle>();

        [SerializeField] private GameObject taskStatusDisplayPrefab;

        private void Awake()
        {
            main = this;
        }
        
        public EditorProcessHandle GetEditorProcessHandle(int processTotalPhaseCount)
        {
            GameObject taskStatusDisplay = Instantiate(taskStatusDisplayPrefab, processDisplayHolder);
            EditorProcessHandle handle = new EditorProcessHandle(taskStatusDisplay.GetComponent<TaskStatusDisplay>(), processTotalPhaseCount);
            runningProcesses.Add(handle);
            return handle;
        }

        public void CompleteEditorProcess(EditorProcessHandle handle)
        {
            runningProcesses.Remove(handle);
        }
    }
}
