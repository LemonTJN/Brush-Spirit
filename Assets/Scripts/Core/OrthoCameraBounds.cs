using UnityEngine;
using UnityEngine.UI;

namespace BrushSpirit.Core
{
    /// <summary>正交相机在 z=0 平面上的可视世界范围（与 Game 视口一致）。</summary>
    public static class OrthoCameraBounds
    {
        /// <summary>视口内可玩区相对边缘的内缩（世界单位），与挡板、钳制共用。</summary>
        public const float PlayInnerInset = 0.2f;

        /// <param name="innerPadding">从视口边缘向内收缩，用于可玩区与碰撞内沿</param>
        public static void GetWorldRect(Camera cam, float innerPadding, out float minX, out float maxX, out float minY,
            out float maxY)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector2 c = cam.transform.position;
            minX = c.x - halfW + innerPadding;
            maxX = c.x + halfW - innerPadding;
            minY = c.y - halfH + innerPadding;
            maxY = c.y + halfH - innerPadding;
        }
    }

    /// <summary>
    /// 屏幕四边半透明装饰 +（可选）世界空间 BoxCollider2D 挡板，与主相机正交视口对齐并随相机移动。
    /// （与 OrthoCameraBounds 同文件，避免工程未纳入单独脚本时出现 CS0103。）
    /// </summary>
    public class PlayfieldBoundaryController : MonoBehaviour
    {
        [SerializeField] float _wallThickness = 0.42f;
        [SerializeField] float _borderAlpha = 0.12f;
        [SerializeField] int _borderThicknessPx = 24;

        Transform _left, _right, _top, _bottom;
        BoxCollider2D _colL, _colR, _colT, _colB;
        Camera _cam;
        bool _physicsWalls;

        public static PlayfieldBoundaryController Ensure(bool physicsWalls, int overlaySortOrder)
        {
            var existing = Object.FindObjectOfType<PlayfieldBoundaryController>();
            if (existing != null)
            {
                existing._physicsWalls = physicsWalls;
                existing.EnableWallColliders(physicsWalls);
                return existing;
            }

            var go = new GameObject("PlayfieldBoundary");
            var c = go.AddComponent<PlayfieldBoundaryController>();
            c._physicsWalls = physicsWalls;
            c.Build(overlaySortOrder);
            return c;
        }

        void Build(int overlaySortOrder)
        {
            _cam = Camera.main;
            BuildWallsParent();
            BuildOverlay(overlaySortOrder);
            SyncWalls();
        }

        void BuildWallsParent()
        {
            var root = new GameObject("WorldWalls").transform;
            root.SetParent(transform, false);

            _left = WallChild(root, "WallL", out _colL);
            _right = WallChild(root, "WallR", out _colR);
            _top = WallChild(root, "WallT", out _colT);
            _bottom = WallChild(root, "WallB", out _colB);

            EnableWallColliders(_physicsWalls);
        }

        void EnableWallColliders(bool on)
        {
            if (_colL == null) return;
            _colL.enabled = on;
            _colR.enabled = on;
            _colT.enabled = on;
            _colB.enabled = on;
        }

        static Transform WallChild(Transform parent, string name, out BoxCollider2D col)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = 0;
            col = go.AddComponent<BoxCollider2D>();
            return go.transform;
        }

        void BuildOverlay(int sortOrder)
        {
            var root = new GameObject("ScreenBorderOverlay");
            root.transform.SetParent(transform, false);
            var cv = root.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = sortOrder;
            root.AddComponent<GraphicRaycaster>();
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            Color barColor = new Color(0.02f, 0.02f, 0.04f, _borderAlpha);
            AddBar(root.transform, "BarL", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(_borderThicknessPx, 0f), barColor);
            AddBar(root.transform, "BarR", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(_borderThicknessPx, 0f), barColor);
            AddBar(root.transform, "BarT", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, _borderThicknessPx), barColor);
            AddBar(root.transform, "BarB", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, _borderThicknessPx), barColor);
        }

        static void AddBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 sizeDelta, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = sizeDelta;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        void LateUpdate()
        {
            _cam = Camera.main;
            if (_cam == null || !_cam.orthographic) return;
            SyncWalls();
        }

        void SyncWalls()
        {
            if (!_physicsWalls) return;

            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;
            Vector3 c = _cam.transform.position;
            float z = 0f;
            float t = _wallThickness;
            float spanV = halfH * 2f + t * 2f;
            float spanH = halfW * 2f + t * 2f;

            PlaceWall(_left, new Vector3(c.x - halfW - t * 0.5f, c.y, z), new Vector2(t, spanV));
            PlaceWall(_right, new Vector3(c.x + halfW + t * 0.5f, c.y, z), new Vector2(t, spanV));
            PlaceWall(_top, new Vector3(c.x, c.y + halfH + t * 0.5f, z), new Vector2(spanH, t));
            PlaceWall(_bottom, new Vector3(c.x, c.y - halfH - t * 0.5f, z), new Vector2(spanH, t));
        }

        static void PlaceWall(Transform tr, Vector3 pos, Vector2 size)
        {
            tr.position = pos;
            var col = tr.GetComponent<BoxCollider2D>();
            col.size = size;
            col.offset = Vector2.zero;
        }
    }
}
