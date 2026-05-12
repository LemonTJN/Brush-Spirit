using System.Collections;
using System.Collections.Generic;
using BrushSpirit.Enemies;
using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    public class WaveSpawner : MonoBehaviour
    {
        public List<int> enemiesPerWave = new List<int> { 2, 3 };
        public List<Transform> spawnPoints = new List<Transform>();

        public System.Action OnAllWavesCleared;

        /// <summary>每波敌人生成完毕、开始等待全灭时触发（0 为第一波）。</summary>
        public System.Action<int> OnWaveStarted;

        public System.Func<Transform, SimpleEnemy> EnemyFactory { get; set; }

        /// <summary>当前波第一个生成点世界 X（在生成敌人之前写入，供裂帛等机关绑定侧别）。</summary>
        public float LastWaveFirstSpawnWorldX { get; private set; }

        [Tooltip("每一波敌人全灭后、下一波开始前的等待（秒，受 timeScale 影响时用 WaitForSecondsRealtime）")]
        public float delayBetweenWaves = 0f;

        readonly List<int> _spawnOrder = new List<int>(32);

        bool _running;
        Transform _fallbackSpawn;

        public void Begin()
        {
            if (_running) return;
            _running = true;
            StartCoroutine(RunWaves());
        }

        IEnumerator RunWaves()
        {
            try
            {
                for (int w = 0; w < enemiesPerWave.Count; w++)
                {
                    int count = enemiesPerWave[w];
                    BuildShuffledSpawnOrder(w);
                    var waveRoots = new List<GameObject>(count);
                    for (int i = 0; i < count; i++)
                    {
                        Transform sp;
                        if (spawnPoints.Count > 0)
                        {
                            int idx = _spawnOrder[i % _spawnOrder.Count];
                            sp = spawnPoints[idx];
                        }
                        else
                        {
                            if (_fallbackSpawn == null)
                            {
                                var go = new GameObject("FallbackSpawn");
                                go.transform.SetParent(transform, false);
                                _fallbackSpawn = go.transform;
                            }

                            _fallbackSpawn.position = transform.position + Vector3.right * (i - count * 0.5f);
                            sp = _fallbackSpawn;
                        }

                        if (i == 0)
                            LastWaveFirstSpawnWorldX = sp.position.x;

                        var e = EnemyFactory != null ? EnemyFactory(sp) : null;
                        if (e != null)
                            waveRoots.Add(e.gameObject);
                    }

                    OnWaveStarted?.Invoke(w);

                    // 不依赖 Die 回调：Destroy 后 UnityEngine.Object 与 null 比较为真，避免漏计导致协程永远等不到第二波
                    while (true)
                    {
                        bool anyAlive = false;
                        for (int j = 0; j < waveRoots.Count; j++)
                        {
                            if (waveRoots[j] != null)
                            {
                                anyAlive = true;
                                break;
                            }
                        }

                        if (!anyAlive) break;
                        yield return null;
                    }

                    if (delayBetweenWaves > 0f && w < enemiesPerWave.Count - 1)
                        yield return new WaitForSecondsRealtime(delayBetweenWaves);
                }

                OnAllWavesCleared?.Invoke();
            }
            finally
            {
                _running = false;
            }
        }

        /// <summary>每波打乱刷怪点顺序，使位置与上轮不同；同波内按打乱顺序轮询。</summary>
        void BuildShuffledSpawnOrder(int waveIndex)
        {
            _spawnOrder.Clear();
            int n = spawnPoints.Count;
            if (n == 0) return;

            float refX = 0f;
            if (PlayerStats.Active != null)
                refX = PlayerStats.Active.transform.position.x;
            else if (Camera.main != null)
                refX = Camera.main.transform.position.x;

            float maxDx = 18f;
            var cam = Camera.main;
            if (cam != null && cam.orthographic)
                maxDx = Mathf.Max(18f, cam.aspect * cam.orthographicSize + 5f);

            for (int i = 0; i < n; i++)
            {
                var t = spawnPoints[i];
                if (t == null) continue;
                if (Mathf.Abs(t.position.x - refX) <= maxDx)
                    _spawnOrder.Add(i);
            }

            if (_spawnOrder.Count == 0)
            {
                for (int i = 0; i < n; i++)
                {
                    if (spawnPoints[i] != null)
                        _spawnOrder.Add(i);
                }
            }

            if (_spawnOrder.Count == 0)
            {
                for (int i = 0; i < n; i++)
                    _spawnOrder.Add(i);
            }

            var rng = new System.Random(unchecked(11003 * (waveIndex + 1) + n * 13001 + enemiesPerWave.Count * 104729));
            int m = _spawnOrder.Count;
            for (int i = m - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = _spawnOrder[i];
                _spawnOrder[i] = _spawnOrder[j];
                _spawnOrder[j] = t;
            }
        }
    }
}
