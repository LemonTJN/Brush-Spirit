using UnityEngine;

namespace BrushSpirit.Enemies
{
    /// <summary>敌人头顶简易血条（Sprite 缩放），随父物体移动。</summary>
    public class WorldHealthBar : MonoBehaviour
    {
        SpriteRenderer _fill;
        float _max;

        public static WorldHealthBar AddTo(Transform parent, float maxHp, float yOffset = 0.62f)
        {
            var root = new GameObject("WorldHP");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, yOffset, 0f);
            var whb = root.AddComponent<WorldHealthBar>();
            whb._max = maxHp;
            var spr = BrushSpirit.GameRuntimeBootstrap.CreatePlaceholderSprite();

            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(root.transform, false);
            var bg = bgGo.AddComponent<SpriteRenderer>();
            bg.sprite = spr;
            bg.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);
            bg.transform.localScale = new Vector3(1.08f, 0.18f, 1f);
            bg.sortingOrder = 45;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(root.transform, false);
            whb._fill = fillGo.AddComponent<SpriteRenderer>();
            whb._fill.sprite = spr;
            whb._fill.color = new Color(0.5f, 0.82f, 0.48f, 0.98f);
            whb._fill.sortingOrder = 46;
            whb._fill.transform.localScale = new Vector3(1f, 0.13f, 1f);
            whb.SetHp(maxHp);
            return whb;
        }

        public void SetHp(float current)
        {
            float r = _max > 0f ? Mathf.Clamp01(current / _max) : 0f;
            if (_fill != null)
                _fill.transform.localScale = new Vector3(Mathf.Max(0.02f, r), 0.13f, 1f);
        }
    }
}
