using System.Collections;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>
    /// 在 playable X 范围内周期性生成 <see cref="AshGroundBurstOneShot"/>（焚道 / 焰心环境压力）。
    /// 可选「Boss 阶段」：更短间隔、略强单发，并概率连发第二处爆灰。
    /// </summary>
    public class EmberValleyAshBurstSpawner : MonoBehaviour
    {
        Sprite _sprite;
        float _minX;
        float _maxX;
        float _spawnY;
        float _normalMinCd;
        float _normalMaxCd;
        float _bossMinCd;
        float _bossMaxCd;
        bool _bossPhase;
        bool _radialSplash;

        /// <summary>烬谷 03：爆灰爆炸后 360° 溅射伤害。</summary>
        public void SetRadialSplash(bool enabled) => _radialSplash = enabled;

        /// <summary>焚道等：全程同一冷却区间。</summary>
        public void Configure(Sprite ringSprite, float minX, float maxX, float spawnY, float minCooldown,
            float maxCooldown)
        {
            ConfigureWithBossPhase(ringSprite, minX, maxX, spawnY, minCooldown, maxCooldown, minCooldown,
                maxCooldown);
        }

        /// <summary>焰心等：清波前用 normal 冷却，Boss 出现后切换为 boss 冷却（更密）。</summary>
        public void ConfigureWithBossPhase(Sprite ringSprite, float minX, float maxX, float spawnY,
            float normalMinCooldown, float normalMaxCooldown, float bossMinCooldown, float bossMaxCooldown)
        {
            _sprite = ringSprite;
            _minX = minX;
            _maxX = maxX;
            _spawnY = spawnY;
            _normalMinCd = normalMinCooldown;
            _normalMaxCd = normalMaxCooldown;
            _bossMinCd = bossMinCooldown;
            _bossMaxCd = bossMaxCooldown;
            _bossPhase = false;
        }

        public void SetBossPhase(bool bossPhase) => _bossPhase = bossPhase;

        float MinCd => _bossPhase ? _bossMinCd : _normalMinCd;
        float MaxCd => _bossPhase ? _bossMaxCd : _normalMaxCd;

        void Start()
        {
            if (_sprite == null) return;
            StartCoroutine(Loop());
        }

        IEnumerator Loop()
        {
            yield return new WaitForSeconds(Random.Range(1.2f, 2.1f));
            while (true)
            {
                float x = Random.Range(_minX, _maxX);
                SpawnOneBurst(x);
                if (_bossPhase && Random.value < 0.45f)
                {
                    yield return new WaitForSeconds(Random.Range(0.14f, 0.32f));
                    SpawnOneBurst(Random.Range(_minX, _maxX));
                }

                yield return new WaitForSeconds(Random.Range(MinCd, MaxCd));
            }
        }

        void SpawnOneBurst(float x)
        {
            float warn;
            float rad;
            float dmg;
            float knock;
            if (_bossPhase)
            {
                warn = Random.Range(0.7f, 0.9f);
                rad = Random.Range(2.42f, 2.88f);
                dmg = Random.Range(8.4f, 10.2f);
                knock = Random.Range(6.2f, 7.5f);
            }
            else
            {
                warn = Random.Range(0.88f, 1.12f);
                rad = Random.Range(2.15f, 2.55f);
                dmg = Random.Range(7f, 9f);
                knock = Random.Range(5.5f, 7f);
            }

            AshGroundBurstOneShot.Create(
                new Vector3(x, _spawnY, 0f),
                _sprite,
                warn,
                rad,
                dmg,
                knock,
                _radialSplash);
        }
    }
}
