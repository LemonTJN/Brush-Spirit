using BrushSpirit.Core;
using BrushSpirit.Player;
using BrushSpirit.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BrushSpirit.Items
{
    /// <summary>
    /// 武器形态解锁拾取物（剑 / 枪）。靠近后显示「按 U 拾取」提示，按下后才真正解锁并切换形态，
    /// 避免误触自动拾取。结构：
    ///   Root（碰撞 + 本脚本，不旋转）
    ///   ├─ Visual（SpriteRenderer + 旋转 + 首拾高亮）
    ///   └─ Prompt（TextMesh，靠近时显示）
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WeaponPickup : MonoBehaviour
    {
        public PlayerCombat.WeaponMode mode = PlayerCombat.WeaponMode.Sword;
        public float rotateSpeed = 95f;
        public KeyCode pickupKey = KeyCode.U;

        [Tooltip("拾取物贴图缩放（相对 PPU=100 的原图）。默认偏小以避免遮挡角色。")]
        public float visualScale = 0.32f;

        [Tooltip("贴近判定半径（玩家进入即弹出提示）")]
        public float pickupRadius = 0.85f;

        Transform _visualTransform;
        GameObject _promptLabel;
        PlayerCombat _nearbyPlayer;

        static Sprite s_swordSprite;
        static Sprite s_pistolSprite;
        static bool s_swordSpriteLoaded;
        static bool s_pistolSpriteLoaded;

        public static Sprite LoadSprite(PlayerCombat.WeaponMode m)
        {
            if (m == PlayerCombat.WeaponMode.Sword)
            {
                if (!s_swordSpriteLoaded)
                {
                    s_swordSprite = Resources.Load<Sprite>("Pickups/Pickup_Sword");
                    s_swordSpriteLoaded = true;
                }
                return s_swordSprite;
            }
            if (m == PlayerCombat.WeaponMode.Pistol)
            {
                if (!s_pistolSpriteLoaded)
                {
                    s_pistolSprite = Resources.Load<Sprite>("Pickups/Pickup_Pistol");
                    s_pistolSpriteLoaded = true;
                }
                return s_pistolSprite;
            }
            return null;
        }

        public static WeaponPickup SpawnAt(Vector3 position, PlayerCombat.WeaponMode m)
        {
            var root = new GameObject("WeaponPickup_" + m);
            root.transform.position = position;

            var col = root.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.85f;

            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(root.transform, false);
            var sr = visualGo.AddComponent<SpriteRenderer>();
            var sprite = LoadSprite(m);
            float scale = 0.32f;
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.color = Color.white;
            }
            else
            {
                sr.sprite = GameRuntimeBootstrap.CreatePlaceholderSprite();
                sr.color = m == PlayerCombat.WeaponMode.Sword
                    ? new Color(0.75f, 0.82f, 0.95f)
                    : new Color(0.88f, 0.76f, 0.42f);
                scale = 0.26f;
            }
            sr.sortingOrder = 22;
            visualGo.transform.localScale = new Vector3(scale, scale, 1f);
            visualGo.AddComponent<PickupFirstHighlight>();

            var wp = root.AddComponent<WeaponPickup>();
            wp.mode = m;
            wp.visualScale = scale;
            wp.pickupRadius = col.radius;
            wp._visualTransform = visualGo.transform;

            var clamp = root.AddComponent<ClampToWorldBounds2D>();
            clamp.halfWidthPad = 0.42f;
            clamp.halfHeightPad = 0.42f;
            clamp.skipClampWhenOutsideViewport = false;

            return wp;
        }

        void Awake()
        {
            EnsurePromptLabel();
            var col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = pickupRadius;
            }
        }

        void EnsurePromptLabel()
        {
            if (_promptLabel != null) return;

            // 世界坐标 UI Canvas：自带背景 + 描边，比 TextMesh 更清晰、不被场景背景干扰
            var canvasGo = new GameObject("PickupPromptCanvas");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            canvasGo.transform.localScale = new Vector3(0.006f, 0.006f, 1f);

            var cv = canvasGo.AddComponent<Canvas>();
            cv.renderMode = RenderMode.WorldSpace;
            cv.sortingOrder = 95;
            canvasGo.AddComponent<CanvasScaler>();

            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.sizeDelta = new Vector2(260f, 80f);
            bgRt.anchoredPosition = Vector2.zero;
            var bg = bgGo.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.62f);

            var txtGo = new GameObject("Txt");
            txtGo.transform.SetParent(bgGo.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(8f, 4f);
            txtRt.offsetMax = new Vector2(-8f, -4f);

            var txt = txtGo.AddComponent<Text>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.font = font;
            txt.fontSize = 40;
            txt.fontStyle = FontStyle.Bold;
            txt.color = new Color(1f, 0.95f, 0.66f);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.text = "按 U 拾取";

            var outline = txtGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);

            _promptLabel = canvasGo;
            canvasGo.SetActive(false);
        }

        void Update()
        {
            if (_visualTransform != null)
                _visualTransform.Rotate(0, 0, rotateSpeed * Time.deltaTime);

            if (_nearbyPlayer != null && Input.GetKeyDown(pickupKey))
                DoPickup(_nearbyPlayer);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            var combat = other.GetComponent<PlayerCombat>() ?? other.GetComponentInParent<PlayerCombat>();
            if (combat == null) return;
            _nearbyPlayer = combat;
            if (_promptLabel != null) _promptLabel.SetActive(true);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            var combat = other.GetComponent<PlayerCombat>() ?? other.GetComponentInParent<PlayerCombat>();
            if (combat == null || combat != _nearbyPlayer) return;
            _nearbyPlayer = null;
            if (_promptLabel != null) _promptLabel.SetActive(false);
        }

        void DoPickup(PlayerCombat combat)
        {
            switch (mode)
            {
                case PlayerCombat.WeaponMode.Sword:
                    if (!PlayerCombat.HasSword)
                    {
                        PlayerCombat.HasSword = true;
                        combat.SetWeapon(PlayerCombat.WeaponMode.Sword);
                        GameplayHudToast.Show(combat, "获得 剑！数字键 2 切换剑形态。", 3.2f, 190);
                    }
                    break;
                case PlayerCombat.WeaponMode.Pistol:
                    if (!PlayerCombat.HasPistol)
                    {
                        PlayerCombat.HasPistol = true;
                        combat.SetWeapon(PlayerCombat.WeaponMode.Pistol);
                        GameplayHudToast.Show(combat, "获得 枪！数字键 3 切换手枪形态（J 射击）。", 3.4f, 190);
                    }
                    break;
            }
            Destroy(gameObject);
        }
    }
}
