using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BrushSpirit.UI
{
    /// <summary>全屏居中卷首语（使用 unscaled 时间，配合 timeScale=0）。</summary>
    public static class GameplayProloguePanel
    {
        static Font BuiltinFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        /// <summary>显示一行正文若干秒（实时），结束后销毁面板。</summary>
        public static IEnumerator ShowLine(string line, float seconds, int sortOrder = 220)
        {
            var root = new GameObject("PrologueLine");
            var cv = root.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = sortOrder;
            root.AddComponent<GraphicRaycaster>();

            var dim = new GameObject("Dim");
            dim.transform.SetParent(root.transform, false);
            var dr = dim.AddComponent<RectTransform>();
            dr.anchorMin = Vector2.zero;
            dr.anchorMax = Vector2.one;
            dr.offsetMin = Vector2.zero;
            dr.offsetMax = Vector2.zero;
            var dimImg = dim.AddComponent<Image>();
            dimImg.color = new Color(0.02f, 0.03f, 0.04f, 0.78f);

            var textGo = new GameObject("Line");
            textGo.transform.SetParent(root.transform, false);
            var tr = textGo.AddComponent<RectTransform>();
            tr.anchorMin = new Vector2(0.5f, 0.5f);
            tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.sizeDelta = new Vector2(880f, 200f);
            var txt = textGo.AddComponent<Text>();
            txt.font = BuiltinFont();
            txt.fontSize = 24;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.88f, 0.9f, 0.85f);
            txt.text = line;

            float inDur = 0.45f;
            float t = 0f;
            while (t < inDur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / inDur);
                var c = dimImg.color;
                c.a = 0.78f * u;
                dimImg.color = c;
                var tc = txt.color;
                tc.a = u;
                txt.color = tc;
                yield return null;
            }

            dimImg.color = new Color(0.02f, 0.03f, 0.04f, 0.78f);
            txt.color = new Color(0.88f, 0.9f, 0.85f, 1f);

            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, seconds));

            float outDur = 0.4f;
            t = 0f;
            while (t < outDur)
            {
                t += Time.unscaledDeltaTime;
                float u = 1f - Mathf.Clamp01(t / outDur);
                var c = dimImg.color;
                c.a = 0.78f * u;
                dimImg.color = c;
                var tc = txt.color;
                tc.a = u;
                txt.color = tc;
                yield return null;
            }

            Object.Destroy(root);
        }
    }
}
