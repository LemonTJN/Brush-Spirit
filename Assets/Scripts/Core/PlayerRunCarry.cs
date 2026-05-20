using BrushSpirit.Items;
using BrushSpirit.Player;
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
            // 即便 Instance 已为 null（首次进游戏 / 上次清理过），仍需要重置全局解锁与掉落计数
            if (Instance != null)
            {
                Destroy(Instance.gameObject);
                Instance = null;
            }
            PlayerCombat.ResetUnlocks();
            WeaponDropDirector.Reset();
        }
    }
}
