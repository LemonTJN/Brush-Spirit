using BrushSpirit.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BrushSpirit
{
    public class MenuRuntimeBootstrap : MonoBehaviour
    {
        void Awake()
        {
            PlayerRunCarry.ClearRun();
            MainCameraEnsure.Ensure(new Color(0.12f, 0.13f, 0.15f), 5f);
            EnsureEventSystem();
            BuildMenu();
            PlayfieldBoundaryController.Ensure(false, -5);
        }

        static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        void BuildMenu()
        {
            var cam = Camera.main;
            if (cam != null)
                cam.backgroundColor = new Color(0.12f, 0.13f, 0.15f);

            var root = new GameObject("MenuCanvas");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            root.AddComponent<GraphicRaycaster>();

            CreateText(root.transform, "Title", "笔灵：绘卷世界", 40, new Vector2(0.5f, 0.78f), new Vector2(800f, 100f));

            var startBtn = CreateButton(root.transform, "StartButton", "开始", new Vector2(0.5f, 0.55f), new Vector2(220f, 52f));
            var selectPanel = CreatePanel(root.transform, "LevelSelect");

            startBtn.onClick.AddListener(() =>
            {
                selectPanel.SetActive(true);
                RefreshLevelSelect(selectPanel.transform);
            });

            CreateText(selectPanel.transform, "SelectTitle", "选择关卡", 30, new Vector2(0.5f, 0.72f), new Vector2(600f, 80f));

            int unlocked = GameSave.GetUnlockedLevel();
            CreateLevelButton(selectPanel.transform, "墨林", 1, unlocked >= 1, new Vector2(0.5f, 0.52f));
            CreateLevelButton(selectPanel.transform, "余烬谷（未解锁）", 2, unlocked >= 2, new Vector2(0.5f, 0.40f));
            CreateLevelButton(selectPanel.transform, "画心（未解锁）", 3, unlocked >= 3, new Vector2(0.5f, 0.28f));

            var back = CreateButton(selectPanel.transform, "BackButton", "返回", new Vector2(0.5f, 0.12f), new Vector2(160f, 44f));
            back.onClick.AddListener(() => selectPanel.SetActive(false));

            selectPanel.SetActive(false);
        }

        void RefreshLevelSelect(Transform panel)
        {
            int unlocked = GameSave.GetUnlockedLevel();
            var b1 = panel.Find("Level1Button")?.GetComponent<Button>();
            var b2 = panel.Find("Level2Button")?.GetComponent<Button>();
            var b3 = panel.Find("Level3Button")?.GetComponent<Button>();
            SetLevelButton(b1, unlocked >= 1, "墨林");
            SetLevelButton(b2, unlocked >= 2, "余烬谷");
            SetLevelButton(b3, unlocked >= 3, "画心");
        }

        static void SetLevelButton(Button btn, bool unlocked, string name)
        {
            if (btn == null) return;
            btn.interactable = unlocked;
            var txt = btn.GetComponentInChildren<Text>();
            if (txt != null)
                txt.text = unlocked ? name : name + "（未解锁）";
            btn.onClick.RemoveAllListeners();
            if (unlocked)
                btn.onClick.AddListener(() => SceneManager.LoadScene("InkForest_01"));
        }

        static GameObject CreatePanel(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.45f);
            return go;
        }

        static void CreateText(Transform parent, string name, string content, int fontSize, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = size;
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.92f, 0.94f, 0.9f);
            t.text = content;
        }

        static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.22f, 0.28f, 0.24f, 0.95f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            var tr = txtGo.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 22;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = label;
            return btn;
        }

        void CreateLevelButton(Transform parent, string label, int level, bool unlocked, Vector2 anchor)
        {
            string goName = level == 1 ? "Level1Button" : level == 2 ? "Level2Button" : "Level3Button";
            var go = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(360f, 48f);
            var img = go.AddComponent<Image>();
            img.color = unlocked ? new Color(0.2f, 0.32f, 0.24f, 0.95f) : new Color(0.12f, 0.12f, 0.12f, 0.8f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = unlocked;

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            var tr = txtGo.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 20;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = unlocked ? Color.white : new Color(0.55f, 0.55f, 0.55f);
            txt.text = unlocked ? label.Replace("（未解锁）", "") : label;

            btn.onClick.RemoveAllListeners();
            if (unlocked)
                btn.onClick.AddListener(() => SceneManager.LoadScene("InkForest_01"));
        }
    }
}
