using BrushSpirit.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BrushSpirit
{
    public class MenuRuntimeBootstrap : MonoBehaviour
    {
        static readonly Color BgTop = new Color(0.07f, 0.09f, 0.13f);
        static readonly Color BgBottom = new Color(0.13f, 0.15f, 0.19f);
        static readonly Color TitleColor = new Color(0.90f, 0.84f, 0.66f);
        static readonly Color SubtitleColor = new Color(0.52f, 0.58f, 0.54f);
        static readonly Color BodyTextColor = new Color(0.88f, 0.90f, 0.86f);
        static readonly Color DividerColor = new Color(0.45f, 0.52f, 0.46f, 0.55f);
        static readonly Color BtnNormal = new Color(0.20f, 0.36f, 0.28f, 0.96f);
        static readonly Color BtnHighlight = new Color(0.28f, 0.48f, 0.36f, 1f);
        static readonly Color BtnPressed = new Color(0.14f, 0.26f, 0.20f, 1f);
        static readonly Color BtnDisabled = new Color(0.14f, 0.15f, 0.16f, 0.65f);
        static readonly Color OverlayDim = new Color(0.02f, 0.04f, 0.06f, 0.78f);
        static readonly Color CardBg = new Color(0.10f, 0.12f, 0.14f, 0.94f);
        static readonly Color CardBorder = new Color(0.38f, 0.48f, 0.40f, 0.85f);
        static readonly Color LevelUnlocked = new Color(0.18f, 0.30f, 0.24f, 0.95f);
        static readonly Color LevelLocked = new Color(0.11f, 0.12f, 0.13f, 0.80f);
        static readonly Color LevelDescColor = new Color(0.62f, 0.68f, 0.64f);

        static Font _regularFont;
        static Font _boldFont;
        static Sprite _roundedSprite;
        static Sprite _gradientSprite;
        static Sprite _whiteSprite;

        void Awake()
        {
            PlayerRunCarry.ClearRun();
            MainCameraEnsure.Ensure(BgTop, 5f);
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
                cam.backgroundColor = BgTop;

            var root = CreateCanvasRoot("MenuCanvas");
            CreateGradientBackground(root.transform);
            CreateVignette(root.transform);

            CreateMenuHeader(root.transform);

            var startBtn = CreateStyledButton(root.transform, "StartButton", "Start Journey",
                new Vector2(0.5f, 0f), new Vector2(480f, 100f), 44);
            var startRect = startBtn.GetComponent<RectTransform>();
            startRect.pivot = new Vector2(0.5f, 0.5f);
            startRect.anchoredPosition = new Vector2(0f, 168f);
            var selectPanel = CreateLevelSelectPanel(root.transform);

            startBtn.onClick.AddListener(() =>
            {
                selectPanel.SetActive(true);
                RefreshLevelSelect(selectPanel.transform);
            });

            CreateBodyText(root.transform, "Footer", "Brush Spirit", 24,
                new Vector2(0.5f, 0.06f), new Vector2(480f, 40f), new Color(0.35f, 0.38f, 0.36f, 0.7f));

            selectPanel.SetActive(false);
        }

        static GameObject CreateCanvasRoot(string name)
        {
            var root = new GameObject(name);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            root.AddComponent<GraphicRaycaster>();

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            return root;
        }

        static void CreateGradientBackground(Transform parent)
        {
            var go = new GameObject("Background");
            go.transform.SetParent(parent, false);
            StretchFull(go);
            var img = go.AddComponent<Image>();
            img.sprite = GetGradientSprite();
            img.type = Image.Type.Simple;
            img.color = Color.white;
            img.raycastTarget = false;
        }

        static void CreateVignette(Transform parent)
        {
            var go = new GameObject("Vignette");
            go.transform.SetParent(parent, false);
            StretchFull(go);
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.22f);
            img.raycastTarget = false;
        }

        static GameObject CreateLevelSelectPanel(Transform parent)
        {
            var overlay = new GameObject("LevelSelect");
            overlay.transform.SetParent(parent, false);
            StretchFull(overlay);
            var overlayImg = overlay.AddComponent<Image>();
            overlayImg.color = OverlayDim;

            var card = new GameObject("Card");
            card.transform.SetParent(overlay.transform, false);
            var cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(880f, 780f);

            var cardBg = card.AddComponent<Image>();
            cardBg.sprite = GetRoundedSprite();
            cardBg.type = Image.Type.Sliced;
            cardBg.color = CardBg;

            var border = new GameObject("Border");
            border.transform.SetParent(card.transform, false);
            var borderRect = border.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-3f, -3f);
            borderRect.offsetMax = new Vector2(3f, 3f);
            var borderImg = border.AddComponent<Image>();
            borderImg.sprite = GetRoundedSprite();
            borderImg.type = Image.Type.Sliced;
            borderImg.color = CardBorder;
            border.transform.SetAsFirstSibling();

            CreateCardHeader(card.transform);
            CreateDivider(card.transform, new Vector2(0.5f, 0.78f), new Vector2(460f, 3f));

            int unlocked = GameSave.GetUnlockedLevel();
            CreateLevelButton(card.transform, "Ink Forest", "Ink woods where the brush spirit awakens", 1, unlocked >= 1,
                new Vector2(0.5f, 0.68f));
            CreateLevelButton(card.transform, "Ember Valley", "Embers linger; color yet to return", 2, unlocked >= 2,
                new Vector2(0.5f, 0.50f));
            CreateLevelButton(card.transform, "Heart Realm", "Deep within the scroll, realm of the heart", 3, unlocked >= 3,
                new Vector2(0.5f, 0.32f));

            var back = CreateStyledButton(card.transform, "BackButton", "Back",
                new Vector2(0.5f, 0.16f), new Vector2(280f, 72f), 36);
            back.onClick.AddListener(() => overlay.SetActive(false));

            return overlay;
        }

        void RefreshLevelSelect(Transform panel)
        {
            int unlocked = GameSave.GetUnlockedLevel();
            var card = panel.Find("Card");
            if (card == null) return;

            SetLevelButton(card.Find("Level1Button")?.GetComponent<Button>(), unlocked >= 1,
                "Ink Forest", "Ink woods where the brush spirit awakens", 1);
            SetLevelButton(card.Find("Level2Button")?.GetComponent<Button>(), unlocked >= 2,
                "Ember Valley", "Embers linger; color yet to return", 2);
            SetLevelButton(card.Find("Level3Button")?.GetComponent<Button>(), unlocked >= 3,
                "Heart Realm", "Deep within the scroll, realm of the heart", 3);
        }

        static void SetLevelButton(Button btn, bool unlocked, string name, string desc, int levelIndex)
        {
            if (btn == null) return;
            btn.interactable = unlocked;
            ApplyButtonColors(btn, tintGraphic: false);

            var nameTxt = btn.transform.Find("Name")?.GetComponent<Text>();
            var descTxt = btn.transform.Find("Desc")?.GetComponent<Text>();
            if (nameTxt != null)
            {
                nameTxt.text = unlocked ? name : name + " · Locked";
                nameTxt.color = unlocked ? BodyTextColor : new Color(0.48f, 0.48f, 0.48f);
            }
            if (descTxt != null)
                descTxt.color = unlocked ? LevelDescColor : new Color(0.38f, 0.38f, 0.38f);

            var img = btn.GetComponent<Image>();
            if (img != null)
                img.color = unlocked ? LevelUnlocked : LevelLocked;

            btn.onClick.RemoveAllListeners();
            if (unlocked)
            {
                int lv = levelIndex;
                btn.onClick.AddListener(() =>
                {
                    string scene = lv == 1 ? "InkForest_01" : lv == 2 ? "EmberValley_01" : "HeartRealm_01";
                    SceneManager.LoadScene(scene);
                });
            }
        }

        static void CreateLevelButton(Transform parent, string name, string desc, int level, bool unlocked, Vector2 anchor)
        {
            string goName = level == 1 ? "Level1Button" : level == 2 ? "Level2Button" : "Level3Button";
            var go = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(660f, 130f);

            var img = go.AddComponent<Image>();
            img.sprite = GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = unlocked ? LevelUnlocked : LevelLocked;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = unlocked;
            ApplyButtonColors(btn, tintGraphic: false);

            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(go.transform, false);
            var nameRect = nameGo.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.55f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(24f, 0f);
            nameRect.offsetMax = new Vector2(-24f, -10f);
            var nameTxt = nameGo.AddComponent<Text>();
            InitText(nameTxt, unlocked ? name : name + " · Locked", 36, TextAnchor.MiddleLeft, bold: true);
            nameTxt.color = unlocked ? BodyTextColor : new Color(0.48f, 0.48f, 0.48f);

            var descGo = new GameObject("Desc");
            descGo.transform.SetParent(go.transform, false);
            var descRect = descGo.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0f, 0f);
            descRect.anchorMax = new Vector2(1f, 0.55f);
            descRect.offsetMin = new Vector2(24f, 10f);
            descRect.offsetMax = new Vector2(-24f, 0f);
            var descTxt = descGo.AddComponent<Text>();
            InitText(descTxt, desc, 26, TextAnchor.MiddleLeft);
            descTxt.color = unlocked ? LevelDescColor : new Color(0.38f, 0.38f, 0.38f);

            btn.onClick.RemoveAllListeners();
            if (unlocked)
            {
                int lv = level;
                btn.onClick.AddListener(() =>
                {
                    string scene = lv == 1 ? "InkForest_01" : lv == 2 ? "EmberValley_01" : "HeartRealm_01";
                    SceneManager.LoadScene(scene);
                });
            }
        }

        static Button CreateStyledButton(Transform parent, string name, string label, Vector2 anchor, Vector2 size, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.sprite = GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            ApplyButtonColors(btn, BtnNormal, tintGraphic: true);

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            StretchFull(txtGo);
            var txt = txtGo.AddComponent<Text>();
            InitText(txt, label, fontSize, TextAnchor.MiddleCenter);
            txt.color = BodyTextColor;

            return btn;
        }

        static void ApplyButtonColors(Button btn, Color normal = default, bool tintGraphic = true)
        {
            var colors = btn.colors;
            if (tintGraphic)
            {
                colors.normalColor = normal == default ? BtnNormal : normal;
                colors.highlightedColor = BtnHighlight;
                colors.pressedColor = BtnPressed;
                colors.selectedColor = BtnHighlight;
                colors.disabledColor = BtnDisabled;
            }
            else
            {
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.92f, 1f, 0.95f);
                colors.pressedColor = new Color(0.82f, 0.94f, 0.88f);
                colors.selectedColor = new Color(0.92f, 1f, 0.95f);
                colors.disabledColor = new Color(0.75f, 0.75f, 0.75f);
            }
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
        }

        static void CreateMenuHeader(Transform parent)
        {
            var header = new GameObject("Header");
            header.transform.SetParent(parent, false);
            var rect = header.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -56f);
            rect.sizeDelta = new Vector2(960f, 400f);

            var layout = header.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 24f;
            layout.padding = new RectOffset(0, 0, 0, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateLayoutText(header.transform, "Title", "Brush Spirit", 96, TitleColor, 128f, true);
            CreateLayoutText(header.transform, "Subtitle", "Scroll World", 56, SubtitleColor, 76f, false);
            CreateLayoutText(header.transform, "Tagline", "Wield the brush and restore color to the ink scroll", 36, SubtitleColor, 88f, false);
            CreateLayoutDivider(header.transform, 480f, 4f);
        }

        static void CreateCardHeader(Transform parent)
        {
            var header = new GameObject("CardHeader");
            header.transform.SetParent(parent, false);
            var rect = header.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -40f);
            rect.sizeDelta = new Vector2(720f, 120f);

            var layout = header.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateLayoutText(header.transform, "SelectTitle", "Select Level", 56, TitleColor, 88f, true);
        }

        static void CreateLayoutDivider(Transform parent, float width, float height)
        {
            var go = new GameObject("Divider");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            le.preferredWidth = width;

            var img = go.AddComponent<Image>();
            img.sprite = GetWhiteSprite();
            img.color = DividerColor;
            img.raycastTarget = false;
        }

        static void CreateLayoutText(Transform parent, string name, string content, int fontSize, Color color,
            float preferredHeight, bool withOutline)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.minHeight = preferredHeight;

            var t = go.AddComponent<Text>();
            InitText(t, content, fontSize, TextAnchor.MiddleCenter, withOutline);
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            if (withOutline)
            {
                var outline = go.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
                outline.effectDistance = new Vector2(2f, -2f);
            }
        }

        static void CreateBodyText(Transform parent, string name, string content, int fontSize, Vector2 anchor,
            Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = size;
            var t = go.AddComponent<Text>();
            InitText(t, content, fontSize, TextAnchor.MiddleCenter);
            t.color = color;
        }

        static void CreateDivider(Transform parent, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject("Divider");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = GetWhiteSprite();
            img.color = DividerColor;
            img.raycastTarget = false;
        }

        static void InitText(Text t, string content, int fontSize, TextAnchor alignment, bool bold = false)
        {
            if (bold && TryGetBoldFont(out var boldFont))
            {
                t.font = boldFont;
                t.fontStyle = FontStyle.Normal;
            }
            else
            {
                t.font = GetRegularFont();
                t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            }

            t.fontSize = fontSize;
            t.alignment = alignment;
            t.text = content;
            t.supportRichText = false;
        }

        static bool TryGetBoldFont(out Font font)
        {
            if (_boldFont != null)
            {
                font = _boldFont;
                return true;
            }

            _boldFont = Resources.Load<Font>("Fonts/Georgia-Bold");
            if (_boldFont != null)
            {
                font = _boldFont;
                return true;
            }

            font = null;
            return false;
        }

        static Font GetRegularFont()
        {
            if (_regularFont != null) return _regularFont;

            _regularFont = Resources.Load<Font>("Fonts/Georgia");
            if (_regularFont != null) return _regularFont;

            foreach (var osName in new[] { "Georgia", "Cambria", "Constantia", "Segoe UI", "Arial" })
            {
                var f = Font.CreateDynamicFontFromOSFont(osName, 48);
                if (f != null)
                {
                    _regularFont = f;
                    return _regularFont;
                }
            }

            _regularFont = Resources.GetBuiltinResource<Font>("Arial.ttf")
                ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _regularFont;
        }

        static void StretchFull(GameObject go)
        {
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color32(255, 255, 255, 255);
            var arr = new Color32[16];
            for (int i = 0; i < arr.Length; i++) arr[i] = px;
            tex.SetPixels32(arr);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
            return _whiteSprite;
        }

        static Sprite GetGradientSprite()
        {
            if (_gradientSprite != null) return _gradientSprite;
            const int h = 256;
            var tex = new Texture2D(4, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                var c = Color.Lerp(BgTop, BgBottom, t);
                for (int x = 0; x < 4; x++)
                    tex.SetPixel(x, y, c);
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            _gradientSprite = Sprite.Create(tex, new Rect(0, 0, 4, h), new Vector2(0.5f, 0.5f), 100f);
            return _gradientSprite;
        }

        static Sprite GetRoundedSprite()
        {
            if (_roundedSprite != null) return _roundedSprite;
            const int size = 64;
            const int radius = 14;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var clear = new Color32(0, 0, 0, 0);
            var white = new Color32(255, 255, 255, 255);
            float r = radius;
            float rSq = r * r;
            var center = new Vector2(size * 0.5f, size * 0.5f);
            float half = size * 0.5f - r;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - center.x) - half, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y - center.y) - half, 0f);
                    bool inside = dx * dx + dy * dy <= rSq;
                    tex.SetPixel(x, y, inside ? white : clear);
                }
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            var border = Vector4.one * (radius + 2);
            _roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, border);
            return _roundedSprite;
        }
    }
}
