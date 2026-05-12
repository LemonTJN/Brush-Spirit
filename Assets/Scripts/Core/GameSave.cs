using UnityEngine;

namespace BrushSpirit.Core
{
    public static class GameSave
    {
        const string UnlockedLevelKey = "BrushSpirit_UnlockedLevel";

        /// <summary>已解锁的最高关卡序号（1=墨林，2=余烬谷，3=画心，4=绘心通关）。默认 1。</summary>
        public static int GetUnlockedLevel()
        {
            return PlayerPrefs.GetInt(UnlockedLevelKey, 1);
        }

        public static void UnlockLevel(int levelIndex1Based)
        {
            int current = GetUnlockedLevel();
            if (levelIndex1Based > current)
            {
                PlayerPrefs.SetInt(UnlockedLevelKey, levelIndex1Based);
                PlayerPrefs.Save();
            }
        }
    }
}
