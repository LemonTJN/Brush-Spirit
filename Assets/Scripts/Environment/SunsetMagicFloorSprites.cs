using System.Collections.Generic;
using UnityEngine;

namespace BrushSpirit.Environment
{
    /// <summary>
    /// Sunset Magic 包内 Floor 平台贴图（Resources/Environment/SunsetMagicFloor）。
    /// </summary>
    public static class SunsetMagicFloorSprites
    {
        const string ResourcesPath = "Environment/SunsetMagicFloor";

        static Sprite[] _sprites;
        static Sprite[] _upSprites;
        static bool _loadAttempted;

        public static bool HasSprites => _sprites != null && _sprites.Length > 0;
        public static bool HasUpSprites => GetUpSprites().Length > 0;

        public static bool IsInkForestScene(string sceneName)
        {
            return sceneName == "InkForest_01" || sceneName == "InkForest_02" || sceneName == "InkForest_03";
        }

        public static void EnsureLoaded()
        {
            if (_loadAttempted) return;
            _loadAttempted = true;

            var loaded = Resources.LoadAll<Sprite>(ResourcesPath);
            if (loaded == null || loaded.Length == 0)
            {
                Debug.LogWarning(
                    "[SunsetMagicFloorSprites] 未找到 Floor 贴图，请确认 Assets/Resources/Environment/SunsetMagicFloor 下已有 Floor_*.png。");
                _sprites = System.Array.Empty<Sprite>();
                return;
            }

            System.Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));
            _sprites = loaded;

            var ups = new List<Sprite>(3);
            foreach (var s in _sprites)
            {
                if (s != null && s.name.EndsWith("u"))
                    ups.Add(s);
            }

            _upSprites = ups.ToArray();
        }

        /// <summary>Floor_1u / Floor_2u / Floor_3u，按名称排序。</summary>
        public static Sprite[] GetUpSprites()
        {
            EnsureLoaded();
            return _upSprites ?? System.Array.Empty<Sprite>();
        }

        public static bool TryPickRandom(out Sprite sprite)
        {
            EnsureLoaded();
            if (!HasSprites)
            {
                sprite = null;
                return false;
            }

            sprite = _sprites[Random.Range(0, _sprites.Length)];
            return true;
        }

        /// <summary>
        /// 将任意 Sprite 缩放到与占位平台相同的世界尺寸（宽 w、高 h）。
        /// 占位块为 1×1 世界单位时等价于 localScale=(w,h,1)。
        /// </summary>
        public static Vector3 ScaleToWorldSize(Sprite sprite, float worldWidth, float worldHeight)
        {
            if (sprite == null)
                return new Vector3(worldWidth, worldHeight, 1f);

            Vector2 native = sprite.bounds.size;
            float sx = native.x > 0.0001f ? worldWidth / native.x : worldWidth;
            float sy = native.y > 0.0001f ? worldHeight / native.y : worldHeight;
            return new Vector3(sx, sy, 1f);
        }
    }
}
