using UnityEngine;

namespace BrushSpirit.Core
{
    /// <summary>墨林多段场景间保留同一玩家（血量、等级、装备）。返回菜单或死亡重开本段时清除。</summary>
    public class PlayerRunCarry : MonoBehaviour
    {
        public static PlayerRunCarry Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void ClearRun()
        {
            if (Instance == null) return;
            Destroy(Instance.gameObject);
            Instance = null;
        }
    }
}
