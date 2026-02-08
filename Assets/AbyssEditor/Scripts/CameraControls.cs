using AbyssEditor.Scripts.VoxelTech;
using UnityEngine;
using UnityEngine.EventSystems;
namespace AbyssEditor.Scripts
{
    public class CameraControls : MonoBehaviour
    {
        public static CameraControls main;
        
        public bool moveLock = true;

        public bool dragging;
        private bool mouseOverUI;
        private Brush brush;

        public float acceleration = 50; // how fast you accelerate
        public float accSprintMultiplier = 4; // how much faster you go when "sprinting"
        public float lookSensitivity = 1; // mouse look sensitivity
        public float dampingCoefficient = 5; // how quickly you break to a halt after you stop your input
        
        Vector3 velocity; // current velocity

#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        public static bool mouseWarpedToCenter;//On linux the mouse only warps on the first move of the mouse, this can cause a big jump in camera movement when pressing right click
#endif
        
        static bool HoldingRMB
        {
            get => Cursor.lockState == CursorLockMode.Locked;
            set
            {
                Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = value == false;
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
                if (!value)
                {
                    mouseWarpedToCenter = false;//reset
                }
#endif
            }
        }

        private void Awake()
        {
            main = this;
        }
        
        private void Start()
        {
            brush = GetComponent<Brush>();
        }

        public void OnRegionLoad(Vector3Int startBatch, Vector3Int endBatch)
        {
            moveLock = false;
            transform.parent.rotation = Quaternion.Euler(new Vector3(30, -135, 0));
            PoseCamera(startBatch, endBatch);
        }

        public void PoseCamera(Vector3 startBatch, Vector3 endBatch)
        {
            Vector3 regionCenter = (startBatch + endBatch) * 0.5f;
            
            Camera.main.transform.parent.position = (regionCenter + Vector3.one + Vector3.up) * VoxelWorld.BATCH_WIDTH;
            Camera.main.transform.LookAt(regionCenter);
        }

        void OnDisable() => HoldingRMB = false;


        private void Update()
        {
            if (moveLock)
                return;

            mouseOverUI = IsMouseOverUI();

            // Input
            if (HoldingRMB)
                UpdateInput();
            
            HoldingRMB = Input.GetMouseButton(1);

            // Physics
            velocity = Vector3.Lerp(velocity, Vector3.zero, dampingCoefficient * Time.deltaTime);
            transform.position += velocity * Time.deltaTime;

            HandleBrushInput();
        }

        void UpdateInput()
        {
            // Rotation
            Vector2 mouseDelta = lookSensitivity * new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
            Quaternion rotation = transform.rotation;
            Quaternion horiz = Quaternion.AngleAxis(mouseDelta.x, Vector3.up);
            Quaternion vert = Quaternion.AngleAxis(mouseDelta.y, Vector3.right);
            
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            //On linux, we need to skip the first frame of mouse movement as it takes 1 set it to the center
            if (!mouseWarpedToCenter && mouseDelta.magnitude != 0)
            {
                mouseWarpedToCenter = true;
                return;
            }
#endif

            //Apply
            transform.rotation = horiz * rotation * vert;
            velocity += GetAccelerationVector() * Time.deltaTime;// Position
        }

        Vector3 GetAccelerationVector()
        {
            Vector3 moveInput = default;

            void AddMovement(KeyCode key, Vector3 dir)
            {
                if (Input.GetKey(key))
                    moveInput += dir;
            }

            AddMovement(KeyCode.W, Vector3.forward);
            AddMovement(KeyCode.S, Vector3.back);
            AddMovement(KeyCode.D, Vector3.right);
            AddMovement(KeyCode.A, Vector3.left);
            AddMovement(KeyCode.Space, Vector3.up);
            AddMovement(KeyCode.LeftControl, Vector3.down);
            Vector3 direction = transform.TransformVector(moveInput.normalized);

            if (Input.GetKey(KeyCode.LeftShift))
                return direction * (acceleration * accSprintMultiplier); // "sprinting"
            return direction * acceleration; // "walking"
        }

        private void HandleBrushInput()
        {
            if (brush.enabled)
            {
                if (mouseOverUI)
                {
                    brush.DisableBrushGizmo();
                }
                else
                {
                    brush.BrushAction(Input.GetMouseButton(0));
                }
            }
        }


        private bool IsMouseOverUI() => EventSystem.current.IsPointerOverGameObject();
    }
}