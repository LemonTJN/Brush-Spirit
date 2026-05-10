using UnityEngine;

namespace BrushSpirit.Core
{
    /// <summary>
    /// 运行时保证至少有一台可用的 MainCamera，避免出现 Game 视图 “No cameras rendering”
    /// （场景资源异常、相机被误关、或引擎未正确识别 MainCamera 时）。
    /// </summary>
    public static class MainCameraEnsure
    {
        public static void Ensure(Color background, float orthographicSize, bool orthographic = true)
        {
            var main = Camera.main;
            if (main != null && main.isActiveAndEnabled && main.gameObject.activeInHierarchy)
            {
                ApplySettings(main, background, orthographicSize, orthographic);
                return;
            }

            var all = Object.FindObjectsOfType<Camera>(true);
            foreach (var c in all)
            {
                if (!c.gameObject.CompareTag("MainCamera")) continue;
                c.gameObject.SetActive(true);
                c.enabled = true;
                ApplySettings(c, background, orthographicSize, orthographic);
                EnsureAudioListener(c.gameObject);
                return;
            }

            if (all.Length == 1)
            {
                var c = all[0];
                c.gameObject.tag = "MainCamera";
                c.gameObject.SetActive(true);
                c.enabled = true;
                ApplySettings(c, background, orthographicSize, orthographic);
                EnsureAudioListener(c.gameObject);
                return;
            }

            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            ApplySettings(cam, background, orthographicSize, orthographic);
            go.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            go.AddComponent<AudioListener>();
        }

        static void ApplySettings(Camera cam, Color background, float orthographicSize, bool orthographic)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.orthographic = orthographic;
            cam.orthographicSize = orthographicSize;
            cam.depth = -1;
        }

        static void EnsureAudioListener(GameObject cameraGo)
        {
            if (Object.FindObjectOfType<AudioListener>() == null)
                cameraGo.AddComponent<AudioListener>();
        }
    }
}
