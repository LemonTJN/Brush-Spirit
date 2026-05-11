using BrushSpirit.Enemies;
using BrushSpirit.UI;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    public class LevelController : MonoBehaviour
    {
        public WaveSpawner waves;
        public GameObject bossTemplate;
        public Transform bossSpawnPoint;
        public ColorRestoreEffect colorRestore;

        [Tooltip("无 Boss 时，清完波次后进入的下一场景名")]
        public string nextSceneAfterWaves;

        public string sectionClearTitle = "本段已肃清";
        public string sectionClearSubtitle = "准备进入下一段旅程。";

        [Tooltip("为 true 时不在 Start 中开波，需调用 StartDeferredWaves()（墨林 01 序章用）。")]
        public bool deferWaveStart;

        /// <summary>Boss 实例已生成并激活后触发（焰心爆灰加压等）。</summary>
        public event System.Action OnBossSpawned;

        bool _bossSpawned;
        bool _wavesBegun;

        void Start()
        {
            if (waves != null)
            {
                waves.OnAllWavesCleared += OnWavesCleared;
                if (!deferWaveStart)
                    BeginWavesInternal();
            }
        }

        /// <summary>墨林 01 序章结束后再开始刷怪。</summary>
        public void StartDeferredWaves()
        {
            BeginWavesInternal();
        }

        void BeginWavesInternal()
        {
            if (_wavesBegun || waves == null) return;
            _wavesBegun = true;
            waves.Begin();
        }

        void OnDestroy()
        {
            if (waves != null)
                waves.OnAllWavesCleared -= OnWavesCleared;
        }

        void OnWavesCleared()
        {
            if (bossTemplate != null)
                SpawnBoss();
            else if (!string.IsNullOrEmpty(nextSceneAfterWaves))
                SectionContinuePrompt.Show(nextSceneAfterWaves, sectionClearTitle, sectionClearSubtitle);
        }

        void SpawnBoss()
        {
            if (_bossSpawned || bossTemplate == null) return;
            _bossSpawned = true;
            Vector3 pos = bossSpawnPoint != null ? bossSpawnPoint.position : new Vector3(12f, -3f, 0f);
            var go = Instantiate(bossTemplate, pos, Quaternion.identity);
            go.SetActive(true);
            var boss = go.GetComponent<BossInkTree>();
            if (boss != null)
                boss.OnDefeated += OnBossBeat;
            OnBossSpawned?.Invoke();
        }

        void OnBossBeat()
        {
            if (colorRestore != null)
                colorRestore.Play(() => VictoryPanel.Instance?.Show());
            else
                VictoryPanel.Instance?.Show();
        }
    }
}
