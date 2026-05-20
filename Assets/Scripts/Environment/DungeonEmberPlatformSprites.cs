using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrushSpirit.Environment
{
    /// <summary>
    /// Dungeon 2D Hand Painted 地面砖 / 岩石地（Resources/Environment/DungeonEmberPlatforms）。
    /// </summary>
    public static class DungeonEmberPlatformSprites
    {
        const string ResourcesPath = "Environment/DungeonEmberPlatforms";
        const string BackgroundResourcesPath = "Environment/DungeonEmberBackground";

        static Sprite[] _platformSprites;
        static Sprite _backgroundSprite;
        static bool _loadAttempted;
        static bool _backgroundLoadAttempted;

        public static bool HasSprites => _platformSprites != null && _platformSprites.Length > 0;

        public static bool IsEmberValleyScene(string sceneName)
        {
            return sceneName == "EmberValley_01" || sceneName == "EmberValley_02" || sceneName == "EmberValley_03";
        }

        public static void EnsureBackgroundLoaded()
        {
            if (_backgroundLoadAttempted) return;
            _backgroundLoadAttempted = true;

            _backgroundSprite = Resources.Load<Sprite>(BackgroundResourcesPath + "/Background");
            if (_backgroundSprite == null)
            {
                var all = Resources.LoadAll<Sprite>(BackgroundResourcesPath);
                if (all != null && all.Length > 0)
                    _backgroundSprite = all[0];
            }

            if (_backgroundSprite == null)
            {
                Debug.LogWarning(
                    "[DungeonEmberPlatformSprites] 未找到焚道背景，请确认 Resources/Environment/DungeonEmberBackground/Background.png 存在。");
            }
        }

        public static bool TryGetBackground(out Sprite sprite)
        {
            EnsureBackgroundLoaded();
            sprite = _backgroundSprite;
            return sprite != null;
        }

        public static void EnsureLoaded()
        {
            if (_loadAttempted) return;
            _loadAttempted = true;

            var loaded = Resources.LoadAll<Sprite>(ResourcesPath);
            if (loaded == null || loaded.Length == 0)
            {
                Debug.LogWarning(
                    "[DungeonEmberPlatformSprites] 未找到地面贴图，请确认已导入 Dungeon 包且 Resources/Environment/DungeonEmberPlatforms 下有 Ground / BricksFill / RockFill。");
                _platformSprites = Array.Empty<Sprite>();
                return;
            }

            var list = new List<Sprite>(loaded.Length);
            foreach (var s in loaded)
            {
                if (s != null && IsRockOrBrickGroundSprite(s.name))
                    list.Add(s);
            }

            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            _platformSprites = list.ToArray();

            if (_platformSprites.Length == 0)
            {
                Debug.LogWarning(
                    "[DungeonEmberPlatformSprites] 已加载图集但未筛到砖/岩地面 Sprite，请检查 Ground.png 是否为 Multiple 切片。");
            }
        }

        static bool IsRockOrBrickGroundSprite(string spriteName)
        {
            return spriteName.IndexOf("brick", StringComparison.OrdinalIgnoreCase) >= 0
                   || spriteName.IndexOf("rock", StringComparison.OrdinalIgnoreCase) >= 0
                   || spriteName.IndexOf("fill", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool TryPickRandom(out Sprite sprite)
        {
            EnsureLoaded();
            if (!HasSprites)
            {
                sprite = null;
                return false;
            }

            sprite = _platformSprites[UnityEngine.Random.Range(0, _platformSprites.Length)];
            return true;
        }

        public static Vector3 ScaleToWorldSize(Sprite sprite, float worldWidth, float worldHeight) =>
            SunsetMagicFloorSprites.ScaleToWorldSize(sprite, worldWidth, worldHeight);
    }
}
