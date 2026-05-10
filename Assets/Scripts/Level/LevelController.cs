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

        bool _bossSpawned;

        void Start()
        {
            if (waves != null)
            {
                waves.OnAllWavesCleared += OnWavesCleared;
                waves.Begin();
            }
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
