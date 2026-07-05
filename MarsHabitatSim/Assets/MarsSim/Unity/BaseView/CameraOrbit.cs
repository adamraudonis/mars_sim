using UnityEngine;

namespace MarsSim.UnityApp.BaseView
{
    /// <summary>
    /// Orbit/pan/zoom camera. Left-drag orbits, right/middle-drag pans, scroll zooms.
    /// Optional slow auto-orbit for hyper-timelapse cinematics (toggled from the HUD).
    /// </summary>
    public sealed class CameraOrbit : MonoBehaviour
    {
        public Vector3 Pivot = new(0, 4, 0);
        public float Distance = 320f;
        public float Yaw = 35f;
        public float Pitch = 32f;
        public bool AutoOrbit;

        private const float MinDistance = 15f, MaxDistance = 2500f;

        /// <summary>Set by the HUD each frame (UI Toolkit picking) so drags on panels don't move the camera.</summary>
        public static bool PointerOverUI;

        private void LateUpdate()
        {
            bool overUi = PointerOverUI;

            if (!overUi)
            {
                if (Input.GetMouseButton(0) && !Input.GetKey(KeyCode.LeftAlt))
                {
                    Yaw += Input.GetAxis("Mouse X") * 3.5f;
                    Pitch = Mathf.Clamp(Pitch - Input.GetAxis("Mouse Y") * 3.5f, 4f, 89f);
                }
                if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
                {
                    float k = Distance * 0.0022f;
                    var right = transform.right;
                    var fwd = Vector3.Cross(right, Vector3.up).normalized;
                    Pivot += (-right * Input.GetAxis("Mouse X") - fwd * Input.GetAxis("Mouse Y")) * k * 3f;
                    float r = Pivot.magnitude;
                    if (r > 1500f) Pivot *= 1500f / r;
                }
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.001f)
                    Distance = Mathf.Clamp(Distance * (1 - scroll * 0.9f), MinDistance, MaxDistance);
            }

            if (AutoOrbit) Yaw += Time.deltaTime * 1.6f;

            // Keyboard pan.
            var move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            if (move.sqrMagnitude > 0.001f)
            {
                var right = transform.right;
                var fwd = Vector3.Cross(right, Vector3.up).normalized;
                Pivot += (right * move.x + fwd * move.z) * Time.deltaTime * Distance * 0.4f;
            }

            var rot = Quaternion.Euler(Pitch, Yaw, 0);
            var pos = Pivot + rot * new Vector3(0, 0, -Distance);
            float ground = TerrainBuilder.HeightAt(pos.x, pos.z) + 2.5f;
            if (pos.y < ground) pos.y = ground;
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(Pivot - pos, Vector3.up));
        }
    }
}
