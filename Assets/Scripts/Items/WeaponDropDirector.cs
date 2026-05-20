using BrushSpirit.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrushSpirit.Items
{
    /// <summary>
    /// 武器形态掉落控制：
    ///   - 前 N 个怪是「教学期」纯拳头，不掉武器；
    ///   - 之后每次击杀按概率掉落，未掉时累积「保底进度」；
    ///   - 到保底阈值仍未掉则强制掉落，避免极端不出货；
    ///   - 已通过场景过关携带的武器在新场景不再重复掉落。
    /// 玩家死亡 / 返回菜单时由 <see cref="BrushSpirit.Core.PlayerRunCarry.ClearRun"/> 重置。
    /// </summary>
    public static class WeaponDropDirector
    {
        // ---- 剑（早期武器）----
        /// <summary>击杀达到该数之前不参与剑掉落（教学期）。</summary>
        public static int SwordStartKills = 3;
        /// <summary>从教学期结束起，每次击杀的剑掉落概率。</summary>
        public static float SwordDropChance = 0.30f;
        /// <summary>累计击杀达到该数后仍未掉落，则下一次击杀强制掉落（保底）。</summary>
        public static int SwordPityKills = 8;

        // ---- 枪（后期武器）----
        public static int PistolStartKills = 6;
        public static float PistolDropChance = 0.30f;
        public static int PistolPityKills = 12;

        static int s_kills;
        static bool s_swordDropped;
        static bool s_pistolDropped;
        static bool s_sceneHookInstalled;

        public static int TotalKills => s_kills;
        public static bool SwordDropped => s_swordDropped;
        public static bool PistolDropped => s_pistolDropped;

        public static void Reset()
        {
            s_kills = 0;
            s_swordDropped = false;
            s_pistolDropped = false;
        }

        static void EnsureSceneHook()
        {
            if (s_sceneHookInstalled) return;
            SceneManager.sceneLoaded += OnSceneLoaded;
            s_sceneHookInstalled = true;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 跨关重置策略：
            //   已真正装备 → 永久标记已掉落，不再重复刷出
            //   尚未装备（无论"从未掉过"还是"掉过但漏拾"）→ 重置标记，下一关有机会再掉
            // 否则一旦玩家漏过一次拾取，本 Run 内就再也拿不到武器了。
            s_swordDropped = PlayerCombat.HasSword;
            s_pistolDropped = PlayerCombat.HasPistol;
        }

        /// <summary>敌人 / Boss 死亡时调用；<paramref name="worldPos"/> 用作拾取物落点。</summary>
        public static void OnEnemyKilled(Vector3 worldPos)
        {
            EnsureSceneHook();
            s_kills++;

            // 先掉剑，再掉枪：一次击杀最多触发一次掉落
            if (TryRollDrop(worldPos, PlayerCombat.WeaponMode.Sword,
                    ref s_swordDropped, PlayerCombat.HasSword,
                    SwordStartKills, SwordDropChance, SwordPityKills))
                return;

            TryRollDrop(worldPos, PlayerCombat.WeaponMode.Pistol,
                ref s_pistolDropped, PlayerCombat.HasPistol,
                PistolStartKills, PistolDropChance, PistolPityKills);
        }

        static bool TryRollDrop(Vector3 worldPos, PlayerCombat.WeaponMode mode,
            ref bool dropped, bool playerAlreadyHas,
            int startKills, float chance, int pityKills)
        {
            if (dropped || playerAlreadyHas) return false;
            if (s_kills < startKills) return false;

            bool pity = s_kills >= pityKills;
            bool roll = Random.value < chance;
            if (!pity && !roll) return false;

            dropped = true;
            WeaponPickup.SpawnAt(worldPos + Vector3.up * 0.45f, mode);
            return true;
        }
    }
}
