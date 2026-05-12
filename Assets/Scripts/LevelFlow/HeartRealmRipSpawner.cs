using System.Collections;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>
    /// 绘心「裂帛」周期生成：裂帛廊为<strong>定向扫全廊</strong>；第二波起扫向与当波首个刷点侧别绑定（怪偏右刷则先左→右扫）。
    /// </summary>
    public class HeartRealmRipSpawner : MonoBehaviour
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
        bool _corridorDirectional;
        WaveSpawner _waves;

        /// <summary>非 null 时下一次定向裂帛使用该方向；null 则随机（用于第一波）。</summary>
        int? _queuedSweepFromLeft;

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

        /// <summary>裂帛廊：定向扫全廊 + 与刷点绑定的扫向。</summary>
        public void SetCorridorDirectionalMode(bool enabled) => _corridorDirectional = enabled;

        float MinCd => _bossPhase ? _bossMinCd : _normalMinCd;
        float MaxCd => _bossPhase ? _bossMaxCd : _normalMaxCd;

        void OnDestroy()
        {
            if (_waves != null)
                _waves.OnWaveStarted -= OnWaveStarted;
        }

        void OnWaveStarted(int waveIndexZeroBased)
        {
            if (!_corridorDirectional || _waves == null) return;
            if (waveIndexZeroBased < 1) return;
            // 怪从右侧刷 → 先扫左侧：从左扫向右
            _queuedSweepFromLeft = _waves.LastWaveFirstSpawnWorldX > 0.2f ? 1 : 0;
        }

        void Start()
        {
            _waves = FindObjectOfType<WaveSpawner>();
            if (_waves != null)
                _waves.OnWaveStarted += OnWaveStarted;

            if (_sprite == null) return;
            StartCoroutine(Loop());
        }

        IEnumerator Loop()
        {
            yield return new WaitForSeconds(Random.Range(1.1f, 1.9f));
            while (true)
            {
                if (_corridorDirectional)
                {
                    bool fromLeft;
                    if (_queuedSweepFromLeft.HasValue)
                    {
                        fromLeft = _queuedSweepFromLeft.Value == 1;
                        _queuedSweepFromLeft = null;
                    }
                    else
                        fromLeft = Random.value > 0.5f;

                    yield return DirectionalSweepBurstFullCorridor(fromLeft);
                }
                else
                {
                    float x = Random.Range(_minX, _maxX);
                    SpawnRip(x);
                    if (_bossPhase && Random.value < 0.38f)
                    {
                        yield return new WaitForSeconds(Random.Range(0.18f, 0.36f));
                        SpawnRip(Random.Range(_minX, _maxX));
                    }
                }

                yield return new WaitForSeconds(Random.Range(MinCd, MaxCd));
            }
        }

        void SpawnRip(float x)
        {
            float warn;
            float hw;
            float hh;
            float dmg;
            float knock;
            if (_bossPhase)
            {
                warn = Random.Range(0.62f, 0.82f);
                hw = Random.Range(5.8f, 7.2f);
                hh = Random.Range(0.42f, 0.52f);
                dmg = Random.Range(9.5f, 11.5f);
                knock = Random.Range(7f, 8.6f);
            }
            else
            {
                warn = Random.Range(0.85f, 1.1f);
                hw = Random.Range(4.8f, 6.2f);
                hh = Random.Range(0.38f, 0.48f);
                dmg = Random.Range(7.8f, 9.6f);
                knock = Random.Range(5.8f, 7.2f);
            }

            HeartRipGroundBurstOneShot.Create(
                new Vector3(x, _spawnY, 0f),
                _sprite,
                warn,
                hw,
                hh,
                dmg,
                knock);
        }

        /// <summary>整条廊道顺序爆发：先全宽预警，再沿 X 步进微裂帛。</summary>
        IEnumerator DirectionalSweepBurstFullCorridor(bool fromLeft)
        {
            float span = Mathf.Max(2.5f, _maxX - _minX);
            float warn = _bossPhase ? 0.58f : 0.68f;
            var warnGo = new GameObject("RipSweepWarnFull");
            warnGo.transform.position = new Vector3((_minX + _maxX) * 0.5f, _spawnY, 0f);
            var sr = warnGo.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = new Color(0.42f, 0.36f, 0.52f, 0.2f);
            sr.sortingOrder = 3;
            warnGo.transform.localScale = new Vector3(0.12f, 0.3f, 1f);
            float t = 0f;
            while (t < warn)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / warn);
                warnGo.transform.localScale = new Vector3(Mathf.Lerp(0.12f, span * 1.04f, u), Mathf.Lerp(0.28f, 0.52f, u), 1f);
                sr.color = new Color(0.78f, 0.26f, 0.36f, 0.16f + 0.48f * u);
                yield return null;
            }

            Destroy(warnGo);

            int steps = _bossPhase ? 17 : 13;
            float hwBurst = _bossPhase ? 1.38f : 1.12f;
            float hhBurst = _bossPhase ? 0.4f : 0.36f;
            float stepWait = _bossPhase ? 0.032f : 0.04f;
            for (int i = 0; i <= steps; i++)
            {
                float u = steps == 0 ? 0f : (float)i / steps;
                float x = fromLeft ? Mathf.Lerp(_minX, _maxX, u) : Mathf.Lerp(_maxX, _minX, u);
                float dmg = _bossPhase ? Random.Range(8.8f, 10.6f) : Random.Range(7.1f, 8.7f);
                float knock = _bossPhase ? Random.Range(6.2f, 7.5f) : Random.Range(5.2f, 6.5f);
                HeartRipGroundBurstOneShot.Create(new Vector3(x, _spawnY, 0f), _sprite, 0.045f, hwBurst, hhBurst, dmg, knock);
                yield return new WaitForSeconds(stepWait);
            }
        }
    }
}
