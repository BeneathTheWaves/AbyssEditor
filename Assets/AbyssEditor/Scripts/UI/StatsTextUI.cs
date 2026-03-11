using System;
using AbyssEditor.Scripts.Essentials;
using AbyssEditor.Scripts.SaveSystem;
using AbyssEditor.Scripts.VoxelTech;
using TMPro;
using UnityEngine;


namespace AbyssEditor.Scripts.UI
{
    public class StatsTextUI : MonoBehaviour
    {
        public static StatsTextUI main;
        
        private TextMeshProUGUI statsText;

        private bool _updatedFirstTime;

        private void Awake()
        {
            main = this;
            statsText = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            statsText.enabled = false;
            ToggleVisibility(Preferences.data.enableStats);
        }

        public void ToggleVisibility(bool value)
        {
            gameObject.SetActive(value);
        }

        public void UpdateStats()
        {
            if (!gameObject.activeInHierarchy) return;
            if (!_updatedFirstTime)
            {
                statsText.enabled = true;
                _updatedFirstTime = true;
            }

            Vector3 cameraPos = CameraControls.main.transform.position;
            Vector3Int cameraBatch = new Vector3Int(
                Mathf.FloorToInt(cameraPos.x / VoxelWorld.BATCH_WIDTH),
                Mathf.FloorToInt(cameraPos.y / VoxelWorld.BATCH_WIDTH),
                Mathf.FloorToInt(cameraPos.z / VoxelWorld.BATCH_WIDTH)
            );
            //convert to Subnautica space Coords
            cameraPos += new Vector3(-2048, -3040, -2048);
            
            statsText.text = Language.main.Get("StatsText")
                .Replace("%CamBatch%", $"{cameraBatch}")
                .Replace("%CamPos%", $"({cameraPos.x:F0}, {cameraPos.y:F0}, {cameraPos.z:F0})")
                .Replace("%BatchCount%", $"{VoxelMetaspace.metaspace.meshes.Count}")
                .Replace("%MeshCount%", $"{VoxelMetaspace.metaspace.meshes.Count * 125}");
        }
    }
}
