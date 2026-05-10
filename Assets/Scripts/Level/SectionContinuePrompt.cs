using BrushSpirit.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BrushSpirit.LevelFlow
{
    /// <summary>非 Boss 段清怪后：提示并淡入淡出进入下一场景。</summary>
    public static class SectionContinuePrompt
    {
        public static void Show(string nextSceneName, string title, string subtitle)
        {
            if (string.IsNullOrEmpty(nextSceneName)) return;

            var root = new GameObject("SectionContinueUI");
            var cv = root.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 500;
            root.AddComponent<GraphicRaycaster>();

            var dim = new GameObject("Dim");
            dim.transform.SetParent(root.transform, false);
            var dimRt = dim.AddComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            var dimImg = dim.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.5f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(root.transform, false);
            var tr = titleGo.AddComponent<RectTransform>();
            tr.anchorMin = new Vector2(0.5f, 0.58f);
            tr.anchorMax = new Vector2(0.5f, 0.58f);
            tr.sizeDelta = new Vector2(720f, 64f);
            var titleTxt = titleGo.AddComponent<Text>();
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 28;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(0.9f, 0.95f, 0.88f);
            titleTxt.text = title;

            var subGo = new GameObject("Subtitle");
            subGo.transform.SetParent(root.transform, false);
            var sr = subGo.AddComponent<RectTransform>();
            sr.anchorMin = new Vector2(0.5f, 0.48f);
            sr.anchorMax = new Vector2(0.5f, 0.48f);
            sr.sizeDelta = new Vector2(680f, 40f);
            var subTxt = subGo.AddComponent<Text>();
            subTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subTxt.fontSize = 18;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = new Color(0.75f, 0.78f, 0.72f);
            subTxt.text = subtitle;

            var btnGo = new GameObject("ContinueBtn");
            btnGo.transform.SetParent(root.transform, false);
            var br = btnGo.AddComponent<RectTransform>();
            br.anchorMin = new Vector2(0.5f, 0.32f);
            br.anchorMax = new Vector2(0.5f, 0.32f);
            br.sizeDelta = new Vector2(240f, 50f);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.22f, 0.32f, 0.26f, 0.95f);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() =>
            {
                UnityEngine.Object.Destroy(root);
                SceneTransition.LoadScene(nextSceneName);
            });

            var label = new GameObject("Label");
            label.transform.SetParent(btnGo.transform, false);
            var lr = label.AddComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = Vector2.zero;
            lr.offsetMax = Vector2.zero;
            var lt = label.AddComponent<Text>();
            lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lt.fontSize = 20;
            lt.alignment = TextAnchor.MiddleCenter;
            lt.color = Color.white;
            lt.text = "继续前行";
        }
    }
}
