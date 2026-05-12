using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>
    /// 在 <see cref="Player.PlayerMovement"/> 的 FixedUpdate 内应用绘心环境（浅池减速、墨涡牵引）。
    /// </summary>
    public static class HeartSceneEnvironment
    {
        static readonly Collider2D[] OverlapBuffer = new Collider2D[24];

        /// <summary>含触发器，避免依赖全局 <see cref="Physics2D.queriesHitTriggers"/> 初始化顺序。</summary>
        static readonly ContactFilter2D OverlapTriggersAllLayers = BuildOverlapTriggersFilter();
        static readonly ContactFilter2D OverlapSolidsNoTriggers = BuildSolidColliderFilter();

        static ContactFilter2D BuildOverlapTriggersFilter()
        {
            var f = new ContactFilter2D();
            f.useLayerMask = false;
            f.useTriggers = true;
            return f;
        }

        static ContactFilter2D BuildSolidColliderFilter()
        {
            var f = new ContactFilter2D();
            f.useLayerMask = false;
            f.useTriggers = false;
            return f;
        }

        /// <summary>水平目标速度倍率（浅池、褪色菌斑边缘、斜堤等）。</summary>
        public static float GetHorizontalMoveMultiplier(Vector2 worldPos)
        {
            int n = Physics2D.OverlapCircle(worldPos, 0.62f, OverlapTriggersAllLayers, OverlapBuffer);
            float mul = 1f;
            for (int i = 0; i < n; i++)
            {
                var c = OverlapBuffer[i];
                if (c == null) continue;
                var pool = c.GetComponent<HeartShallowPoolZone>();
                if (pool != null)
                    mul *= pool.horizontalVelocityRetain;

                var desat = c.GetComponent<HeartDesaturationZone>();
                if (desat != null)
                {
                    var zc = desat.ZoneCollider;
                    if (zc != null && zc.OverlapPoint(worldPos))
                    {
                        if (desat.useCoreEdgeBehavior)
                            mul *= desat.IsPlayerInCore(worldPos)
                                ? desat.corePlayerHorizontalSlow
                                : desat.edgePlayerHorizontalSlow;
                        else if (desat.uniformPlayerHorizontalSlow < 0.999f)
                            mul *= desat.uniformPlayerHorizontalSlow;
                    }
                }

                var ramp = c.GetComponent<HeartRampTerrain>();
                if (ramp != null)
                    mul *= ramp.horizontalMoveRetain;
            }

            return Mathf.Clamp(mul, 0.45f, 1f);
        }

        /// <summary>
        /// 玩家水平移速倍率：用脚附近 + 躯干偏下两点采样，避免薄菌斑/浅洼只在脚底而 <paramref name="bodyCenterWorld"/> 在触发体上方时检测不到。
        /// </summary>
        public static float GetHorizontalMoveMultiplierForPlayer(Vector2 feetWorld, Vector2 bodyCenterWorld)
        {
            Vector2 low = feetWorld + Vector2.up * 0.12f;
            Vector2 mid = bodyCenterWorld + Vector2.down * 0.42f;
            float a = GetHorizontalMoveMultiplier(low);
            float b = GetHorizontalMoveMultiplier(mid);
            return Mathf.Clamp(Mathf.Min(a, b), 0.35f, 1f);
        }

        /// <summary>目标世界位置处在褪色边缘等时，对其造成的伤害倍率（叠乘取最小）。</summary>
        public static float GetOutgoingDamageMultiplierOnTarget(Vector2 worldPos)
        {
            int n = Physics2D.OverlapCircle(worldPos, 0.65f, OverlapTriggersAllLayers, OverlapBuffer);
            float mult = 1f;
            for (int i = 0; i < n; i++)
            {
                var c = OverlapBuffer[i];
                if (c == null) continue;
                var desat = c.GetComponent<HeartDesaturationZone>();
                if (desat == null) continue;
                mult = Mathf.Min(mult, desat.EvaluateOutgoingDamageMultiplierAt(worldPos));
            }

            return Mathf.Clamp(mult, 0.15f, 1f);
        }

        /// <summary>斜堤：沿坡面向下的滑动加速度（用于站立下滑）。</summary>
        public static Vector2 GetRampSlideAcceleration(Vector2 worldPos)
        {
            int n = Physics2D.OverlapCircle(worldPos, 0.42f, OverlapSolidsNoTriggers, OverlapBuffer);
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < n; i++)
            {
                var c = OverlapBuffer[i];
                if (c == null) continue;
                var ramp = c.GetComponent<HeartRampTerrain>();
                if (ramp != null)
                    sum += ramp.EvaluateSlideAcceleration();
            }

            return sum;
        }

        /// <summary>站在斜坡上时额外向下的加速度（世界单位/秒²），使顺坡下落更快。</summary>
        public static float GetRampExtraDownwardAccel(Vector2 worldPos)
        {
            int n = Physics2D.OverlapCircle(worldPos, 0.42f, OverlapSolidsNoTriggers, OverlapBuffer);
            float sum = 0f;
            for (int i = 0; i < n; i++)
            {
                var c = OverlapBuffer[i];
                if (c == null) continue;
                var ramp = c.GetComponent<HeartRampTerrain>();
                if (ramp != null)
                    sum += ramp.extraDownwardWhileOnRamp;
            }

            return sum;
        }

        /// <summary>墨涡等附加加速度（世界单位 / 秒）。</summary>
        public static Vector2 GetExtraAcceleration(Vector2 worldPos)
        {
            int n = Physics2D.OverlapCircle(worldPos, 0.55f, OverlapTriggersAllLayers, OverlapBuffer);
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < n; i++)
            {
                var c = OverlapBuffer[i];
                if (c == null) continue;
                var pull = c.GetComponent<HeartMaelstromPullZone>();
                if (pull != null)
                    sum += pull.GetAccelerationAt(worldPos);
            }

            return sum;
        }

        /// <summary>墨镜区：水平惯性略增（滑感）。</summary>
        public static float GetInkMirrorHorizontalBoost(Vector2 worldPos)
        {
            int n = Physics2D.OverlapCircle(worldPos, 0.38f, OverlapTriggersAllLayers, OverlapBuffer);
            float boost = 1f;
            for (int i = 0; i < n; i++)
            {
                var c = OverlapBuffer[i];
                if (c == null) continue;
                if (c.GetComponent<HeartInkMirrorZone>() != null)
                    boost = Mathf.Max(boost, 1.22f);
            }

            return Mathf.Min(boost, 1.35f);
        }
    }
}
