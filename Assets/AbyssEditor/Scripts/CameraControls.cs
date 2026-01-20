using UnityEngine;
using UnityEngine.EventSystems;

namespace AbyssEditor
{
    public class CameraControls : MonoBehaviour
    {
        public bool moveLock = true;

        public bool dragging;
        private bool mouseOverUI;
        private Brush brush;

        public float acceleration = 50; // how fast you accelerate
        public float accSprintMultiplier = 4; // how much faster you go when "sprinting"
        public float lookSensitivity = 1; // mouse look sensitivity
        public float dampingCoefficient = 5; // how quickly you break to a halt after you stop your input

        Vector3 velocity; // current velocity

        static bool Focused
        {
            get => Cursor.lockState == CursorLockMode.Locked;
            set
            {
                Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = value == false;
            }
        }

        private void Start()
        {
            brush = GetComponent<Brush>();
        }

        private void OnRegionLoad()
        {
            moveLock = false;
            transform.parent.rotation = Quaternion.Euler(new Vector3(30, -135, 0));
            PoseCamera();
        }

        public void PoseCamera()
        {
            Camera.main.transform.parent.position = (VoxelWorld.endBatch - VoxelWorld.startBatch + Vector3.one + Vector3.up) * VoxelWorld.OCTREE_WIDTH * VoxelWorld.CONTAINERS_PER_SIDE / 2;
            Camera.main.transform.LookAt(Vector3.zero);
        }

        void OnDisable() => Focused = false;


        private void Update()
        {
            if (moveLock)
                return;

            mouseOverUI = IsMouseOverUI();

            // Input
            if (Focused)
                UpdateInput();
            Focused = Input.GetMouseButton(1);

            // Physics
            velocity = Vector3.Lerp(velocity, Vector3.zero, dampingCoefficient * Time.deltaTime);
            transform.position += velocity * Time.deltaTime;

            HandleBrushInput();
        }

        void UpdateInput()
        {
            // Position
            velocity += GetAccelerationVector() * Time.deltaTime;

            // Rotation
            Vector2 mouseDelta = lookSensitivity * new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
            Quaternion rotation = transform.rotation;
            Quaternion horiz = Quaternion.AngleAxis(mouseDelta.x, Vector3.up);
            Quaternion vert = Quaternion.AngleAxis(mouseDelta.y, Vector3.right);
            transform.rotation = horiz * rotation * vert;
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