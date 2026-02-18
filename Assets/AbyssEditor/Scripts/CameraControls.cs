using AbyssEditor.Scripts.CursorTools;
using AbyssEditor.Scripts.CursorTools.Brush;
using AbyssEditor.Scripts.InputMaps;
using AbyssEditor.Scripts.VoxelTech;
using UnityEngine;
using UnityEngine.EventSystems;
namespace AbyssEditor.Scripts
{
    public class CameraControls : MonoBehaviour
    {
        public static CameraControls main;
        
        public AbyssEditorInput.FreeCamActions input;
        
        public bool moveLock = true;

        public bool dragging;
        private BrushTool brushTool;

        public float acceleration = 50; // how fast you accelerate
        public float accSprintMultiplier = 4; // how much faster you go when "sprinting"
        public float lookSensitivity = 1; // mouse look sensitivity
        public float dampingCoefficient = 5; // how quickly you break to a halt after you stop your input
        
        Vector3 velocity; // current velocity

        static bool HoldingRMB
        {
            get => Cursor.lockState == CursorLockMode.Locked;
            set
            {
                Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !value;
            }
        }

        private void Awake()
        {
            main = this;
            input = new AbyssEditorInput().FreeCam;
        }
        
        private void Start()
        {
            input.Enable();
            brushTool = CursorToolManager.main.brushTool;
        }

        /// <summary>
        /// NOTE: Requires start batch to be the lowest batch and end batch to be the highest for proper positioning
        /// </summary>
        public void OnRegionLoad(Vector3Int startBatch, Vector3Int endBatch)
        {
            moveLock = false;
            PoseCamera(startBatch, endBatch);
        }

        private void PoseCamera(Vector3 startBatch, Vector3 endBatch)
        {
            Vector3 regionCenter = (startBatch + endBatch) * (0.5f * VoxelWorld.BATCH_WIDTH);
            
            transform.position = (endBatch + Vector3.one + Vector3.up) * VoxelWorld.BATCH_WIDTH;
            transform.LookAt(regionCenter);
        }

        void OnDisable() => HoldingRMB = false;

        private void Update()
        {
            if (moveLock)
                return;

            // Input
            if (HoldingRMB)
                UpdateInput();
            
            HoldingRMB = input.ActivateFreeCam.IsPressed();

            // Physics
            velocity = Vector3.Lerp(velocity, Vector3.zero, dampingCoefficient * Time.deltaTime);
            transform.position += velocity * Time.deltaTime;
        }

        void UpdateInput()
        {
            // Rotation
            Vector2 mouseDelta = lookSensitivity * input.Look.ReadValue<Vector2>();
            mouseDelta.y *= -1f;
            Quaternion rotation = transform.rotation;
            Quaternion horiz = Quaternion.AngleAxis(mouseDelta.x, Vector3.up);
            Quaternion vert = Quaternion.AngleAxis(mouseDelta.y, Vector3.right);
            

            //Apply
            transform.rotation = horiz * rotation * vert;
            velocity += GetAccelerationVector() * Time.deltaTime;// Position
        }

        Vector3 GetAccelerationVector()
        {
            Vector3 moveInput = default;

            Vector2 wasd = input.Move.ReadValue<Vector2>();
            
            moveInput.x = wasd.x;
            moveInput.z = wasd.y;
            
            moveInput.y = input.UpDown.ReadValue<float>();
            
            Vector3 direction = transform.TransformVector(moveInput.normalized);

            if (input.SpeedUp.IsPressed())
                return direction * (acceleration * accSprintMultiplier); // "sprinting"
            return direction * acceleration; // "walking"
        }
    }
}
