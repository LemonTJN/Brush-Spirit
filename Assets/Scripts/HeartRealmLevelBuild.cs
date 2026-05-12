using System.Collections.Generic;
using BrushSpirit.LevelFlow;
using UnityEngine;

namespace BrushSpirit
{
    /// <summary>绘心关卡地形与机关（由 <see cref="GameRuntimeBootstrap"/> 调用）。</summary>
    public static class HeartRealmLevelBuild
    {
        static Transform TerrainRoot(string name)
        {
            var go = new GameObject(name);
            return go.transform;
        }

        static void Ramp(Sprite spr, Transform parent, float x, float y, float w, float h, float rotZDeg,
            bool slowRamp = false)
        {
            var go = new GameObject("TerrainRamp");
            go.tag = "Ground";
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotZDeg);
            go.transform.localScale = new Vector3(w, h, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = new Color(0.42f, 0.38f, 0.46f);
            sr.sortingOrder = -5;
            go.AddComponent<BoxCollider2D>();
            if (slowRamp)
            {
                var hr = go.AddComponent<HeartRampTerrain>();
                hr.downhillSlideAccel = 28f;
                hr.extraDownwardWhileOnRamp = 28f;
            }
        }

        static void Column(Sprite spr, Transform parent, float x, float yMid, float w, float h)
        {
            var go = new GameObject("TerrainColumn");
            go.tag = "Ground";
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, yMid, 0f);
            go.transform.localScale = new Vector3(w, h, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = new Color(0.38f, 0.36f, 0.44f);
            sr.sortingOrder = -5;
            go.AddComponent<BoxCollider2D>();
        }

