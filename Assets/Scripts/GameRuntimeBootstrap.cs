using System.Collections.Generic;
using BrushSpirit.Core;
using BrushSpirit.Enemies;
using BrushSpirit.Items;
using BrushSpirit.LevelFlow;
using BrushSpirit.Player;
using BrushSpirit.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BrushSpirit
{
    public class GameRuntimeBootstrap : MonoBehaviour
    {
        struct SectionEnemyTuning
        {
            public float maxHp;
            public float moveSpeed;
            public float attackDamage;
            public float attackCooldown;
            public float attackRange;
            public int xpReward;
            public float visualScale;
            public Color tint;
            public float waveGap;
        }

        struct SectionBossTuning
        {
            public float maxHp;
            public float moveSpeed;
            public float slamDamage;
        }

        SpriteRenderer _backdropA;
        SpriteRenderer _backdropB;
        SpriteRenderer _backdropC;

        [Header("Art Overrides (可选；为空时回退到白方块)")]
        public Sprite backgroundSprite;
        public Sprite playerSprite;
        public RuntimeAnimatorController playerController;        // 默认/Sword
        public RuntimeAnimatorController bareController;          // 1 号：空手
        public RuntimeAnimatorController pistolController;        // 3 号：手枪
        public GameObject bulletPrefab;                           // 手枪子弹
        public Sprite enemySprite;
        public Sprite bossSprite;
        public Sprite attackCircleSprite;
        public GameObject attackCirclePrefab;
        public GameObject hitSparkPrefab;
        public GameObject attackHitPrefab;
        public Sprite platformSprite;
        public Sprite groundSprite;
        public Sprite pickupSprite;

        static GameRuntimeBootstrap _instance;
        public static GameRuntimeBootstrap Instance => _instance;

        void Awake()
        {
            _instance = this;
            string sn = SceneManager.GetActiveScene().name;
            EnsureEventSystem();
            Physics2D.gravity = new Vector2(0f, -30f);
            var spr = MakeWhiteSprite();

            float groundWidth = 60f;
            Vector3 playerSpawn = new Vector3(-7f, -2.85f, 0f);
            List<int> waveCounts = new List<int> { 2, 3 };
            bool hasBoss = true;
            string nextScene = "";
            string clearTitle = "";
            string clearSub = "";
            int spawnPointCount = 3;
            float bossSpawnX = 11f;
            List<Transform> hr02BridgeSpawns = null;
            SectionEnemyTuning enemyTune = DefaultEnemyTuning();
            SectionBossTuning bossTune = new SectionBossTuning
            {
                maxHp = 760f,
                moveSpeed = 2.8f,
                slamDamage = 30f
            };

            switch (sn)
            {
                case "InkForest_01":
                    groundWidth = 46f;
                    waveCounts = new List<int> { 2, 3 };
                    hasBoss = false;
                    nextScene = "InkForest_02";
                    clearTitle = "墨林 · 林缘 已肃清";
                    clearSub = "墨色渐深，继续前行。";
                    spawnPointCount = 3;
                    enemyTune = new SectionEnemyTuning
                    {
                        maxHp = 28f,
                        moveSpeed = 1.75f,
                        attackDamage = 3.2f,
                        attackCooldown = 1.05f,
                        attackRange = 2.4f,
                        xpReward = 10,
                        visualScale = 0.88f,
                        tint = new Color(0.14f, 0.14f, 0.17f),
                        waveGap = 0.35f
                    };
                    break;
                case "InkForest_02":
                    groundWidth = 54f;
                    waveCounts = new List<int> { 3, 5 };
                    hasBoss = false;
                    nextScene = "InkForest_03";
                    clearTitle = "墨林 · 深处 已肃清";
                    clearSub = "树心已近，直面墨树。";
                    spawnPointCount = 4;
                    enemyTune = new SectionEnemyTuning
                    {
                        maxHp = 40f,
                        moveSpeed = 2.25f,
                        attackDamage = 5f,
                        attackCooldown = 0.78f,
                        attackRange = 2.75f,
                        xpReward = 15,
                        visualScale = 0.98f,
                        tint = new Color(0.12f, 0.12f, 0.16f),
                        waveGap = 0.85f
                    };
                    break;
                case "InkForest_03":
                    groundWidth = 58f;
                    waveCounts = new List<int> { 3, 6 };
                    hasBoss = true;
                    spawnPointCount = 5;
                    bossSpawnX = 22f;
                    playerSpawn = new Vector3(-7.5f, -2.85f, 0f);
                    enemyTune = new SectionEnemyTuning
                    {
                        maxHp = 52f,
                        moveSpeed = 2.55f,
                        attackDamage = 6.2f,
                        attackCooldown = 0.68f,
                        attackRange = 2.9f,
                        xpReward = 18,
                        visualScale = 1.08f,
                        tint = new Color(0.09f, 0.09f, 0.13f),
                        waveGap = 1.1f
                    };
                    bossTune = new SectionBossTuning
                    {
                        maxHp = 880f,
                        moveSpeed = 3.0f,
                        slamDamage = 34f
                    };
                    break;
                case "EmberValley_01":
                    groundWidth = 44f;
                    waveCounts = new List<int> { 2, 3, 3 };
                    hasBoss = false;
                    nextScene = "EmberValley_02";
                    clearTitle = "烬谷 · 烬口 已肃清";
                    clearSub = "峡道更窄了，焚道在前。";
                    spawnPointCount = 5;
                    bossSpawnX = 12f;
                    playerSpawn = new Vector3(-6.5f, -2.85f, 0f);
                    enemyTune = new SectionEnemyTuning
                    {
                        maxHp = 34f,
                        moveSpeed = 1.92f,
                        attackDamage = 4.1f,
                        attackCooldown = 0.95f,
                        attackRange = 2.55f,
                        xpReward = 12,
                        visualScale = 0.9f,
                        tint = new Color(0.22f, 0.16f, 0.14f),
                        waveGap = 0.28f
                    };
                    break;
                case "EmberValley_02":
                    groundWidth = 38f;
                    waveCounts = new List<int> { 3, 4, 4 };
                    hasBoss = false;
                    nextScene = "EmberValley_03";
                    clearTitle = "烬谷 · 焚道 已肃清";
                    clearSub = "谷底有东西在烧——焰心已近。";
                    spawnPointCount = 6;
                    bossSpawnX = 10f;
                    playerSpawn = new Vector3(-5.5f, -2.85f, 0f);
                    enemyTune = new SectionEnemyTuning
                    {
                        maxHp = 40f,
                        moveSpeed = 2.05f,
                        attackDamage = 4.8f,
                        attackCooldown = 0.82f,
                        attackRange = 2.62f,
                        xpReward = 14,
                        visualScale = 0.92f,
                        tint = new Color(0.2f, 0.14f, 0.12f),
                        waveGap = 0.22f
                    };
                    break;
                case "EmberValley_03":
                    groundWidth = 58f;
                    waveCounts = new List<int> { 3, 6 };
                    hasBoss = true;
                    spawnPointCount = 6;
                    bossSpawnX = 22f;
                    playerSpawn = new Vector3(-7.5f, -2.85f, 0f);
                    enemyTune = new SectionEnemyTuning
                    {
                        maxHp = 52f,
                        moveSpeed = 2.55f,
                        attackDamage = 6.2f,
                        attackCooldown = 0.68f,
                        attackRange = 2.9f,
                        xpReward = 18,
                        visualScale = 1.08f,
                        tint = new Color(0.09f, 0.09f, 0.13f),
                        waveGap = 1.1f
                    };
                    bossTune = new SectionBossTuning
                    {
                        maxHp = 880f,
                        moveSpeed = 3.0f,
                        slamDamage = 34f
                    };
                    break;
                case "HeartRealm_01":
                    groundWidth = 54f;
                    waveCounts = new List<int> { 2, 3, 4 };
                    hasBoss = false;
                    nextScene = "HeartRealm_02";
                    clearTitle = "绘心 · 褪色庭 已肃清";
                    clearSub = "裂帛廊在前，狭雾更紧。";
                    spawnPointCount = 5;
                    bossSpawnX = 12f;
                    playerSpawn = new Vector3(-5.5f, -2.85f, 0f);
                    enemyTune = new SectionEnemyTuning
                    {
                        maxHp = 38f,
                        moveSpeed = 2f,
                        attackDamage = 4.5f,
                        attackCooldown = 0.88f,
                        attackRange = 2.58f,
                        xpReward = 13,
                        visualScale = 0.92f,
                        tint = new Color(0.2f, 0.18f, 0.24f),
                        waveGap = 0.2f
                    };
                    break;
                case "HeartRealm_02":
                    groundWidth = 44f;
                    waveCounts = new List<int> { 3, 4, 4 };
                    hasBoss = false;
                    nextScene = "HeartRealm_03";
                    clearTitle = "绘心 · 裂帛廊 已肃清";
                    clearSub = "悬枢阶在上，坠台与雾一同逼近。";
                    spawnPointCount = 6;
                    bossSpawnX = 10f;
                    playerSpawn = new Vector3(-5.5f, -2.85f, 0f);
                    enemyTune = new SectionEnemyTuning
                    {
                        maxHp = 44f,
                        moveSpeed = 2.12f,
                        attackDamage = 5.2f,
                        attackCooldown = 0.8f,
                        attackRange = 2.68f,
                        xpReward = 15,
                        visualScale = 0.94f,
                        tint = new Color(0.18f, 0.16f, 0.22f),
                        waveGap = 0.17f
                    };
                    break;
                case "HeartRealm_03":
                    groundWidth = 58f;
                    waveCounts = new List<int> { 3, 6, 5 };
                    hasBoss = false;
                    nextScene = "HeartRealm_04";
                    clearTitle = "绘心 · 悬枢阶 已肃清";
                    clearSub = "王座在前，墨魇待击。";
                    spawnPointCount = 7;
                    bossSpawnX = 22f;
                    playerSpawn = new Vector3(-7.5f, -2.85f, 0f);
                    enemyTune = new SectionEnemyTuning
                    {
                        maxHp = 52f,
                        moveSpeed = 2.48f,
                        attackDamage = 6.1f,
                        attackCooldown = 0.7f,
                        attackRange = 2.85f,
                        xpReward = 17,
                        visualScale = 1.02f,
                        tint = new Color(0.15f, 0.13f, 0.2f),
                        waveGap = 0.14f
                    };
                    break;
                case "HeartRealm_04":
                    groundWidth = 58f;
                    waveCounts = new List<int> { 3, 7 };
                    hasBoss = true;
                    spawnPointCount = 7;
                    bossSpawnX = 22f;
                    playerSpawn = new Vector3(-7.5f, -2.85f, 0f);
                    enemyTune = new SectionEnemyTuning
                    {
                        maxHp = 58f,
                        moveSpeed = 2.7f,
                        attackDamage = 7f,
                        attackCooldown = 0.6f,
                        attackRange = 2.95f,
                        xpReward = 19,
                        visualScale = 1.06f,
                        tint = new Color(0.12f, 0.1f, 0.16f),
                        waveGap = 0.82f
                    };
                    bossTune = new SectionBossTuning
                    {
                        maxHp = 1180f,
                        moveSpeed = 3.15f,
                        slamDamage = 40f
                    };
                    break;
            }

            EmberValleyAshBurstSpawner ember03AshSpawner = null;
            HeartRealmRipSpawner heartRipSpawner = null;

            BuildBackdrop(spr, sn);
            BuildGround(spr, groundWidth, sn);
            if (sn == "EmberValley_02")
            {
                BuildEmberValley02SideArena(groundWidth);
                BuildEmberValley02ThinCinders(spr, groundWidth);
            }

            if (sn == "EmberValley_01")
                BuildEmberValley01CinderPatches(spr, groundWidth);
            if (sn == "InkForest_03")
                BuildInkForest03SideArena(groundWidth);
            if (sn == "EmberValley_03")
                BuildInkForest03SideArena(groundWidth, 2.45f, 24f);
            if (sn == "HeartRealm_03" || sn == "HeartRealm_04")
                BuildInkForest03SideArena(groundWidth, 2.45f, 24f);
            if (sn == "HeartRealm_01")
                HeartRealmLevelBuild.BuildHeartRealm01Courtyard(spr, groundWidth);
            if (sn == "HeartRealm_02")
            {
                BuildEmberValley02SideArena(groundWidth);
                hr02BridgeSpawns = HeartRealmLevelBuild.BuildHeartRealm02Corridor(spr, groundWidth);
            }

            if (sn == "HeartRealm_03")
                HeartRealmLevelBuild.BuildHeartRealm03PivotArena(spr, groundWidth);
            if (sn == "HeartRealm_03")
                HeartRealmLevelBuild.BuildDriftingDesaturationCluster(spr);
            if (sn == "HeartRealm_04")
                HeartRealmLevelBuild.BuildHeartRealm04ThroneArena(spr, groundWidth);
            if (sn == "EmberValley_01")
                BuildEmberValley01SideArena(groundWidth);
            var platformSpawnInfos = BuildPlatforms(spr, sn);
            if (sn == "HeartRealm_03")
            {
                var platRoot = GameObject.Find("Platforms")?.transform;
                HeartRealmLevelBuild.BuildSinkerPlatforms(platRoot, spr);
            }
            if (sn == "EmberValley_02")
                BuildEmberValley02PlatformCinders(spr);
            if (sn == "EmberValley_03")
                BuildEmberValley03PlatformCinders(spr);
            GameObject player = GetOrCreatePlayer(spr, playerSpawn);
            var gear = BuildEquipment();
            float spawnSpan = sn == "InkForest_03" || sn == "EmberValley_03" || sn == "HeartRealm_03" ||
                              sn == "HeartRealm_04"
                ? groundWidth * 0.58f
                : sn == "EmberValley_01" || sn == "HeartRealm_01"
                    ? groundWidth * 0.52f
                    : sn == "EmberValley_02" || sn == "HeartRealm_02"
                        ? groundWidth * 0.42f
                        : -1f;
            var groundSpawns = BuildSpawnPoints(spawnPointCount, spawnSpan);
            var platformSpawns = CreateSpawnTransforms(groundSpawns[0].parent, platformSpawnInfos);
            var spawns = InterleaveSpawnLists(groundSpawns, platformSpawns);

            var waveRoot = new GameObject("WaveRoot");
            var wave = waveRoot.AddComponent<WaveSpawner>();
            wave.enemiesPerWave = waveCounts;
            wave.delayBetweenWaves = enemyTune.waveGap;
            foreach (var s in spawns)
                wave.spawnPoints.Add(s);
            if (hr02BridgeSpawns != null)
            {
                foreach (var t in hr02BridgeSpawns)
                {
                    if (t != null)
                        wave.spawnPoints.Add(t);
                }
            }

            var tuneCopy = enemyTune;
            wave.EnemyFactory = sp => CreateEnemy(sp, spr, gear.whiteA, gear.whiteB, tuneCopy);

            var levelRoot = new GameObject("LevelRoot");
            var level = levelRoot.AddComponent<LevelController>();
            level.waves = wave;
            var bossSpawn = new GameObject("BossSpawn").transform;
            // Y 偏高一点，配合 BossInkTree/BossDemonKing.Start 内的 EnsureGroundFooting 自动落地，避免穿地
            bossSpawn.position = new Vector3(bossSpawnX, -1.6f, 0f);
            level.bossSpawnPoint = bossSpawn;
            level.nextSceneAfterWaves = hasBoss ? "" : nextScene;
            level.sectionClearTitle = clearTitle;
            level.sectionClearSubtitle = clearSub;

            if (sn == "EmberValley_03")
            {
                var ashGo = new GameObject("AshBurstSpawner");
                ember03AshSpawner = ashGo.AddComponent<EmberValleyAshBurstSpawner>();
                float halfEv3 = groundWidth * 0.5f - 3.2f;
                // 清波阶段略疏；Boss 出现后冷却明显缩短，并配合连发（见 Spawner）
                ember03AshSpawner.ConfigureWithBossPhase(spr, -halfEv3, halfEv3, -3.12f, 2.75f, 4.35f, 0.72f, 1.25f);
                ember03AshSpawner.SetRadialSplash(true);
            }

            if (sn == "HeartRealm_02" || sn == "HeartRealm_03" || sn == "HeartRealm_04")
            {
                var ripGo = new GameObject("HeartRipSpawner");
                heartRipSpawner = ripGo.AddComponent<HeartRealmRipSpawner>();
                float halfRip = groundWidth * 0.5f - 3.2f;
                if (sn == "HeartRealm_04")
                    heartRipSpawner.ConfigureWithBossPhase(spr, -halfRip, halfRip, -3.12f, 2.5f, 4f, 0.62f, 1.02f);
                else if (sn == "HeartRealm_03")
                    heartRipSpawner.ConfigureWithBossPhase(spr, -halfRip, halfRip, -3.12f, 2.15f, 3.45f, 2.15f, 3.45f);
                else
                    heartRipSpawner.ConfigureWithBossPhase(spr, -halfRip, halfRip, -3.12f, 3.05f, 4.85f, 2.45f, 3.85f);
                if (sn == "HeartRealm_02")
                    heartRipSpawner.SetCorridorDirectionalMode(true);
            }

            if (sn == "InkForest_01")
            {
                level.deferWaveStart = true;
                levelRoot.AddComponent<InkForest01Director>();
            }

            if (sn == "EmberValley_01")
            {
                level.deferWaveStart = true;
                levelRoot.AddComponent<EmberValley01Director>();
            }

            if (sn == "EmberValley_02")
            {
                level.deferWaveStart = true;
                levelRoot.AddComponent<EmberValley02Director>();
                levelRoot.AddComponent<EmberValley02SmokeOverlay>();
            }

            if (sn == "HeartRealm_01")
            {
                level.deferWaveStart = true;
                levelRoot.AddComponent<HeartRealm01Director>();
                level.autoAdvanceNextSceneDelay = 3.1f;
            }

            if (sn == "HeartRealm_02")
            {
                level.deferWaveStart = true;
                levelRoot.AddComponent<HeartRealm02Director>();
            }

            if (sn == "HeartRealm_03")
            {
                level.deferWaveStart = true;
                levelRoot.AddComponent<HeartRealm03Director>();
            }

            if (sn == "HeartRealm_04")
            {
                level.deferWaveStart = true;
                levelRoot.AddComponent<HeartRealm04Director>();
            }

            if (hasBoss)
            {
                level.bossTemplate = CreateBossTemplate(spr, gear.colorBoss, bossTune);
                level.bossTemplate.SetActive(false);
                var fx = levelRoot.AddComponent<ColorRestoreEffect>();
                fx.duration = 4.2f;
                fx.fullscreenTint = CreateFullscreenTintImage();
                fx.extraTargets = _backdropC != null
                    ? new[] { _backdropA, _backdropB, _backdropC }
                    : new[] { _backdropA, _backdropB };
                level.colorRestore = fx;
                if (sn == "EmberValley_03")
                {
                    fx.useCustomPalette = true;
                    fx.customStartTint = new Color(0.14f, 0.1f, 0.1f, 0.82f);
                    fx.customEndTint = new Color(0.78f, 0.32f, 0.18f, 0.12f);
                    fx.customStartSprite = new Color(0.36f, 0.28f, 0.26f);
                    fx.customEndSprite = new Color(0.85f, 0.48f, 0.32f);
                    VictoryPanel.PendingVictoryTitle = "烬谷 · 赤色归来";
                    VictoryPanel.PendingUnlockLevel = 3;
                }
                else if (sn == "HeartRealm_04")
                {
                    fx.useSpectrumRestore = true;
                    fx.useCustomPalette = false;
                    fx.duration = 5.8f;
                    VictoryPanel.PendingVictoryTitle = "绘心 · 万色归来";
                    VictoryPanel.PendingUnlockLevel = 4;
                }
                else
                {
                    VictoryPanel.PendingVictoryTitle = null;
                    VictoryPanel.PendingUnlockLevel = 0;
                }

                var vicGo = new GameObject("VictoryUI");
                vicGo.AddComponent<VictoryPanel>();
            }
            else
            {
                level.bossTemplate = null;
                level.colorRestore = null;
            }

            if (ember03AshSpawner != null && hasBoss)
                level.OnBossSpawned += () => ember03AshSpawner.SetBossPhase(true);

            if (heartRipSpawner != null && hasBoss)
                level.OnBossSpawned += () => heartRipSpawner.SetBossPhase(true);

            BuildHud(player);

            bool emberVertical = sn == "EmberValley_01" || sn == "EmberValley_02" || sn == "EmberValley_03" ||
                                 sn == "HeartRealm_01" || sn == "HeartRealm_02" || sn == "HeartRealm_03" ||
                                 sn == "HeartRealm_04";
            float cameraOrtho = sn == "InkForest_03" || emberVertical ? 6.75f : 6.5f;
            Color clearBg = sn == "EmberValley_03"
                ? new Color(0.22f, 0.15f, 0.14f)
                : sn == "EmberValley_02"
                    ? new Color(0.2f, 0.16f, 0.15f)
                    : sn == "EmberValley_01"
                        ? new Color(0.24f, 0.19f, 0.17f)
                        : sn == "HeartRealm_04"
                            ? new Color(0.14f, 0.1f, 0.16f)
                            : sn == "HeartRealm_03"
                                ? new Color(0.15f, 0.11f, 0.17f)
                                : sn == "HeartRealm_02"
                                    ? new Color(0.17f, 0.13f, 0.18f)
                                    : sn == "HeartRealm_01"
                                        ? new Color(0.18f, 0.15f, 0.2f)
                                        : new Color(0.16f, 0.17f, 0.19f);
            MainCameraEnsure.Ensure(clearBg, cameraOrtho);
            if (sn == "InkForest_03" || emberVertical)
            {
                float yMax = sn == "EmberValley_01"
                    ? 14.6f
                    : sn == "EmberValley_02"
                        ? 15.2f
                        : sn == "EmberValley_03"
                            ? 15.8f
                            : sn == "HeartRealm_01"
                                ? 11.55f
                                : sn == "HeartRealm_02"
                                    ? 15.2f
                                    : sn == "HeartRealm_03"
                                        ? 15.8f
                                        : sn == "HeartRealm_04"
                                            ? 16f
                                            : 9.4f;
                SetupCameraFollowArena(player, groundWidth, -5.15f, yMax);
            }
            else
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 p = player.transform.position;
                    cam.transform.position = new Vector3(p.x + 2.5f, p.y + 1.2f, -10f);
                }
            }

            if (sn == "EmberValley_02")
            {
                var ashGo = new GameObject("AshBurstSpawner");
                var spawner = ashGo.AddComponent<EmberValleyAshBurstSpawner>();
                float half = groundWidth * 0.5f - 3.2f;
                spawner.Configure(spr, -half, half, -3.12f, 2.45f, 4.05f);
            }

            PlayfieldBoundaryController.Ensure(true, 115);
        }

        static GameObject GetOrCreatePlayer(Sprite spr, Vector3 spawnPos)
        {
            if (PlayerRunCarry.Instance != null)
            {
                var go = PlayerRunCarry.Instance.gameObject;
                go.transform.position = spawnPos;
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.velocity = Vector2.zero;
                if (go.GetComponent<ClampToWorldBounds2D>() == null)
                {
                    var clamp = go.AddComponent<ClampToWorldBounds2D>();
                    clamp.halfWidthPad = 0.22f;
                    clamp.halfHeightPad = 0.52f;
                    clamp.skipClampWhenOutsideViewport = false;
                }

                return go;
            }

            var p = BuildPlayer(spr);
            p.transform.position = spawnPos;
            p.AddComponent<PlayerRunCarry>();
            return p;
        }

        static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        /// <summary>
        /// 占位精灵：whiteTexture 仅 1px 且 PPU=32 时在世界空间里只有约 0.03 单位，角色几乎看不见。
        /// 使用 64×64 贴图、PPU=64 → 单格 1×1 世界单位，再靠 localScale 控制体型。
        /// </summary>
        public static Sprite CreatePlaceholderSprite()
        {
            const int size = 64;
            const float ppu = 64f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32(255, 255, 255, 255);
            var arr = new Color32[size * size];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = px;
            tex.SetPixels32(arr);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
        }

        /// <summary>
        /// 直线斜杀特效：在敌人面向玩家一侧生成横向矩形闪光，模拟格斗游戏挥击判定框。
        /// facingDir = +1（玩家在右）/ -1（玩家在左）/ 0（双侧，用于以自身为中心的爆炸技能）。
        /// </summary>
        public static void ShowAttackSlashFx(Vector2 enemyPos, float facingDir, float range,
            float duration, Color color, float slashHeight = 0.72f)
        {
            float width;
            Vector2 center;
            if (Mathf.Approximately(facingDir, 0f))
            {
                width = range * 2f;
                center = enemyPos;
            }
            else
            {
                width = range;
                center = enemyPos + new Vector2(facingDir * range * 0.5f, 0f);
            }

            var go = new GameObject("AttackSlashFX");
            go.transform.position = (Vector3)(Vector2)center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlaceholderSprite();
            sr.color = color;
            sr.sortingOrder = 55;
            go.transform.localScale = new Vector3(width, slashHeight, 1f);
            Object.Destroy(go, duration);
        }

        static Sprite MakeWhiteSprite() => CreatePlaceholderSprite();

        static SectionEnemyTuning DefaultEnemyTuning()
        {
            return new SectionEnemyTuning
            {
                maxHp = 38f,
                moveSpeed = 2.1f,
                attackDamage = 4.5f,
                attackCooldown = 0.85f,
                attackRange = 2.65f,
                xpReward = 14,
                visualScale = 0.95f,
                tint = new Color(0.16f, 0.16f, 0.19f),
                waveGap = 0.5f
            };
        }

        void BuildBackdrop(Sprite spr, string sceneName)
        {
            _backdropC = null;
            Color left, right;
            if (sceneName == "InkForest_03")
            {
                left = new Color(0.22f, 0.24f, 0.30f);
                right = new Color(0.18f, 0.20f, 0.26f);
            }
            else if (sceneName == "InkForest_02")
            {
                left = new Color(0.26f, 0.28f, 0.30f);
                right = new Color(0.24f, 0.26f, 0.28f);
            }
            else if (sceneName == "InkForest_01")
            {
                // 林缘：略亮灰绿，与 02/03 墨色递进
                left = new Color(0.36f, 0.39f, 0.37f);
                right = new Color(0.33f, 0.36f, 0.35f);
            }
            else if (sceneName == "EmberValley_01")
            {
                left = new Color(0.34f, 0.30f, 0.28f);
                right = new Color(0.27f, 0.24f, 0.24f);
            }
            else if (sceneName == "EmberValley_02")
            {
                left = new Color(0.26f, 0.22f, 0.22f);
                right = new Color(0.2f, 0.17f, 0.18f);
            }
            else if (sceneName == "EmberValley_03")
            {
                left = new Color(0.26f, 0.16f, 0.15f);
                right = new Color(0.2f, 0.13f, 0.14f);
            }
            else if (sceneName == "HeartRealm_01")
            {
                left = new Color(0.28f, 0.22f, 0.3f);
                right = new Color(0.22f, 0.2f, 0.26f);
            }
            else if (sceneName == "HeartRealm_02")
            {
                left = new Color(0.22f, 0.18f, 0.26f);
                right = new Color(0.17f, 0.14f, 0.22f);
            }
            else if (sceneName == "HeartRealm_03")
            {
                left = new Color(0.2f, 0.15f, 0.24f);
                right = new Color(0.16f, 0.12f, 0.2f);
            }
            else if (sceneName == "HeartRealm_04")
            {
                left = new Color(0.18f, 0.12f, 0.22f);
                right = new Color(0.14f, 0.1f, 0.18f);
            }
            else
            {
                float g = 0.34f;
                left = new Color(g, g + 0.02f, g + 0.04f);
                right = new Color(g - 0.02f, g, g + 0.02f);
            }

            float ax, ay, asx, asy, bx, by, bsx, bsy;
            if (sceneName == "InkForest_03" || sceneName == "EmberValley_03" || sceneName == "HeartRealm_03" ||
                sceneName == "HeartRealm_04")
            {
                bool emberBoss = sceneName == "EmberValley_03";
                bool heartBoss = sceneName == "HeartRealm_04";
                ax = -17.5f;
                ay = emberBoss || heartBoss ? 0.65f : 0.5f;
                asx = 25f;
                asy = emberBoss || heartBoss ? 15.6f : 14f;
                bx = 20f;
                by = emberBoss || heartBoss ? 1.85f : 1.65f;
                bsx = 29f;
                bsy = emberBoss || heartBoss ? 18.2f : 17f;
            }
            else if (sceneName == "EmberValley_01")
            {
                ax = -10f;
                ay = 0.35f;
                asx = 22.5f;
                asy = 11.2f;
                bx = 10.5f;
                by = 0.55f;
                bsx = 20f;
                bsy = 11.8f;
            }
            else if (sceneName == "EmberValley_02")
            {
                ax = -8.5f;
                ay = 0.15f;
                asx = 18.5f;
                asy = 11.5f;
                bx = 8.5f;
                by = 0.2f;
                bsx = 17f;
                bsy = 12f;
            }
            else if (sceneName == "HeartRealm_02")
            {
                ax = -8.5f;
                ay = 0.15f;
                asx = 18.5f;
                asy = 11.5f;
                bx = 8.5f;
                by = 0.2f;
                bsx = 17f;
                bsy = 12f;
            }
            else
            {
                ax = -11f;
                ay = -0.5f;
                asx = 22f;
                asy = 9f;
                bx = 12f;
                by = -0.2f;
                bsx = 18f;
                bsy = 7f;
            }

            if (backgroundSprite != null)
            {
                // 单张水墨背景模式：用 Sprite 原始宽高比覆盖整个游戏区域，不再分 L/R/Upper 拼接
                Vector2 spriteSize = backgroundSprite.bounds.size; // 单位：world units
                float sceneHalfWidth = sceneName == "InkForest_03" || sceneName == "EmberValley_03" ||
                                       sceneName == "HeartRealm_03" || sceneName == "HeartRealm_04"
                    ? 30f
                    : sceneName == "EmberValley_02" || sceneName == "HeartRealm_02"
                        ? 18.5f
                        : sceneName == "EmberValley_01"
                            ? 22f
                            : sceneName == "HeartRealm_01"
                                ? 25f
                                : 24f;
                float sceneHalfHeight = sceneName == "InkForest_03" || sceneName == "EmberValley_03" ||
                                        sceneName == "HeartRealm_03" || sceneName == "HeartRealm_04"
                    ? 11f
                    : sceneName == "EmberValley_02" || sceneName == "HeartRealm_02"
                        ? 10.5f
                        : sceneName == "EmberValley_01"
                            ? 10.2f
                            : sceneName == "HeartRealm_01"
                                ? 9.8f
                                : 7.5f;
                float scaleByW = (sceneHalfWidth * 2f) / Mathf.Max(0.01f, spriteSize.x);
                float scaleByH = (sceneHalfHeight * 2f) / Mathf.Max(0.01f, spriteSize.y);
                float fitScale = Mathf.Max(scaleByW, scaleByH); // 用较大值保证完全覆盖、不留空白边

                var a = new GameObject("BackdropL");
                a.transform.position = new Vector3(0f, (ay + by) * 0.5f, 0f);
                _backdropA = a.AddComponent<SpriteRenderer>();
                _backdropA.sprite = backgroundSprite;
                _backdropA.color = Color.white;
                _backdropA.sortingOrder = -12;
                a.transform.localScale = new Vector3(fitScale, fitScale, 1f);

                _backdropB = null;
                _backdropC = null;
                return; // 跳过 BackdropR / BackdropUpper 创建
            }

            var a2 = new GameObject("BackdropL");
            a2.transform.position = new Vector3(ax, ay, 0f);
            _backdropA = a2.AddComponent<SpriteRenderer>();
            _backdropA.sprite = spr;
            _backdropA.color = left;
            _backdropA.sortingOrder = -12;
            a2.transform.localScale = new Vector3(asx, asy, 1f);

            var b = new GameObject("BackdropR");
            b.transform.position = new Vector3(bx, by, 0f);
            _backdropB = b.AddComponent<SpriteRenderer>();
            _backdropB.sprite = spr;
            _backdropB.color = right;
            _backdropB.sortingOrder = -11;
            b.transform.localScale = new Vector3(bsx, bsy, 1f);

            if (sceneName == "InkForest_03" || sceneName == "EmberValley_03" || sceneName == "HeartRealm_03" ||
                sceneName == "HeartRealm_04")
            {
                var c = new GameObject("BackdropUpper");
                bool emberBoss = sceneName == "EmberValley_03";
                bool heartBoss = sceneName == "HeartRealm_04";
                c.transform.position = new Vector3(3f, emberBoss || heartBoss ? 8.4f : 7.8f, 0f);
                _backdropC = c.AddComponent<SpriteRenderer>();
                _backdropC.sprite = spr;
                _backdropC.color = emberBoss
                    ? new Color(0.18f, 0.1f, 0.12f)
                    : heartBoss
                        ? new Color(0.12f, 0.08f, 0.18f)
                        : new Color(0.15f, 0.14f, 0.24f);
                _backdropC.sortingOrder = -14;
                c.transform.localScale = new Vector3(36f, emberBoss || heartBoss ? 13.5f : 12f, 1f);
                if (backgroundSprite != null)
                {
                    _backdropC.sprite = backgroundSprite;
                    _backdropC.color = Color.white;
                }
            }
        }

        static void BuildGround(Sprite spr, float widthScale, string sceneName)
        {
            Color c = sceneName == "InkForest_03"
                ? new Color(0.16f, 0.17f, 0.22f)
                : sceneName == "InkForest_02"
                    ? new Color(0.19f, 0.20f, 0.22f)
                    : sceneName == "InkForest_01"
                        ? new Color(0.245f, 0.255f, 0.268f)
                        : sceneName == "EmberValley_01"
                            ? new Color(0.26f, 0.22f, 0.21f)
                            : sceneName == "EmberValley_02"
                                ? new Color(0.22f, 0.19f, 0.18f)
                        : sceneName == "EmberValley_03"
                            ? new Color(0.2f, 0.17f, 0.18f)
                            : sceneName == "HeartRealm_01"
                                ? new Color(0.24f, 0.2f, 0.26f)
                                : sceneName == "HeartRealm_02"
                                    ? new Color(0.21f, 0.18f, 0.24f)
                                    : sceneName == "HeartRealm_03"
                                        ? new Color(0.19f, 0.16f, 0.23f)
                                        : sceneName == "HeartRealm_04"
                                            ? new Color(0.17f, 0.14f, 0.21f)
                                            : new Color(0.22f, 0.23f, 0.25f);

            var g = new GameObject("Ground");
            g.tag = "Ground";
            g.transform.position = new Vector3(0f, sceneName == "HeartRealm_01" ? -4.22f : -4.25f, 0f);
            g.transform.localScale = new Vector3(widthScale, sceneName == "HeartRealm_01" ? 1.32f : 1.25f, 1f);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = c;
            sr.sortingOrder = -6;
            if (_instance != null && _instance.groundSprite != null)
            {
                sr.sprite = _instance.groundSprite;
                sr.color = Color.white;
            }
            g.AddComponent<BoxCollider2D>();
        }

        /// <summary>烬口：地面余烬带（触发伤害），视觉为半透明暗红裂纹。</summary>
        static void BuildEmberValley01CinderPatches(Sprite spr, float groundWidth)
        {
            var root = new GameObject("CinderHazardRoot").transform;

            void Patch(float centerX, float widthWorld, float heightWorld)
            {
                var go = new GameObject("CinderPatch");
                go.layer = 0;
                go.transform.SetParent(root);
                go.transform.position = new Vector3(centerX, -3.58f, 0f);
                go.transform.localScale = new Vector3(widthWorld, heightWorld, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.color = new Color(0.58f, 0.14f, 0.09f, 0.44f);
                sr.sortingOrder = -4;
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                go.AddComponent<EmberCinderZone>();
            }

            float half = groundWidth * 0.5f - 2.5f;
            Patch(-11f, 4.2f, 0.48f);
            Patch(1.2f, 5f, 0.48f);
            Patch(Mathf.Min(12.5f, half - 2f), 3.6f, 0.48f);
        }

        /// <summary>烬口：侧壁（可蹬墙），与纵向多层平台对齐。</summary>
        static void BuildEmberValley01SideArena(float groundWidth)
        {
            var root = new GameObject("Ember01SideWalls").transform;
            float half = groundWidth * 0.5f - 0.3f;
            const float midY = 1.45f;
            const float wallH = 20f;
            foreach (float sign in new[] { -1f, 1f })
            {
                var go = new GameObject(sign < 0f ? "Ember01Wall_L" : "Ember01Wall_R");
                go.tag = "Ground";
                go.transform.SetParent(root);
                go.transform.position = new Vector3(sign * half, midY, 0f);
                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.55f, wallH);
            }
        }

        static void BuildEmberValley02SideArena(float groundWidth)
        {
            var root = new GameObject("EmberNarrowWalls").transform;
            float half = groundWidth * 0.5f - 0.28f;
            const float midY = 2.05f;
            const float wallH = 24f;
            foreach (float sign in new[] { -1f, 1f })
            {
                var go = new GameObject(sign < 0f ? "EmberWall_L" : "EmberWall_R");
                go.tag = "Ground";
                go.transform.SetParent(root);
                go.transform.position = new Vector3(sign * half, midY, 0f);
                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.55f, wallH);
            }
        }

        /// <summary>焚道：少量贴地余烬带，与爆灰叠加施压。</summary>
        static void BuildEmberValley02ThinCinders(Sprite spr, float groundWidth)
        {
            var root = new GameObject("CinderHazardRoot_02").transform;

            void Patch(float centerX, float widthWorld, float heightWorld)
            {
                var go = new GameObject("CinderPatch");
                go.layer = 0;
                go.transform.SetParent(root);
                go.transform.position = new Vector3(centerX, -3.58f, 0f);
                go.transform.localScale = new Vector3(widthWorld, heightWorld, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.color = new Color(0.52f, 0.12f, 0.08f, 0.38f);
                sr.sortingOrder = -4;
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                go.AddComponent<EmberCinderZone>();
            }

            float half = groundWidth * 0.5f - 2f;
            Patch(-7f, 2.8f, 0.42f);
            Patch(Mathf.Min(7.5f, half - 1.5f), 2.6f, 0.42f);
        }

        struct EmberPlatformCinderDef
        {
            public float x, y, w, h;
            public bool enemySpawn;
        }

        /// <summary>焚道 / 焰心：非刷怪平台随机铺烬口；刷怪平台（与 <see cref="BuildPlatforms"/> registerSpawn 一致）永不铺烬。</summary>
        static void BuildEmberValley02PlatformCinders(Sprite spr)
        {
            var defs = new[]
            {
                new EmberPlatformCinderDef { x = -16f, y = -2.1f, w = 1.75f, h = 0.66f, enemySpawn = false },
                new EmberPlatformCinderDef { x = -5f, y = -1.2f, w = 1.8f, h = 0.65f, enemySpawn = false },
                new EmberPlatformCinderDef { x = 5f, y = -1.75f, w = 2f, h = 0.67f, enemySpawn = false },
                new EmberPlatformCinderDef { x = 15f, y = -2f, w = 1.75f, h = 0.66f, enemySpawn = false },
                new EmberPlatformCinderDef { x = -10f, y = -0.82f, w = 3.6f, h = 0.76f, enemySpawn = true },
                new EmberPlatformCinderDef { x = 4f, y = -0.72f, w = 3.5f, h = 0.76f, enemySpawn = true },
                new EmberPlatformCinderDef { x = -2.5f, y = 1.15f, w = 2.4f, h = 0.68f, enemySpawn = false },
                new EmberPlatformCinderDef { x = -14f, y = 2.05f, w = 2.2f, h = 0.66f, enemySpawn = false },
                new EmberPlatformCinderDef { x = 12f, y = 2f, w = 2.2f, h = 0.66f, enemySpawn = false },
                new EmberPlatformCinderDef { x = -6f, y = 2.9f, w = 3.4f, h = 0.74f, enemySpawn = true },
                new EmberPlatformCinderDef { x = 7f, y = 3.1f, w = 3.2f, h = 0.74f, enemySpawn = true },
                new EmberPlatformCinderDef { x = -12f, y = 4.35f, w = 1.65f, h = 0.60f, enemySpawn = false },
                new EmberPlatformCinderDef { x = 2f, y = 4.5f, w = 1.7f, h = 0.60f, enemySpawn = false },
                new EmberPlatformCinderDef { x = 13f, y = 4.25f, w = 1.65f, h = 0.60f, enemySpawn = false },
                new EmberPlatformCinderDef { x = -4f, y = 5.6f, w = 3.2f, h = 0.72f, enemySpawn = true },
                new EmberPlatformCinderDef { x = 8f, y = 5.85f, w = 3f, h = 0.72f, enemySpawn = true },
                new EmberPlatformCinderDef { x = 1f, y = 7f, w = 2.8f, h = 0.70f, enemySpawn = true },
            };
            BuildEmberValleyPlatformCindersRandom(spr, "02", defs, 0.58f,
                new Color(0.52f, 0.12f, 0.08f, 0.42f), 0.2f);
        }

        static void BuildEmberValley03PlatformCinders(Sprite spr)
        {
            var defs = new[]
            {
                new EmberPlatformCinderDef { x = -26f, y = -2.35f, w = 2.2f, h = 0.42f, enemySpawn = false },
                new EmberPlatformCinderDef { x = -19f, y = -1.85f, w = 1.9f, h = 0.38f, enemySpawn = false },
                new EmberPlatformCinderDef { x = 7.5f, y = -2.05f, w = 2.5f, h = 0.42f, enemySpawn = false },
                new EmberPlatformCinderDef { x = 27f, y = -1.55f, w = 2.3f, h = 0.40f, enemySpawn = false },
                new EmberPlatformCinderDef { x = -3f, y = -0.35f, w = 2f, h = 0.36f, enemySpawn = false },
                new EmberPlatformCinderDef { x = 4f, y = -0.65f, w = 2.3f, h = 0.38f, enemySpawn = false },
                new EmberPlatformCinderDef { x = 14f, y = -1.05f, w = 2.2f, h = 0.38f, enemySpawn = false },
                new EmberPlatformCinderDef { x = -23f, y = 0.1f, w = 1.55f, h = 0.34f, enemySpawn = false },
                new EmberPlatformCinderDef { x = -20f, y = 1f, w = 1.5f, h = 0.32f, enemySpawn = false },
                new EmberPlatformCinderDef { x = -17f, y = 1.9f, w = 1.45f, h = 0.32f, enemySpawn = false },
                new EmberPlatformCinderDef { x = -1f, y = 3.35f, w = 2.9f, h = 0.38f, enemySpawn = false },
                new EmberPlatformCinderDef { x = 7f, y = 4.4f, w = 2.7f, h = 0.36f, enemySpawn = false },
                new EmberPlatformCinderDef { x = 16f, y = 5.5f, w = 2.85f, h = 0.38f, enemySpawn = false },
                new EmberPlatformCinderDef { x = -21f, y = -1.05f, w = 4.2f, h = 0.44f, enemySpawn = true },
                new EmberPlatformCinderDef { x = -9f, y = 0.4f, w = 4.5f, h = 0.44f, enemySpawn = true },
                new EmberPlatformCinderDef { x = 3f, y = 1.85f, w = 4.6f, h = 0.44f, enemySpawn = true },
                new EmberPlatformCinderDef { x = 14f, y = 0.3f, w = 4.3f, h = 0.44f, enemySpawn = true },
                new EmberPlatformCinderDef { x = 23f, y = 2.7f, w = 4.5f, h = 0.44f, enemySpawn = true },
                new EmberPlatformCinderDef { x = -14f, y = 6.1f, w = 3.6f, h = 0.42f, enemySpawn = true },
                new EmberPlatformCinderDef { x = 2f, y = 6.45f, w = 3.8f, h = 0.42f, enemySpawn = true },
                new EmberPlatformCinderDef { x = 18f, y = 6.25f, w = 3.5f, h = 0.42f, enemySpawn = true },
                new EmberPlatformCinderDef { x = -6f, y = 7.85f, w = 3.4f, h = 0.40f, enemySpawn = true },
                new EmberPlatformCinderDef { x = 10f, y = 8.2f, w = 3.6f, h = 0.40f, enemySpawn = true },
                new EmberPlatformCinderDef { x = 22f, y = 7.95f, w = 3.2f, h = 0.40f, enemySpawn = true },
                new EmberPlatformCinderDef { x = -2f, y = 9.35f, w = 2.9f, h = 0.38f, enemySpawn = true },
            };
            BuildEmberValleyPlatformCindersRandom(spr, "03", defs, 0.55f,
                new Color(0.56f, 0.13f, 0.09f, 0.45f), 0.16f);
        }

        static void BuildEmberValleyPlatformCindersRandom(Sprite spr, string sceneKey,
            EmberPlatformCinderDef[] defs, float cinderChance, Color tint, float stripHeight)
        {
            var root = new GameObject($"CinderPlatformRoot_{sceneKey}").transform;
            const int sortOrder = 2;
            var rng = new System.Random($"EmberCinder_{sceneKey}".GetHashCode());

            void Strip(float platX, float platY, float platW, float platH, float widthMul, float xOffset = 0f)
            {
                float top = platY + platH * 0.5f;
                float stripW = Mathf.Max(0.28f, platW * widthMul);
                float cx = platX + xOffset;
                float cy = top - stripHeight * 0.5f + 0.02f;
                var go = new GameObject("CinderPlatformStrip");
                go.layer = 0;
                go.transform.SetParent(root);
                go.transform.position = new Vector3(cx, cy, 0f);
                go.transform.localScale = new Vector3(stripW, stripHeight, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.color = tint;
                sr.sortingOrder = sortOrder;
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                go.AddComponent<EmberCinderZone>();
            }

            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                if (d.enemySpawn) continue;
                if (rng.NextDouble() > cinderChance) continue;

                float widthMul = 0.32f + (float)rng.NextDouble() * 0.2f;
                bool wide = d.w >= 3f;
                if (wide && rng.NextDouble() < 0.42f)
                {
                    float off = d.w * (0.18f + (float)rng.NextDouble() * 0.08f);
                    Strip(d.x, d.y, d.w, d.h, widthMul, -off);
                    if (rng.NextDouble() < 0.65f)
                        Strip(d.x, d.y, d.w, d.h, widthMul * 0.92f, off);
                }
                else
                {
                    float xOff = ((float)rng.NextDouble() - 0.5f) * d.w * 0.14f;
                    Strip(d.x, d.y, d.w, d.h, widthMul, xOff);
                }
            }
        }

        /// <summary>树心关两侧实体挡墙（Ground），可蹬墙跳；与屏幕四边挡板不同，沿场地宽度固定。</summary>
        static void BuildInkForest03SideArena(float groundWidth, float midY = 2f, float wallH = 19f)
        {
            var root = new GameObject("ArenaSideWalls").transform;
            float half = groundWidth * 0.5f - 0.32f;
            foreach (float sign in new[] { -1f, 1f })
            {
                var go = new GameObject(sign < 0f ? "ArenaWall_L" : "ArenaWall_R");
                go.tag = "Ground";
                go.transform.SetParent(root);
                go.transform.position = new Vector3(sign * half, midY, 0f);
                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.62f, wallH);
            }
        }

        void SetupCameraFollowArena(GameObject player, float groundWidth, float minY, float maxY)
        {
            var cam = Camera.main;
            if (cam == null || player == null) return;

            var existing = cam.GetComponent<CameraFollowPlayer2D>();
            if (existing != null)
                Destroy(existing);

            float aspect = Mathf.Max(0.2f, cam.aspect);
            float halfH = cam.orthographicSize;
            float halfW = halfH * aspect;
            float halfArena = groundWidth * 0.5f;
            const float margin = 0.4f;

            var follow = cam.gameObject.AddComponent<CameraFollowPlayer2D>();
            follow.target = player.transform;
            follow.focusOffset = new Vector2(2.5f, 1.45f);
            follow.smoothTime = 0.06f;
            follow.minX = -halfArena + halfW - margin;
            follow.maxX = halfArena - halfW + margin;
            follow.minY = minY;
            follow.maxY = maxY;

            Vector3 p = player.transform.position + (Vector3)follow.focusOffset;
            p.z = cam.transform.position.z;
            p.x = Mathf.Clamp(p.x, follow.minX, follow.maxX);
            p.y = Mathf.Clamp(p.y, follow.minY, follow.maxY);
            cam.transform.position = p;
        }

        /// <summary>平台样式：色相与排序微调，第三关组合最多。</summary>
        enum PlatformStyleKind
        {
            Standard = 0,
            WideMoss = 1,
            CoolSlate = 2,
            NarrowRidge = 3
        }

        struct PlatformSpawnInfo
        {
            public Vector3 standWorld;
            public float patrolMinX;
            public float patrolMaxX;
        }

        /// <returns>平台刷怪位置与水平巡逻边界，供 <see cref="PlatformEnemySpawn"/> 与 AI 使用。</returns>
        static List<PlatformSpawnInfo> BuildPlatforms(Sprite spr, string sceneName)
        {
            var root = new GameObject("Platforms").transform;
            Color baseTint = sceneName == "EmberValley_03"
                ? new Color(0.34f, 0.28f, 0.28f)
                : sceneName == "EmberValley_02"
                    ? new Color(0.36f, 0.30f, 0.28f)
                    : sceneName == "EmberValley_01"
                        ? new Color(0.40f, 0.34f, 0.32f)
                        : sceneName == "HeartRealm_04"
                            ? new Color(0.32f, 0.28f, 0.38f)
                            : sceneName == "HeartRealm_03"
                                ? new Color(0.34f, 0.3f, 0.4f)
                                : sceneName == "HeartRealm_02"
                                    ? new Color(0.36f, 0.32f, 0.4f)
                                    : sceneName == "HeartRealm_01"
                                        ? new Color(0.38f, 0.34f, 0.42f)
                                        : sceneName == "InkForest_03"
                                            ? new Color(0.28f, 0.30f, 0.36f)
                                            : sceneName == "InkForest_02"
                                                ? new Color(0.32f, 0.33f, 0.36f)
                                                : new Color(0.36f, 0.37f, 0.40f);

            var stands = new List<PlatformSpawnInfo>();

            Color StyleColor(Color @base, PlatformStyleKind style)
            {
                switch (style)
                {
                    case PlatformStyleKind.WideMoss:
                        return new Color(@base.r * 0.88f, @base.g * 1.08f, @base.b * 0.95f);
                    case PlatformStyleKind.CoolSlate:
                        return new Color(@base.r * 0.92f, @base.g * 0.96f, @base.b * 1.12f);
                    case PlatformStyleKind.NarrowRidge:
                        return new Color(@base.r * 1.05f, @base.g * 1.02f, @base.b * 0.98f);
                    default:
                        return @base;
                }
            }

            int StyleSort(PlatformStyleKind style)
            {
                switch (style)
                {
                    case PlatformStyleKind.WideMoss: return -6;
                    case PlatformStyleKind.CoolSlate: return -4;
                    case PlatformStyleKind.NarrowRidge: return -5;
                    default: return -5;
                }
            }

            void Plank(float x, float y, float w, float h, PlatformStyleKind style, bool registerSpawn)
            {
                var go = new GameObject(registerSpawn ? "SpawnPlatform" : "Platform");
                go.tag = "Ground";
                go.transform.SetParent(root);
                go.transform.position = new Vector3(x, y, 0f);
                go.transform.localScale = new Vector3(w, h, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.color = StyleColor(baseTint, style);
                sr.sortingOrder = StyleSort(style);
                if (_instance != null && _instance.platformSprite != null)
                {
                    sr.sprite = _instance.platformSprite;
                    sr.color = Color.white;
                }
                go.AddComponent<BoxCollider2D>();
                if (!registerSpawn) return;

                const float enemyFeetFromCenter = 0.48f;
                float surfaceY = y + h * 0.5f;
                const float edgeInset = 0.3f;
                float halfW = w * 0.5f;
                float minX = x - halfW + edgeInset;
                float maxX = x + halfW - edgeInset;
                if (maxX > minX)
                    stands.Add(new PlatformSpawnInfo
                    {
                        standWorld = new Vector3(x, surfaceY + enemyFeetFromCenter, 0f),
                        patrolMinX = minX,
                        patrolMaxX = maxX
                    });
            }

            // 装饰台较窄；仅「长刷怪台」注册刷怪点（明显长于其他台子）
            switch (sceneName)
            {
                case "EmberValley_02":
                    Plank(-16f, -2.1f, 1.75f, 0.66f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-5f,  -1.2f, 1.8f, 0.65f, PlatformStyleKind.Standard,    false);
                    Plank(5f,   -1.75f, 2f, 0.67f, PlatformStyleKind.CoolSlate,   false);
                    Plank(15f,  -2f, 1.75f, 0.66f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-10f, -0.82f, 3.6f, 0.76f, PlatformStyleKind.WideMoss,    true);
                    Plank(4f,   -0.72f, 3.5f, 0.76f, PlatformStyleKind.CoolSlate,   true);
                    Plank(-2.5f, 1.15f, 2.4f, 0.68f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-14f, 2.05f, 2.2f, 0.66f, PlatformStyleKind.Standard,    false);
                    Plank(12f,  2f, 2.2f, 0.66f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-6f,  2.9f, 3.4f, 0.74f, PlatformStyleKind.WideMoss,      true);
                    Plank(7f,   3.1f, 3.2f, 0.74f, PlatformStyleKind.CoolSlate,     true);
                    Plank(-12f, 4.35f, 1.65f, 0.60f, PlatformStyleKind.NarrowRidge, false);
                    Plank(2f,   4.5f, 1.7f, 0.60f, PlatformStyleKind.NarrowRidge,  false);
                    Plank(13f,  4.25f, 1.65f, 0.60f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-4f,  5.6f, 3.2f, 0.72f, PlatformStyleKind.Standard,     true);
                    Plank(8f,   5.85f, 3f, 0.72f, PlatformStyleKind.WideMoss,       true);
                    Plank(1f,   7f, 2.8f, 0.70f, PlatformStyleKind.CoolSlate,    true);
                    break;
                case "EmberValley_01":
                    Plank(-18f, -2.05f, 2.2f, 0.70f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-7f,  -1.15f, 2f, 0.68f, PlatformStyleKind.Standard,    false);
                    Plank(6f,   -1.65f, 2.3f, 0.70f, PlatformStyleKind.CoolSlate,   false);
                    Plank(17f,  -1.95f, 2.1f, 0.68f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-12f, -0.95f, 4.4f, 0.78f, PlatformStyleKind.WideMoss,    true);
                    Plank(6f,   -0.78f, 4.6f, 0.78f, PlatformStyleKind.CoolSlate,   true);
                    Plank(-15.5f, 1.5f, 2f, 0.64f, PlatformStyleKind.NarrowRidge, false);
                    Plank(14.5f, 1.35f, 2f, 0.64f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-5f,   2.25f, 4.2f, 0.76f, PlatformStyleKind.Standard,    true);
                    Plank(10f,  2.45f, 4f, 0.76f, PlatformStyleKind.WideMoss,      true);
                    Plank(-16f, 4f, 1.85f, 0.62f, PlatformStyleKind.NarrowRidge, false);
                    Plank(2f,   4.15f, 1.9f, 0.62f, PlatformStyleKind.NarrowRidge, false);
                    Plank(16f,  4.05f, 1.85f, 0.62f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-8f,  5.8f, 4.5f, 0.74f, PlatformStyleKind.CoolSlate,   true);
                    Plank(6f,   6f, 4.3f, 0.74f, PlatformStyleKind.WideMoss,    true);
                    Plank(-2f,  7.35f, 3.2f, 0.70f, PlatformStyleKind.Standard,    true);
                    break;
                case "InkForest_01":
                    Plank(-15f, -2f,   2.5f, 0.75f, PlatformStyleKind.Standard,    false);
                    Plank(-3f,  -1.05f, 2.4f, 0.72f, PlatformStyleKind.CoolSlate,   false);
                    Plank(12f,  -1.72f, 2.6f, 0.78f, PlatformStyleKind.WideMoss,    false);
                    Plank(-11.5f,-1.02f,4.85f,0.80f, PlatformStyleKind.WideMoss,    true);
                    Plank(5.5f, -0.88f, 5.2f, 0.78f, PlatformStyleKind.CoolSlate,   true);
                    break;
                case "InkForest_02":
                    Plank(-17f, -1.9f,  2.5f, 0.75f, PlatformStyleKind.Standard,    false);
                    Plank(-5f,  -0.85f, 2.5f, 0.72f, PlatformStyleKind.NarrowRidge, false);
                    Plank(6f,   -1.82f, 2.6f, 0.78f, PlatformStyleKind.WideMoss,    false);
                    Plank(17f,  -0.5f,  2.4f, 0.72f, PlatformStyleKind.CoolSlate,   false);
                    Plank(-12f, -1.08f, 5f,   0.82f, PlatformStyleKind.WideMoss,    true);
                    Plank(1f,   -0.62f, 5.4f, 0.80f, PlatformStyleKind.CoolSlate,   true);
                    Plank(14f,  -1.35f, 4.7f, 0.80f, PlatformStyleKind.Standard,    true);
                    break;
                case "EmberValley_03":
                    Plank(-26f, -2.35f, 2.2f, 0.42f, PlatformStyleKind.Standard,    false);
                    Plank(-19f, -1.85f, 1.9f, 0.38f, PlatformStyleKind.NarrowRidge, false);
                    Plank(7.5f, -2.05f, 2.5f, 0.42f, PlatformStyleKind.CoolSlate,   false);
                    Plank(27f,  -1.55f, 2.3f, 0.40f, PlatformStyleKind.WideMoss,    false);
                    Plank(-3f,  -0.35f, 2f,   0.36f, PlatformStyleKind.NarrowRidge, false);
                    Plank(4f,   -0.65f, 2.3f, 0.38f, PlatformStyleKind.Standard,    false);
                    Plank(14f,  -1.05f, 2.2f, 0.38f, PlatformStyleKind.CoolSlate,   false);
                    Plank(-23f, 0.1f,   1.55f, 0.34f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-20f, 1f,     1.5f, 0.32f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-17f, 1.9f,   1.45f, 0.32f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-1f,  3.35f,  2.9f, 0.38f, PlatformStyleKind.CoolSlate,   false);
                    Plank(7f,   4.4f,   2.7f, 0.36f, PlatformStyleKind.NarrowRidge, false);
                    Plank(16f,  5.5f,   2.85f, 0.38f, PlatformStyleKind.WideMoss,    false);
                    Plank(-21f, -1.05f, 4.2f, 0.44f, PlatformStyleKind.WideMoss,    true);
                    Plank(-9f,  0.4f,   4.5f, 0.44f, PlatformStyleKind.CoolSlate,   true);
                    Plank(3f,   1.85f,  4.6f, 0.44f, PlatformStyleKind.Standard,    true);
                    Plank(14f,  0.3f,   4.3f, 0.44f, PlatformStyleKind.WideMoss,    true);
                    Plank(23f,  2.7f,   4.5f, 0.44f, PlatformStyleKind.CoolSlate,   true);
                    Plank(-14f, 6.1f,   3.6f, 0.42f, PlatformStyleKind.NarrowRidge, true);
                    Plank(2f,   6.45f,  3.8f, 0.42f, PlatformStyleKind.WideMoss,    true);
                    Plank(18f,  6.25f,  3.5f, 0.42f, PlatformStyleKind.CoolSlate,   true);
                    Plank(-6f,  7.85f,  3.4f, 0.40f, PlatformStyleKind.Standard,    true);
                    Plank(10f,  8.2f,   3.6f, 0.40f, PlatformStyleKind.WideMoss,    true);
                    Plank(22f,  7.95f,  3.2f, 0.40f, PlatformStyleKind.CoolSlate,   true);
                    Plank(-2f,  9.35f,  2.9f, 0.38f, PlatformStyleKind.NarrowRidge, true);
                    break;
                case "InkForest_03":
                    // 树心区（再收窄，地面半宽约 29）
                    Plank(-26f, -2.35f, 2.2f, 0.42f, PlatformStyleKind.Standard,    false);
                    Plank(-19f, -1.85f, 1.9f, 0.38f, PlatformStyleKind.NarrowRidge, false);
                    Plank(7.5f, -2.05f, 2.5f, 0.42f, PlatformStyleKind.CoolSlate,   false);
                    Plank(27f,  -1.55f, 2.3f, 0.40f, PlatformStyleKind.WideMoss,    false);
                    Plank(-3f,  -0.35f, 2f,   0.36f, PlatformStyleKind.NarrowRidge, false);
                    Plank(4f,   -0.65f, 2.3f, 0.38f, PlatformStyleKind.Standard,    false);
                    Plank(14f,  -1.05f, 2.2f, 0.38f, PlatformStyleKind.CoolSlate,   false);
                    Plank(-23f, 0.1f,   1.55f, 0.34f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-20f, 1f,     1.5f, 0.32f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-17f, 1.9f,   1.45f, 0.32f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-1f,  3.35f,  2.9f, 0.38f, PlatformStyleKind.CoolSlate,   false);
                    Plank(7f,   4.4f,   2.7f, 0.36f, PlatformStyleKind.NarrowRidge, false);
                    Plank(16f,  5.5f,   2.85f, 0.38f, PlatformStyleKind.WideMoss,    false);
                    Plank(-21f, -1.05f, 4.2f, 0.44f, PlatformStyleKind.WideMoss,    true);
                    Plank(-9f,  0.4f,   4.5f, 0.44f, PlatformStyleKind.CoolSlate,   true);
                    Plank(3f,   1.85f,  4.6f, 0.44f, PlatformStyleKind.Standard,    true);
                    Plank(14f,  0.3f,   4.3f, 0.44f, PlatformStyleKind.WideMoss,    true);
                    Plank(23f,  2.7f,   4.5f, 0.44f, PlatformStyleKind.CoolSlate,   true);
                    break;
                case "HeartRealm_01":
                    Plank(-18f, -2.05f, 2.2f, 0.70f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-7f,  -1.15f, 2f, 0.68f, PlatformStyleKind.Standard,    false);
                    Plank(7f,   -1.65f, 2.3f, 0.70f, PlatformStyleKind.CoolSlate,   false);
                    Plank(18f,  -1.95f, 2.1f, 0.68f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-12f, -0.95f, 4.4f, 0.78f, PlatformStyleKind.WideMoss,    true);
                    Plank(8f,   -0.78f, 4.6f, 0.78f, PlatformStyleKind.CoolSlate,   true);
                    Plank(-5f,   2.25f, 4.2f, 0.76f, PlatformStyleKind.Standard,    true);
                    Plank(10f,  2.45f, 4f, 0.76f, PlatformStyleKind.WideMoss,      true);
                    Plank(-8f,  5.8f, 4.5f, 0.74f, PlatformStyleKind.CoolSlate,   true);
                    break;
                case "HeartRealm_02":
                    // 裂帛廊：少量空中窄台 + 带巡逻的刷怪台，与倒置 ∧ 脊、链桥错层，避免再堆焚道式满屏大台。
                    Plank(-13.5f, 0.28f, 1.65f, 0.36f, PlatformStyleKind.NarrowRidge, false);
                    Plank(13.2f, 0.28f, 1.65f, 0.36f, PlatformStyleKind.NarrowRidge, false);
                    Plank(0f, 2.35f, 2.5f, 0.38f, PlatformStyleKind.Standard, true);
                    Plank(-8.2f, 3.95f, 2f, 0.36f, PlatformStyleKind.CoolSlate, true);
                    Plank(8.4f, 3.95f, 2f, 0.36f, PlatformStyleKind.WideMoss, true);
                    break;
                case "HeartRealm_03":
                case "HeartRealm_04":
                    Plank(-26f, -2.35f, 2.2f, 0.42f, PlatformStyleKind.Standard,    false);
                    Plank(-19f, -1.85f, 1.9f, 0.38f, PlatformStyleKind.NarrowRidge, false);
                    Plank(7.5f, -2.05f, 2.5f, 0.42f, PlatformStyleKind.CoolSlate,   false);
                    Plank(27f,  -1.55f, 2.3f, 0.40f, PlatformStyleKind.WideMoss,    false);
                    Plank(-3f,  -0.35f, 2f,   0.36f, PlatformStyleKind.NarrowRidge, false);
                    Plank(4f,   -0.65f, 2.3f, 0.38f, PlatformStyleKind.Standard,    false);
                    Plank(14f,  -1.05f, 2.2f, 0.38f, PlatformStyleKind.CoolSlate,   false);
                    Plank(-23f, 0.1f,   1.55f, 0.34f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-20f, 1f,     1.5f, 0.32f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-17f, 1.9f,   1.45f, 0.32f, PlatformStyleKind.NarrowRidge, false);
                    Plank(-1f,  3.35f,  2.9f, 0.38f, PlatformStyleKind.CoolSlate,   false);
                    Plank(7f,   4.4f,   2.7f, 0.36f, PlatformStyleKind.NarrowRidge, false);
                    Plank(16f,  5.5f,   2.85f, 0.38f, PlatformStyleKind.WideMoss,    false);
                    Plank(-21f, -1.05f, 4.2f, 0.44f, PlatformStyleKind.WideMoss,    true);
                    Plank(-9f,  0.4f,   4.5f, 0.44f, PlatformStyleKind.CoolSlate,   true);
                    Plank(3f,   1.85f,  4.6f, 0.44f, PlatformStyleKind.Standard,    true);
                    Plank(14f,  0.3f,   4.3f, 0.44f, PlatformStyleKind.WideMoss,    true);
                    Plank(23f,  2.7f,   4.5f, 0.44f, PlatformStyleKind.CoolSlate,   true);
                    Plank(-14f, 6.1f,   3.6f, 0.42f, PlatformStyleKind.NarrowRidge, true);
                    Plank(2f,   6.45f,  3.8f, 0.42f, PlatformStyleKind.WideMoss,    true);
                    Plank(18f,  6.25f,  3.5f, 0.42f, PlatformStyleKind.CoolSlate,   true);
                    Plank(-6f,  7.85f,  3.4f, 0.40f, PlatformStyleKind.Standard,    true);
                    Plank(10f,  8.2f,   3.6f, 0.40f, PlatformStyleKind.WideMoss,    true);
                    Plank(22f,  7.95f,  3.2f, 0.40f, PlatformStyleKind.CoolSlate,   true);
                    Plank(-2f,  9.35f,  2.9f, 0.38f, PlatformStyleKind.NarrowRidge, true);
                    break;
            }

            return stands;
        }

        static List<Transform> CreateSpawnTransforms(Transform parent, List<PlatformSpawnInfo> infos)
        {
            var list = new List<Transform>();
            if (parent == null || infos == null) return list;
            for (int i = 0; i < infos.Count; i++)
            {
                var sp = new GameObject("Spawn_Platform_" + i).transform;
                sp.SetParent(parent);
                sp.position = infos[i].standWorld;
                var marker = sp.gameObject.AddComponent<PlatformEnemySpawn>();
                marker.patrolMinX = infos[i].patrolMinX;
                marker.patrolMaxX = infos[i].patrolMaxX;
                list.Add(sp);
            }

            return list;
        }

        /// <summary>地面与平台刷点交错排列，波次按序号轮询时更容易在平台上刷怪。</summary>
        static List<Transform> InterleaveSpawnLists(List<Transform> ground, List<Transform> platform)
        {
            var merged = new List<Transform>(ground.Count + platform.Count);
            int i = 0, j = 0;
            while (i < ground.Count || j < platform.Count)
            {
                if (i < ground.Count) merged.Add(ground[i++]);
                if (j < platform.Count) merged.Add(platform[j++]);
            }

            return merged;
        }

        static GameObject BuildPlayer(Sprite spr)
        {
            var p = new GameObject("Player");
            p.tag = "Player";
            p.transform.position = new Vector3(-7f, -2.85f, 0f);
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = new Color(0.88f, 0.9f, 0.92f);
            sr.sortingOrder = 10;
            p.transform.localScale = new Vector3(0.7f, 1.15f, 1f);
            if (_instance != null && _instance.playerSprite != null)
            {
                sr.sprite = _instance.playerSprite;
                sr.color = Color.white;
                p.transform.localScale = Vector3.one;
            }
            if (_instance != null && (_instance.bareController != null || _instance.playerController != null))
            {
                var anim = p.AddComponent<Animator>();
                // 默认形态为赤手空拳；剑 / 枪需通过拾取解锁。bareController 缺失时回退到剑控制器，
                // 避免出现没有 Animator Controller 的空状态。
                anim.runtimeAnimatorController = _instance.bareController != null
                    ? _instance.bareController
                    : _instance.playerController;
                anim.applyRootMotion = false;
            }
            var rb = p.AddComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var box = p.AddComponent<BoxCollider2D>();
            if (_instance != null && _instance.playerSprite != null)
            {
                box.size = new Vector2(0.9f, 1.5f);
                box.offset = new Vector2(0f, -0.5f);
            }
            else
            {
                box.size = new Vector2(0.55f, 0.95f);
            }
            var pm = p.AddComponent<PlayerMovement>();
            pm.moveSpeed = 7.2f;
            pm.jumpForce = 14.5f;
            if (_instance != null && _instance.playerSprite != null && pm.groundCheck != null)
            {
                pm.groundCheck.localPosition = new Vector3(0f, box.offset.y - box.size.y * 0.5f, 0f);
            }
            p.AddComponent<EquipmentHolder>();
            var ps = p.AddComponent<PlayerStats>();
            ps.xpPerLevel = new[] { 45, 80, 130, 200, 300 };
            p.AddComponent<PlayerCombat>();
            var clamp = p.AddComponent<ClampToWorldBounds2D>();
            clamp.halfWidthPad = 0.22f;
            clamp.halfHeightPad = 0.52f;
            clamp.skipClampWhenOutsideViewport = false;
            return p;
        }

        struct GearSet
        {
            public EquipmentData whiteA;
            public EquipmentData whiteB;
            public EquipmentData colorBoss;
        }

        static GearSet BuildEquipment()
        {
            var w1 = ScriptableObject.CreateInstance<EquipmentData>();
            w1.displayName = "白毫短笔";
            w1.slot = EquipmentSlot.Weapon;
            w1.attackBonus = 3;
            w1.hpBonus = 0;
            w1.isColorGear = false;
            w1.visualTint = new Color(0.75f, 0.75f, 0.78f);

            var w2 = ScriptableObject.CreateInstance<EquipmentData>();
            w2.displayName = "素绢短衫";
            w2.slot = EquipmentSlot.Armor;
            w2.attackBonus = 0;
            w2.hpBonus = 18;
            w2.isColorGear = false;
            w2.visualTint = new Color(0.7f, 0.72f, 0.76f);

            var c = ScriptableObject.CreateInstance<EquipmentData>();
            c.displayName = "翠色·树心笔";
            c.slot = EquipmentSlot.Weapon;
            c.attackBonus = 12;
            c.hpBonus = 6;
            c.isColorGear = true;
            c.visualTint = new Color(0.35f, 0.85f, 0.45f);

            return new GearSet { whiteA = w1, whiteB = w2, colorBoss = c };
        }

        static List<Transform> BuildSpawnPoints(int count, float horizontalSpanOverride = -1f)
        {
            var root = new GameObject("SpawnPoints").transform;
            var list = new List<Transform>();
            float span = horizontalSpanOverride >= 0f ? horizontalSpanOverride : (count >= 4 ? 7.5f : 4.8f);
            for (int i = 0; i < count; i++)
            {
                float u = count <= 1 ? 0f : (i / (float)(count - 1)) * 2f - 1f;
                var sp = new GameObject("Spawn_" + i).transform;
                sp.SetParent(root);
                sp.position = new Vector3(u * (span * 0.5f) + 0.8f, -2.95f, 0f);
                list.Add(sp);
            }

            return list;
        }

        static SimpleEnemy CreateEnemy(Transform spawnPoint, Sprite white, EquipmentData a, EquipmentData b, SectionEnemyTuning t)
        {
            var root = new GameObject("InkCritter");
            root.transform.position = spawnPoint.position;
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = white;
            sr.color = t.tint;
            sr.sortingOrder = 5;
            float s = t.visualScale;
            root.transform.localScale = new Vector3(s, s, 1f);
            if (_instance != null && _instance.enemySprite != null)
            {
                sr.sprite = _instance.enemySprite;
                sr.color = Color.white;
                root.transform.localScale = new Vector3(s * 0.6f, s * 0.6f, 1f);
                var pos = root.transform.position;
                pos.y += 0.25f;
                root.transform.position = pos;
            }
            var se = root.AddComponent<SimpleEnemy>();
            se.whiteDropA = a;
            se.whiteDropB = b;
            se.maxHp = t.maxHp;
            se.moveSpeed = t.moveSpeed;
            se.attackDamage = t.attackDamage;
            se.attackCooldown = t.attackCooldown;
            se.attackRange = t.attackRange;
            se.xpReward = t.xpReward;

            var hbGo = new GameObject("Hurtbox");
            hbGo.transform.SetParent(root.transform, false);
            var col = hbGo.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.48f;
            var hb = hbGo.AddComponent<Hurtbox>();
            hb.Configure(se);
            var plat = spawnPoint.GetComponent<PlatformEnemySpawn>();
            if (plat != null)
                se.SetPlatformPatrol(plat.patrolMinX, plat.patrolMaxX);
            var ec = root.AddComponent<ClampToWorldBounds2D>();
            ec.halfWidthPad = 0.52f;
            ec.halfHeightPad = 0.55f;
            ec.clampVertical = false;
            return se;
        }

        static GameObject CreateBossTemplate(Sprite white, EquipmentData colorDrop, SectionBossTuning t)
        {
            var root = new GameObject("BossInkTree_Template");
            root.transform.position = Vector3.zero;
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = white;
            sr.color = new Color(0.11f, 0.11f, 0.13f);
            sr.sortingOrder = 8;
            // 调大体型：~30%，呈现更具压迫感的 Boss。BossInkTree.Start 会自动落地，避免穿地。
            root.transform.localScale = new Vector3(2.85f, 3.30f, 1f);
            if (_instance != null && _instance.bossSprite != null)
            {
                sr.sprite = _instance.bossSprite;
                sr.color = Color.white;
            }
            var boss = root.AddComponent<BossInkTree>();
            boss.colorGearDrop = colorDrop;
            boss.maxHp = t.maxHp;
            boss.moveSpeed = t.moveSpeed;
            boss.slamDamage = t.slamDamage;

            var hbGo = new GameObject("Hurtbox");
            hbGo.transform.SetParent(root.transform, false);
            var c = hbGo.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            // 受击半径相对缩小以适配更大躯干（lossyScale 已放大）
            c.radius = 0.42f;
            var hb = hbGo.AddComponent<Hurtbox>();
            hb.Configure(boss);
            var bc = root.AddComponent<ClampToWorldBounds2D>();
            bc.halfWidthPad = 1.4f;
            bc.halfHeightPad = 1.65f;
            bc.skipClampWhenOutsideViewport = false;
            bc.clampVertical = false;
            return root;
        }

        Image CreateFullscreenTintImage()
        {
            var go = new GameObject("RestoreTintCanvas");
            var cv = go.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 250;
            go.AddComponent<GraphicRaycaster>();
            var imgGo = new GameObject("Tint");
            imgGo.transform.SetParent(go.transform, false);
            var rect = imgGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = imgGo.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.14f, 0f);
            img.raycastTarget = false;
            return img;
        }

        static Font BuiltinFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        void BuildHud(GameObject player)
        {
            var go = new GameObject("HUDCanvas");
            var cv = go.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 120;
            go.AddComponent<GraphicRaycaster>();
            var hud = go.AddComponent<HUDView>();

            var hpSlider = CreateHealthSlider(go.transform);
            hud.healthSlider = hpSlider;

            hud.levelText = CreateHudText(go.transform, "LevelText", "Lv.1", 22, new Vector2(0.05f, 0.86f), new Vector2(160f, 36f),
                TextAnchor.UpperLeft);

            hud.jAttackText = CreateHudText(go.transform, "JHint", "拳·普攻 J：无冷却（三段循环）  [1 拳  2 剑✗  3 枪✗]", 17, new Vector2(0.05f, 0.805f), new Vector2(620f, 28f),
                TextAnchor.UpperLeft);

            hud.kCooldownText = CreateHudText(go.transform, "KCD", "墨爆 K: 就绪", 18, new Vector2(0.05f, 0.745f), new Vector2(420f, 32f),
                TextAnchor.UpperLeft);

            hud.lCooldownText = CreateHudText(go.transform, "LCD", "三连 L: 就绪", 18, new Vector2(0.05f, 0.685f), new Vector2(420f, 32f),
                TextAnchor.UpperLeft);

            hud.dashCooldownText = CreateHudText(go.transform, "DashCD", "冲刺 双击←/→: 就绪", 18, new Vector2(0.05f, 0.625f), new Vector2(460f, 32f),
                TextAnchor.UpperLeft);

            hud.Bind(player.GetComponent<PlayerStats>(), player.GetComponent<PlayerCombat>(), player.GetComponent<PlayerMovement>());
        }

        static Slider CreateHealthSlider(Transform parent)
        {
            var root = new GameObject("HealthSlider");
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.9f);
            rt.anchorMax = new Vector2(0.38f, 0.9f);
            rt.sizeDelta = new Vector2(0f, 22f);

            var bg = new GameObject("Background");
            bg.transform.SetParent(root.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.75f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(root.transform, false);
            var faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.offsetMin = new Vector2(3f, 3f);
            faRt.offsetMax = new Vector2(-3f, -3f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillArea.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.45f, 0.78f, 0.5f, 0.98f);

            var slider = root.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.targetGraphic = fillImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;
            return slider;
        }

        static Text CreateHudText(Transform parent, string name, string text, int size, Vector2 anchor, Vector2 sizeDelta,
            TextAnchor align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = sizeDelta;
            var t = go.AddComponent<Text>();
            t.font = BuiltinFont();
            t.fontSize = size;
            t.alignment = align;
            t.color = new Color(0.08f, 0.08f, 0.10f);
            t.text = text;
            return t;
        }
    }
}
