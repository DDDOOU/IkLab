using UnityEngine;

namespace IkLabUnity
{
    /// <summary>
    /// 平面演示用的正交相机：滚轮缩放、中键平移、F 重置视角。
    /// 始终正视 XY 平面（相机沿 +Z 方向观察）。
    /// </summary>
    public class PlanarCameraController : MonoBehaviour
    {
        public Vector3 homePosition = new Vector3(0f, 0f, -10f);
        public float homeOrthoSize = 7f;
        public float minOrthoSize = 1.5f;
        public float maxOrthoSize = 16f;
        public Rect hudRect = new Rect(0f, 0f, 430f, 566f); // HUD 区域，避免在面板上滚轮缩放

        private Camera cam;
        private bool panning;
        private Vector3 panMouseStart;

        private void Start()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            ResetView();
        }

        private void Update()
        {
            if (cam == null) return;

            Vector2 mouseGui = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            bool overHud = hudRect.Contains(mouseGui);

            float scroll = Input.mouseScrollDelta.y;
            if (!overHud && Mathf.Abs(scroll) > 0.01f)
            {
                cam.orthographic = true;
                cam.orthographicSize = Mathf.Clamp(
                    cam.orthographicSize * (1f - scroll * 0.1f), minOrthoSize, maxOrthoSize);
            }

            if (Input.GetMouseButtonDown(2))
            {
                panning = true;
                panMouseStart = Input.mousePosition;
            }
            if (panning && Input.GetMouseButton(2))
            {
                Vector3 delta = Input.mousePosition - panMouseStart;
                float worldPerPixel = cam.orthographicSize * 2f / Mathf.Max(1, Screen.height);
                transform.position -= new Vector3(delta.x * worldPerPixel, delta.y * worldPerPixel, 0f);
            }
            if (!Input.GetMouseButton(2)) panning = false;

            if (Input.GetKeyDown(KeyCode.F)) ResetView();
        }

        public void ResetView()
        {
            if (cam == null) return;
            cam.orthographic = true;
            cam.orthographicSize = homeOrthoSize;
            transform.position = homePosition;
            transform.rotation = Quaternion.identity;
        }
    }
}