        static void Beam(Sprite spr, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject("TerrainBeam");
            go.tag = "Ground";
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(w, h, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = new Color(0.48f, 0.44f, 0.38f, 0.95f);
            sr.sortingOrder = -3;
            go.AddComponent<BoxCollider2D>();
        }

        static void Pendulum(Sprite spr, Transform parent, float pivotX, float pivotY, float phaseOffset = 0f)
        {
            var pivot = new GameObject("HeartPendulum");
            pivot.transform.SetParent(parent);
            pivot.transform.position = new Vector3(pivotX, pivotY, 0f);
            var pivotComp = pivot.AddComponent<HeartPendulumPivot>();
            pivotComp.phaseTimeOffset = phaseOffset;
            var arm = new GameObject("PendulumArm");
            arm.transform.SetParent(pivot.transform, false);
            arm.transform.localPosition = new Vector3(0f, -2.35f, 0f);
            arm.transform.localRotation = Quaternion.identity;
            arm.transform.localScale = new Vector3(2.1f, 0.38f, 1f);
            var sr = arm.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = new Color(0.55f, 0.32f, 0.38f, 0.85f);
            sr.sortingOrder = 6;
            var box = arm.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            arm.AddComponent<HeartPendulumBladeHit>();
        }

        static void DesatPatch(Transform root, Sprite spr, float cx, float cy, float w, float h, bool falloff,
            float edgeFrac = 0.32f)
        {
            var go = new GameObject("DesatPatch");
            go.layer = 0;
            go.transform.SetParent(root);
            go.transform.position = new Vector3(cx, cy, 0f);
            go.transform.localScale = new Vector3(w, h, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = new Color(0.72f, 0.7f, 0.76f, falloff ? 0.34f : 0.38f);
            sr.sortingOrder = -4;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            var z = go.AddComponent<HeartDesaturationZone>();
            if (falloff)
            {
                z.useHorizontalFalloff = true;
                z.edgeBandFraction = edgeFrac;
                z.edgeDamageMultiplier = 0.38f;
                z.damagePerTick = 2.55f;
            }
        }

        /// <summary>整块均匀褪色（如月牙池心），无核边分区。</summary>
        static void DesatPatchUniform(Transform root, Sprite spr, float cx, float cy, float w, float h,
            float damagePerTick, float uniformMoveSlow = 1f)
        {
            var go = new GameObject("DesatPatchUniform");
            go.layer = 0;
            go.transform.SetParent(root);
            go.transform.position = new Vector3(cx, cy, 0f);
            go.transform.localScale = new Vector3(w, h, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = new Color(0.66f, 0.64f, 0.74f, 0.4f);
            sr.sortingOrder = -4;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            var z = go.AddComponent<HeartDesaturationZone>();
            z.damagePerTick = damagePerTick;
            z.tickInterval = 0.38f;
            z.uniformPlayerHorizontalSlow = uniformMoveSlow;
        }

        /// <summary>褪色菌斑：水平内侧核心高伤，外侧边缘只减速且敌人更耐打。</summary>
        static void DesatPatchCoreEdge(Transform root, Sprite spr, float cx, float cy, float w, float h,
            float coreFrac, float coreDmg, float coreMoveSlow, float edgeMoveSlow, float enemyEdgeMul)
        {
            var go = new GameObject("DesatMildew");
            go.layer = 0;
            go.transform.SetParent(root);
            go.transform.position = new Vector3(cx, cy, 0f);
            go.transform.localScale = new Vector3(w, h, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = new Color(0.76f, 0.74f, 0.82f, 0.34f);
            sr.sortingOrder = -4;
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            var z = go.AddComponent<HeartDesaturationZone>();
            z.useCoreEdgeBehavior = true;
            z.coreWidthFraction = coreFrac;
            z.coreDamagePerTick = coreDmg;
            z.corePlayerHorizontalSlow = coreMoveSlow;
            z.edgePlayerHorizontalSlow = edgeMoveSlow;
            z.edgeEnemyDamageTakenMultiplier = enemyEdgeMul;
            z.tickInterval = 0.4f;
        }

        /// <summary>绘心 01「褪色庭」：月牙浅洼、斜堤、残柱/半墙、窄灯笼梁、双钟摆、菌斑式褪色域。</summary>
        public static void BuildHeartRealm01Courtyard(Sprite spr, float groundWidth)
        {
            var root = TerrainRoot("HeartTerrain_01");
            var desRoot = new GameObject("HeartDesaturationRoot").transform;
            desRoot.SetParent(root);

            // 月牙池外圈：略靠左、收径，减少对主跑道的挤压
            var pool = new GameObject("ShallowMoonPool");
            pool.transform.SetParent(root);
            pool.transform.position = new Vector3(-11.2f, -3.72f, 0f);
            pool.transform.localScale = new Vector3(8.6f, 0.48f, 1f);
            var psr = pool.AddComponent<SpriteRenderer>();
            psr.sprite = spr;
            psr.color = new Color(0.52f, 0.6f, 0.72f, 0.26f);
            psr.sortingOrder = -5;
            var pbox = pool.AddComponent<BoxCollider2D>();
            pbox.isTrigger = true;
            pool.AddComponent<HeartShallowPoolZone>().horizontalVelocityRetain = 0.78f;

            // 池心强褪色 + 雾内明显减速
            DesatPatchUniform(desRoot, spr, -11.2f, -3.54f, 3.15f, 0.48f, 3.05f, 0.62f);

            // 主菌斑：核/边均带较强水平减速
            DesatPatchCoreEdge(desRoot, spr, 2.4f, -3.56f, 10.4f, 0.52f, 0.34f, 3.95f, 0.64f, 0.42f, 0.44f);

            // 斜堤靠场地两侧，中间缓坡更短，避免与浅洼、雾区叠成「三角卡脚」
            Ramp(spr, root, -19.2f, -3.22f, 5.8f, 0.4f, 13f, true);
            Ramp(spr, root, 15.8f, -3.24f, 5.5f, 0.4f, -11.5f, true);
            Ramp(spr, root, -5.5f, -3.36f, 5.6f, 0.32f, 7f, true);

            // 两根远柱 + 一根矮墙，保留分割感但不挡主干通道
            Column(spr, root, -22.2f, 0.42f, 0.48f, 4.95f);
            Column(spr, root, 19.4f, 0.38f, 0.46f, 5.05f);
            Column(spr, root, -8.8f, -1.9f, 0.44f, 2f);

            // 两根窄灯笼梁
            Beam(spr, root, -12.5f, 4.05f, 1.62f, 0.1f);
            Beam(spr, root, 10.2f, 4.65f, 1.58f, 0.1f);

            // 双钟摆略抬高、外移，减少与平地走位带重叠感
            const float pendT = 3.2f;
            Pendulum(spr, root, -9f, 6.35f, 0f);
            Pendulum(spr, root, 8.6f, 6.35f, pendT * 0.5f);
        }

        /// <summary>
        /// 绘心 02「裂帛廊」：<strong>倒置 V（∧ 脊）</strong>、侧缘、抬高链桥、桥下雾、补救台与绊索；返回浮桥刷怪点。
        /// </summary>
        public static List<Transform> BuildHeartRealm02Corridor(Sprite spr, float groundWidth)
        {
            var bridgeSpawns = new List<Transform>(10);
            var root = TerrainRoot("HeartTerrain_02");
            float half = groundWidth * 0.5f - 0.35f;

            void Pit(float centerX)
            {
                var go = new GameObject("PitEdge");
                go.transform.SetParent(root);
                go.transform.position = new Vector3(centerX, 1.2f, 0f);
                go.transform.localScale = new Vector3(1.1f, 11f, 1f);
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                go.AddComponent<HeartPitDamageZone>().damagePerTick = 7.2f;
            }

            Pit(-half + 0.55f);
            Pit(half - 0.55f);

            // 倒置 V：两翼向廊心「抬起」汇成脊线（∧），与原先向心下沉的槽谷相反。
            void RidgeWing(bool left)
            {
                float rot = left ? -8.8f : 8.8f;
                float x = left ? -5.35f : 5.35f;
                var go = new GameObject(left ? "CorridorRidgeWing_L" : "CorridorRidgeWing_R");
                go.tag = "Ground";
                go.transform.SetParent(root);
                go.transform.position = new Vector3(x, -3.28f, 0f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, rot);
                go.transform.localScale = new Vector3(12f, 0.4f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.color = new Color(0.3f, 0.28f, 0.37f);
                sr.sortingOrder = -5;
                go.AddComponent<BoxCollider2D>();
                var ramp = go.AddComponent<HeartRampTerrain>();
                ramp.horizontalMoveRetain = 0.9f;
                ramp.downhillSlideAccel = 10f;
                ramp.extraDownwardWhileOnRamp = 8f;
            }

            RidgeWing(true);
            RidgeWing(false);

            var ridgeCap = new GameObject("CorridorRidgeCap");
            ridgeCap.tag = "Ground";
            ridgeCap.transform.SetParent(root);
            ridgeCap.transform.position = new Vector3(0f, -3.04f, 0f);
            ridgeCap.transform.localScale = new Vector3(2.45f, 0.22f, 1f);
            var capSr = ridgeCap.AddComponent<SpriteRenderer>();
            capSr.sprite = spr;
            capSr.color = new Color(0.34f, 0.32f, 0.4f);
            capSr.sortingOrder = -5;
            ridgeCap.AddComponent<BoxCollider2D>();

            void InkLineSide(float cx)
            {
                var go = new GameObject("TroughInkLineSide");
                go.transform.SetParent(root);
                go.transform.position = new Vector3(cx, -3.66f, 0f);
                go.transform.localScale = new Vector3(3.8f, 0.09f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.color = new Color(0.16f, 0.14f, 0.26f, 0.5f);
                sr.sortingOrder = -3;
            }

            InkLineSide(-5.8f);
            InkLineSide(5.8f);

            void BridgeSeg(float cx, float cy, float standSec, float standMul, bool cracked, List<Transform> spawns)
            {
                var go = new GameObject("BridgeSeg");
                go.tag = "Ground";
                go.transform.SetParent(root);
                go.transform.position = new Vector3(cx, cy, 0f);
                go.transform.localScale = new Vector3(1.45f, 0.34f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.color = new Color(0.44f, 0.4f, 0.36f);
                sr.sortingOrder = -4;
                go.AddComponent<BoxCollider2D>();
                var frag = go.AddComponent<HeartFragileBridgeSegment>();
                frag.breakAfterStandSeconds = standSec;
                frag.standTimeMultiplier = standMul;
                frag.crackVisual = cracked;

                var sp = new GameObject("BridgeEnemySpawn");
                sp.transform.SetParent(go.transform, false);
                sp.transform.localPosition = new Vector3(0f, 0.24f, 0f);
                spawns.Add(sp.transform);
            }

            const float bridgeY = 1.38f;
            float[] bridgeXs = { -6.85f, -4.25f, -1.55f, 1.55f, 4.25f, 6.85f };
            for (int i = 0; i < bridgeXs.Length; i++)
            {
                bool cracked = i % 3 == 1;
                BridgeSeg(bridgeXs[i], bridgeY, cracked ? 0.95f : 2.15f, cracked ? 2.2f : 1f, cracked, bridgeSpawns);
            }

            var under = new GameObject("UnderBridgeDesat");
            under.transform.SetParent(root);
            under.transform.position = new Vector3(0f, -3.54f, 0f);
            under.transform.localScale = new Vector3(groundWidth * 0.34f, 0.34f, 1f);
            var uSr = under.AddComponent<SpriteRenderer>();
            uSr.sprite = spr;
            uSr.color = new Color(0.62f, 0.58f, 0.72f, 0.28f);
            uSr.sortingOrder = -4;
            under.AddComponent<BoxCollider2D>().isTrigger = true;
            var uz = under.AddComponent<HeartDesaturationZone>();
            uz.damagePerTick = 2.2f;
            uz.tickInterval = 0.4f;

            void RescuePlat(float cx)
            {
                var go = new GameObject("RescueLedge");
                go.tag = "Ground";
                go.transform.SetParent(root);
                go.transform.position = new Vector3(cx, -2.28f, 0f);
                go.transform.localScale = new Vector3(1.35f, 0.2f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.color = new Color(0.36f, 0.34f, 0.4f);
                sr.sortingOrder = -4;
                go.AddComponent<BoxCollider2D>();
            }

            RescuePlat(-3.4f);
            RescuePlat(3.4f);

            void WallCage(float sign)
            {
                float x = sign * (half - 1.38f);
                var cage = new GameObject("WallCage_Lone");
                cage.transform.SetParent(root);
                cage.transform.position = new Vector3(x, 0.25f, 0f);

                var body = new GameObject("CageBody");
                body.tag = "Ground";
                body.transform.SetParent(cage.transform, false);
                body.transform.localPosition = Vector3.zero;
                body.transform.localScale = new Vector3(0.32f, 2.05f, 1f);
                var bSr = body.AddComponent<SpriteRenderer>();
                bSr.sprite = spr;
                bSr.color = new Color(0.24f, 0.22f, 0.3f);
                bSr.sortingOrder = -2;
                body.AddComponent<BoxCollider2D>();

                var top = new GameObject("CageTopLedge");
                top.tag = "Ground";
                top.transform.SetParent(cage.transform, false);
                top.transform.localPosition = new Vector3(-sign * 0.2f, 1.18f, 0f);
                top.transform.localScale = new Vector3(1f, 0.16f, 1f);
                var tSr = top.AddComponent<SpriteRenderer>();
                tSr.sprite = spr;
                tSr.color = new Color(0.4f, 0.37f, 0.45f);
                tSr.sortingOrder = -2;
                top.AddComponent<BoxCollider2D>();
            }

            WallCage(-1f);

            void Trip(float x)
            {
                var go = new GameObject("InkTripwire");
                go.transform.SetParent(root);
                go.transform.position = new Vector3(x, -2.52f, 0f);
                go.transform.localScale = new Vector3(3.2f, 0.1f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.color = new Color(0.32f, 0.26f, 0.44f, 0.58f);
                sr.sortingOrder = 2;
                go.AddComponent<BoxCollider2D>().isTrigger = true;
                go.AddComponent<HeartInkTripwire>().tripHorizontalSpeed = 3.85f;
            }

            Trip(-4.2f);
            Trip(4.2f);

            BuildDesaturationThinGround(spr, groundWidth, root, true);
            return bridgeSpawns;
        }

        /// <summary>绘心 03：漂移褪色域簇。</summary>
        public static void BuildDriftingDesaturationCluster(Sprite spr)
        {
            var drift = new GameObject("HeartDriftDesat");
            drift.transform.position = new Vector3(0f, 0f, 0f);
            var motor = drift.AddComponent<HeartHorizontalDrift>();
            motor.speed = 0.55f;
            motor.minX = -16f;
            motor.maxX = 16f;

            void Patch(float lx, float ly, float w, float h)
            {
                var go = new GameObject("DesatDriftChild");
                go.layer = 0;
                go.transform.SetParent(drift.transform);
                go.transform.localPosition = new Vector3(lx, ly, 0f);
                go.transform.localScale = new Vector3(w, h, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.color = new Color(0.7f, 0.68f, 0.78f, 0.36f);
                sr.sortingOrder = -4;
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                go.AddComponent<HeartDesaturationZone>();
            }

            Patch(-6f, -3.55f, 4.5f, 0.48f);
            Patch(5f, -3.52f, 4.2f, 0.46f);
        }

        /// <summary>绘心 03「悬枢阶」：旋转枢轴带落脚点、墨涡、外环坡。</summary>
        public static void BuildHeartRealm03PivotArena(Sprite spr, float groundWidth)
        {
            var root = TerrainRoot("HeartTerrain_03");

            var spin = new GameObject("HeartRotatingCarry");
            spin.transform.SetParent(root);
            spin.transform.position = new Vector3(0f, -0.35f, 0f);
            spin.AddComponent<HeartRotatingArenaRoot>().degreesPerSecond = 7.5f;

            void SpinPlat(float localX, float localY, float w, float h)
            {
                var go = new GameObject("SpinPlat");
                go.tag = "Ground";
                go.transform.SetParent(spin.transform, false);
                go.transform.localPosition = new Vector3(localX, localY, 0f);
                go.transform.localScale = new Vector3(w, h, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.color = new Color(0.4f, 0.38f, 0.48f);
                sr.sortingOrder = -5;
                go.AddComponent<BoxCollider2D>();
            }

            SpinPlat(-10.5f, 0.15f, 2.6f, 0.42f);
            SpinPlat(10.5f, 0.15f, 2.6f, 0.42f);
            SpinPlat(0f, 2.4f, 3.2f, 0.4f);

            Ramp(spr, root, -22f, -1.05f, 4.2f, 0.38f, 22f);
            Ramp(spr, root, 21f, -1.1f, 4f, 0.38f, -20f);

            var vortex = new GameObject("Maelstrom");
            vortex.transform.SetParent(root);
            vortex.transform.position = new Vector3(0f, -0.85f, 0f);
            vortex.transform.localScale = Vector3.one;
            var circ = vortex.AddComponent<CircleCollider2D>();
            circ.isTrigger = true;
            circ.radius = 3.2f;
            var pull = vortex.AddComponent<HeartMaelstromPullZone>();
            var anchor = new GameObject("PullAnchor").transform;
            anchor.SetParent(vortex.transform, false);
            anchor.localPosition = new Vector3(0f, -2.2f, 0f);
            pull.pullTarget = anchor;
            pull.acceleration = 11f;
            var vis = new GameObject("MaelstromVis");
            vis.transform.SetParent(vortex.transform, false);
            vis.transform.localScale = Vector3.one * 6.4f;
            var vsr = vis.AddComponent<SpriteRenderer>();
            vsr.sprite = spr;
            vsr.color = new Color(0.28f, 0.22f, 0.38f, 0.22f);
            vsr.sortingOrder = -6;
        }

        /// <summary>绘心 04「王座」：同心阶条、漂物、墨镜带、起落墨墙。</summary>
        public static void BuildHeartRealm04ThroneArena(Sprite spr, float groundWidth)
        {
            var root = TerrainRoot("HeartTerrain_04");

            void Step(float x, float y, float w)
            {
                var go = new GameObject("RingStep");
                go.tag = "Ground";
                go.transform.SetParent(root);
                go.transform.position = new Vector3(x, y, 0f);
                go.transform.localScale = new Vector3(w, 0.4f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.color = new Color(0.36f, 0.34f, 0.42f);
                sr.sortingOrder = -5;
                go.AddComponent<BoxCollider2D>();
            }

            Step(-12f, -1.15f, 3.4f);
            Step(-6f, -0.35f, 3.8f);
            Step(0f, 0.45f, 4.2f);
            Step(6f, 1.15f, 3.8f);
            Step(12f, 1.85f, 3.2f);

            var driftRoot = new GameObject("DriftingDebris");
            driftRoot.transform.SetParent(root);
            driftRoot.transform.position = new Vector3(-5f, 3.1f, 0f);
            driftRoot.AddComponent<HeartDriftingTerrain>().speed = 0.32f;
            var deb = new GameObject("DebrisPlank");
            deb.tag = "Ground";
            deb.transform.SetParent(driftRoot.transform, false);
            deb.transform.localPosition = Vector3.zero;
            deb.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            deb.transform.localScale = new Vector3(2.8f, 0.42f, 1f);
            var dsr = deb.AddComponent<SpriteRenderer>();
            dsr.sprite = spr;
            dsr.color = new Color(0.45f, 0.4f, 0.36f, 0.9f);
            dsr.sortingOrder = -4;
            deb.AddComponent<BoxCollider2D>();

            var mirror = new GameObject("InkMirrorStrip");
            mirror.transform.SetParent(root);
            mirror.transform.position = new Vector3(-3.5f, -3.58f, 0f);
            mirror.transform.localScale = new Vector3(7f, 0.38f, 1f);
            var msr = mirror.AddComponent<SpriteRenderer>();
            msr.sprite = spr;
            msr.color = new Color(0.5f, 0.55f, 0.62f, 0.35f);
            msr.sortingOrder = -4;
            var mbox = mirror.AddComponent<BoxCollider2D>();
            mbox.isTrigger = true;
            mirror.AddComponent<HeartInkMirrorZone>();

            var wall = new GameObject("PeriodicInkWall");
            wall.transform.SetParent(root);
            wall.transform.position = new Vector3(0f, -0.5f, 0f);
            wall.transform.localScale = new Vector3(0.42f, 8.5f, 1f);
            var wsr = wall.AddComponent<SpriteRenderer>();
            wsr.sprite = spr;
            wsr.color = new Color(0.12f, 0.1f, 0.16f, 0f);
            wsr.sortingOrder = 8;
            var wbox = wall.AddComponent<BoxCollider2D>();
            wbox.enabled = false;
            wall.AddComponent<HeartPeriodicInkWall>().interval = 5.5f;
        }

        /// <summary>绘心 02：窄带褪色（供裂帛廊调用）。<paramref name="corridorSparse"/> 为 true 时只铺一条淡雾，减少与主路重叠。</summary>
        public static void BuildDesaturationThinGround(Sprite spr, float groundWidth, Transform parent = null,
            bool corridorSparse = false)
        {
            var rootGo = new GameObject("HeartDesatThinRoot");
            var root = rootGo.transform;
            if (parent != null)
                root.SetParent(parent);

            void Patch(float centerX, float widthWorld, float heightWorld)
            {
                var go = new GameObject("DesatThin");
                go.layer = 0;
                go.transform.SetParent(root);
                go.transform.position = new Vector3(centerX, -3.58f, 0f);
                go.transform.localScale = new Vector3(widthWorld, heightWorld, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.color = new Color(0.68f, 0.66f, 0.74f, corridorSparse ? 0.28f : 0.34f);
                sr.sortingOrder = -4;
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                var z = go.AddComponent<HeartDesaturationZone>();
                z.damagePerTick = corridorSparse ? 1.95f : 2.15f;
                z.tickInterval = 0.42f;
            }

            if (corridorSparse)
            {
                Patch(0f, 2.6f, 0.36f);
                return;
            }

            float half = groundWidth * 0.5f - 2f;
            Patch(-6.5f, 3.2f, 0.4f);
            Patch(Mathf.Min(7f, half - 2f), 2.9f, 0.4f);
        }

        /// <summary>焚道式侧墙。</summary>
        public static void BuildNarrowSideWalls(float groundWidth)
        {
            var root = new GameObject("HeartNarrowWalls").transform;
            float half = groundWidth * 0.5f - 0.28f;
            const float midY = 2.05f;
            const float wallH = 24f;
            foreach (float sign in new[] { -1f, 1f })
            {
                var go = new GameObject(sign < 0f ? "HeartWall_L" : "HeartWall_R");
                go.tag = "Ground";
                go.transform.SetParent(root);
                go.transform.position = new Vector3(sign * half, midY, 0f);
                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.55f, wallH);
            }
        }

        public static void BuildSinkerPlatforms(Transform platformsParent, Sprite spr)
        {
            if (platformsParent == null) return;

            void SinkPlat(float x, float y, float w, float h)
            {
                var go = new GameObject("SinkPlatform");
                go.tag = "Ground";
                go.transform.SetParent(platformsParent);
                go.transform.position = new Vector3(x, y, 0f);
                go.transform.localScale = new Vector3(w, h, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.color = new Color(0.42f, 0.4f, 0.48f);
                sr.sortingOrder = -5;
                go.AddComponent<BoxCollider2D>();
                go.AddComponent<HeartPlatformSinker>();
            }

            SinkPlat(-10f, 5.4f, 2.6f, 0.62f);
            SinkPlat(12f, 6.1f, 2.4f, 0.6f);
        }
    }
}
