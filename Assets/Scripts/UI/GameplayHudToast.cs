using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BrushSpirit.UI
{
    /// <summary>屏幕下方短暂提示（不受 timeScale 影响），用于教学与波次提示。</summary>
    public static class GameplayHudToast
    {
        static Font BuiltinFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        public static Coroutine Show(MonoBehaviour host, string message, float visibleSeconds, int sortOrder = 180)
        {
            if (host == null || string.IsNullOrEmpty(message)) return null;
            return host.StartCoroutine(Run(host, message, visibleSeconds, sortOrder));
        }

        static IEnumerator Run(MonoBehaviour host, string message, float visibleSeconds, int sortOrder)
        {
            var root = new GameObject("HudToast");
            var cv = root.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = sortOrder;
            root.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            var pr = panel.AddComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.08f, 0.08f);
            pr.anchorMax = new Vector2(0.92f, 0.22f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.52f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(panel.transform, false);
            var tr = textGo.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(12f, 8f);
            tr.offsetMax = new Vector2(-12f, -8f);
            var txt = textGo.AddComponent<Text>();
            txt.font = BuiltinFont();
            txt.fontSize = 18;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.93f, 0.94f, 0.9f);
            txt.text = message;

            float fade = 0.35f;
            float t = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / fade);
                var c = bg.color;
                c.a = 0.52f * a;
                bg.color = c;
                var tc = txt.color;
                tc.a = a;
                txt.color = tc;
                yield return null;
            }

            bg.color = new Color(0f, 0f, 0f, 0.52f);
            txt.color = new Color(0.93f, 0.94f, 0.9f, 1f);

            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, visibleSeconds));

            t = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(t / fade);
                var c = bg.color;
                c.a = 0.52f * a;
                bg.color = c;
                var tc = txt.color;
                tc.a = a;
                txt.color = tc;
                yield return null;
            }

            Object.Destroy(root);
        }
    }
}
