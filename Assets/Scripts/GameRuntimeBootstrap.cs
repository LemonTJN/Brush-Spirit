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

        void Awake()
        {
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
            SectionEnemyTuning enemyTune = DefaultEnemyTuning();
            SectionBossTuning bossTune = new SectionBossTuning
            {
                maxHp = 210f,
                moveSpeed = 2.6f,
                slamDamage = 26f
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
                        maxHp = 260f,
                        moveSpeed = 2.85f,
                        slamDamage = 32f
                    };
                    break;
            }

            BuildBackdrop(spr, sn);
            BuildGround(spr, groundWidth, sn);
            if (sn == "InkForest_03")
                BuildInkForest03SideArena(groundWidth);
            var platformSpawnInfos = BuildPlatforms(spr, sn);
            GameObject player = GetOrCreatePlayer(spr, playerSpawn);
            var gear = BuildEquipment();
            float spawnSpan = sn == "InkForest_03" ? groundWidth * 0.58f : -1f;
            var groundSpawns = BuildSpawnPoints(spawnPointCount, spawnSpan);
            var platformSpawns = CreateSpawnTransforms(groundSpawns[0].parent, platformSpawnInfos);
            var spawns = InterleaveSpawnLists(groundSpawns, platformSpawns);

            var waveRoot = new GameObject("WaveRoot");
            var wave = waveRoot.AddComponent<WaveSpawner>();
            wave.enemiesPerWave = waveCounts;
            wave.delayBetweenWaves = enemyTune.waveGap;
            foreach (var s in spawns)
                wave.spawnPoints.Add(s);
            var tuneCopy = enemyTune;
            wave.EnemyFactory = sp => CreateEnemy(sp, spr, gear.whiteA, gear.whiteB, tuneCopy);

            var levelRoot = new GameObject("LevelRoot");
            var level = levelRoot.AddComponent<LevelController>();
            level.waves = wave;
            var bossSpawn = new GameObject("BossSpawn").transform;
            bossSpawn.position = new Vector3(bossSpawnX, -2.95f, 0f);
            level.bossSpawnPoint = bossSpawn;
            level.nextSceneAfterWaves = hasBoss ? "" : nextScene;
            level.sectionClearTitle = clearTitle;
            level.sectionClearSubtitle = clearSub;

            if (sn == "InkForest_01")
            {
                level.deferWaveStart = true;
                levelRoot.AddComponent<InkForest01Director>();
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
                var vicGo = new GameObject("VictoryUI");
                vicGo.AddComponent<VictoryPanel>();
            }
            else
            {
                level.bossTemplate = null;
                level.colorRestore = null;
            }

            BuildHud(player);

            float cameraOrtho = sn == "InkForest_03" ? 6.75f : 6.5f;
            MainCameraEnsure.Ensure(new Color(0.16f, 0.17f, 0.19f), cameraOrtho);
            if (sn == "InkForest_03")
                SetupInkForest03CameraFollow(player, groundWidth);
            else
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 p = player.transform.position;
                    cam.transform.position = new Vector3(p.x + 2.5f, p.y + 1.2f, -10f);
                }
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
            else
            {
                float g = 0.34f;
                left = new Color(g, g + 0.02f, g + 0.04f);
                right = new Color(g - 0.02f, g, g + 0.02f);
            }

            float ax, ay, asx, asy, bx, by, bsx, bsy;
            if (sceneName == "InkForest_03")
            {
                ax = -17.5f;
                ay = 0.5f;
                asx = 25f;
                asy = 14f;
                bx = 20f;
                by = 1.65f;
                bsx = 29f;
                bsy = 17f;
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

            var a = new GameObject("BackdropL");
            a.transform.position = new Vector3(ax, ay, 0f);
            _backdropA = a.AddComponent<SpriteRenderer>();
            _backdropA.sprite = spr;
            _backdropA.color = left;
            _backdropA.sortingOrder = -12;
            a.transform.localScale = new Vector3(asx, asy, 1f);

            var b = new GameObject("BackdropR");
            b.transform.position = new Vector3(bx, by, 0f);
            _backdropB = b.AddComponent<SpriteRenderer>();
            _backdropB.sprite = spr;
            _backdropB.color = right;
            _backdropB.sortingOrder = -11;
            b.transform.localScale = new Vector3(bsx, bsy, 1f);

            if (sceneName == "InkForest_03")
            {
                var c = new GameObject("BackdropUpper");
                c.transform.position = new Vector3(3f, 7.8f, 0f);
                _backdropC = c.AddComponent<SpriteRenderer>();
                _backdropC.sprite = spr;
                _backdropC.color = new Color(0.16f, 0.18f, 0.26f);
                _backdropC.sortingOrder = -14;
                c.transform.localScale = new Vector3(36f, 12f, 1f);
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
                        : new Color(0.22f, 0.23f, 0.25f);

            var g = new GameObject("Ground");
            g.tag = "Ground";
            g.transform.position = new Vector3(0f, -4.25f, 0f);
            g.transform.localScale = new Vector3(widthScale, 1.25f, 1f);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = c;
            sr.sortingOrder = -6;
            g.AddComponent<BoxCollider2D>();
        }

        /// <summary>树心关两侧实体挡墙（Ground），可蹬墙跳；与屏幕四边挡板不同，沿场地宽度固定。</summary>
        static void BuildInkForest03SideArena(float groundWidth)
        {
            var root = new GameObject("ArenaSideWalls").transform;
            float half = groundWidth * 0.5f - 0.32f;
            const float midY = 2f;
            const float wallH = 19f;
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

        void SetupInkForest03CameraFollow(GameObject player, float groundWidth)
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
            follow.smoothTime = 0.15f;
            follow.minX = -halfArena + halfW - margin;
            follow.maxX = halfArena - halfW + margin;
            follow.minY = -4.6f;
            follow.maxY = 9.4f;

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
            Color baseTint = sceneName == "InkForest_03"
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
            var rb = p.AddComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var box = p.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.55f, 0.95f);
            var pm = p.AddComponent<PlayerMovement>();
            pm.moveSpeed = 7.2f;
            pm.jumpForce = 14.5f;
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
            root.transform.localScale = new Vector3(2.15f, 2.55f, 1f);
            var boss = root.AddComponent<BossInkTree>();
            boss.colorGearDrop = colorDrop;
            boss.maxHp = t.maxHp;
            boss.moveSpeed = t.moveSpeed;
            boss.slamDamage = t.slamDamage;

            var hbGo = new GameObject("Hurtbox");
            hbGo.transform.SetParent(root.transform, false);
            var c = hbGo.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = 0.5f;
            var hb = hbGo.AddComponent<Hurtbox>();
            hb.Configure(boss);
            var bc = root.AddComponent<ClampToWorldBounds2D>();
            bc.halfWidthPad = 1.15f;
            bc.halfHeightPad = 1.35f;
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

            hud.jAttackText = CreateHudText(go.transform, "JHint", "普攻 J：无冷却（三段循环）", 17, new Vector2(0.05f, 0.805f), new Vector2(480f, 28f),
                TextAnchor.UpperLeft);

            hud.kCooldownText = CreateHudText(go.transform, "KCD", "墨爆 K: 就绪", 18, new Vector2(0.05f, 0.745f), new Vector2(420f, 32f),
                TextAnchor.UpperLeft);

            hud.lCooldownText = CreateHudText(go.transform, "LCD", "三连 L: 就绪", 18, new Vector2(0.05f, 0.685f), new Vector2(420f, 32f),
                TextAnchor.UpperLeft);

            hud.Bind(player.GetComponent<PlayerStats>(), player.GetComponent<PlayerCombat>());
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
            t.color = new Color(0.92f, 0.94f, 0.9f);
            t.text = text;
            return t;
        }
    }
}
