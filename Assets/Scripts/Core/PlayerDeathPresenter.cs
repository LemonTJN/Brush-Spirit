using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BrushSpirit.Core
{
    /// <summary>玩家阵亡时短暂全屏提示再重载当前关（unscaled 时间）。</summary>
    public static class PlayerDeathPresenter
    {
        static Font BuiltinFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        public static IEnumerator PlayThenReload(string sceneName, float holdSeconds = 1.35f)
        {
            var root = new GameObject("PlayerDeathOverlay");
            var cv = root.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 350;
            root.AddComponent<GraphicRaycaster>();

            var dim = new GameObject("Dim");
            dim.transform.SetParent(root.transform, false);
            var dr = dim.AddComponent<RectTransform>();
            dr.anchorMin = Vector2.zero;
            dr.anchorMax = Vector2.one;
            dr.offsetMin = Vector2.zero;
            dr.offsetMax = Vector2.zero;
            var dimImg = dim.AddComponent<Image>();
            dimImg.color = new Color(0.04f, 0.04f, 0.06f, 0f);

            var textGo = new GameObject("Msg");
            textGo.transform.SetParent(root.transform, false);
            var tr = textGo.AddComponent<RectTransform>();
            tr.anchorMin = new Vector2(0.5f, 0.5f);
            tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.sizeDelta = new Vector2(640f, 80f);
            var txt = textGo.AddComponent<Text>();
            txt.font = BuiltinFont();
            txt.fontSize = 28;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.75f, 0.76f, 0.78f, 0f);
            txt.text = "墨色溃散……";

            float fadeIn = 0.5f;
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / fadeIn);
                dimImg.color = new Color(0.04f, 0.04f, 0.06f, 0.82f * u);
                var tc = txt.color;
                tc.a = u;
                txt.color = tc;
                yield return null;
            }

            dimImg.color = new Color(0.04f, 0.04f, 0.06f, 0.82f);
            txt.color = new Color(0.75f, 0.76f, 0.78f, 1f);

            yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, holdSeconds));

            SceneManager.LoadScene(string.IsNullOrEmpty(sceneName) ? SceneManager.GetActiveScene().name : sceneName);
        }
    }
}
