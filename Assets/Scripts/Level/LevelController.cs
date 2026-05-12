using System.Collections;
using BrushSpirit.Core;
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

        [Tooltip("≥0：清波后经真实时间（秒）自动切换下一关；<0 则仅弹出「继续前行」按钮。")]
        public float autoAdvanceNextSceneDelay = -1f;

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
            {
                if (autoAdvanceNextSceneDelay >= 0f)
                    StartCoroutine(CoAutoAdvanceNextScene());
                else
                    SectionContinuePrompt.Show(nextSceneAfterWaves, sectionClearTitle, sectionClearSubtitle);
            }
        }

        IEnumerator CoAutoAdvanceNextScene()
        {
            GameplayHudToast.Show(this, sectionClearTitle + "\n" + sectionClearSubtitle, 2.2f, 220);
            yield return new WaitForSecondsRealtime(Mathf.Max(1.2f, autoAdvanceNextSceneDelay));
            if (!string.IsNullOrEmpty(nextSceneAfterWaves))
                SceneTransition.LoadScene(nextSceneAfterWaves);
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
