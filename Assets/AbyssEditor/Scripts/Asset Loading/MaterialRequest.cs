using UnityEngine;

namespace AbyssEditor.Scripts.Asset_Loading
{
    public class MaterialRequest : CustomYieldInstruction
    {
        public float Progress { get; private set; }
        public string Status { get; private set; }
        public bool IsDone { get; private set; }

        public override bool keepWaiting => !IsDone;

        public void SetProgress(float value)
        {
            Progress = Mathf.Clamp01(value);
        }
        public void SetStatus(string status)
        {
            Status = status;
        }

        public void Complete()
        {
            Progress = 1f;
            IsDone = true;
        } 
    }
}