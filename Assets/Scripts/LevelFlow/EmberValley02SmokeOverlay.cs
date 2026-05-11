using UnityEngine;
using UnityEngine.UI;

namespace BrushSpirit.LevelFlow
{
    /// <summary>
    /// 焚道黑烟：全屏极低不透明度脉动，略压对比度（不挡操作）。
    /// </summary>
    public class EmberValley02SmokeOverlay : MonoBehaviour
    {
        Image _img;
        float _phase;

        void Awake()
        {
            var root = new GameObject("SmokeOverlayCanvas");
            root.transform.SetParent(transform, false);
            var cv = root.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 42;
            root.AddComponent<GraphicRaycaster>();

            var go = new GameObject("Smoke");
            go.transform.SetParent(root.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _img = go.AddComponent<Image>();
            _img.color = new Color(0.04f, 0.03f, 0.03f, 0.06f);
            _img.raycastTarget = false;
        }

        void Update()
        {
            if (_img == null) return;
            _phase += Time.deltaTime * 0.55f;
            float a = 0.035f + Mathf.Sin(_phase) * 0.028f;
            var c = _img.color;
            c.a = Mathf.Clamp(a, 0.02f, 0.09f);
            _img.color = c;
        }
    }
}
