using BrushSpirit.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BrushSpirit.UI
{
    public class VictoryPanel : MonoBehaviour
    {
        public static VictoryPanel Instance { get; private set; }

        Canvas _canvas;
        GameObject _panelRoot;

        void Awake()
        {
            Instance = this;
            Build();
            Hide();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Build()
        {
            _canvas = gameObject.GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 400;
            if (gameObject.GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            _panelRoot = new GameObject("VictoryContent");
            _panelRoot.transform.SetParent(transform, false);
            var rect = _panelRoot.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = _panelRoot.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_panelRoot.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.62f);
            titleRect.anchorMax = new Vector2(0.5f, 0.62f);
            titleRect.sizeDelta = new Vector2(720f, 120f);
            var title = titleGo.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 36;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.85f, 0.95f, 0.8f);
            title.text = "墨林 · 颜色归来";

            var btnGo = new GameObject("BackButton");
            btnGo.transform.SetParent(_panelRoot.transform, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.35f);
            btnRect.anchorMax = new Vector2(0.5f, 0.35f);
            btnRect.sizeDelta = new Vector2(260f, 56f);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.25f, 0.35f, 0.28f, 0.95f);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() =>
            {
                PlayerRunCarry.ClearRun();
                SceneTransition.LoadScene("Menu");
            });

            var btnTextGo = new GameObject("Label");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRect = btnTextGo.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
            var btnText = btnTextGo.AddComponent<Text>();
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 22;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
            btnText.text = "返回关卡选择";
        }

        void Hide()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        public void Show()
        {
            GameSave.UnlockLevel(2);
            if (_panelRoot != null) _panelRoot.SetActive(true);
        }
    }
}
