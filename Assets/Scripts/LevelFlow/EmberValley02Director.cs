using System.Collections;
using BrushSpirit.UI;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>
    /// 烬谷 02「焚道」：窄峡序章、延迟开波、爆灰与黑烟教学 Toast。
    /// </summary>
    public class EmberValley02Director : MonoBehaviour
    {
        LevelController _level;
        WaveSpawner _waves;

        void Awake()
        {
            _level = GetComponent<LevelController>();
            if (_level != null)
                _waves = _level.waves;
        }

        void Start()
        {
            if (_level == null || _waves == null) return;
            _waves.OnWaveStarted += OnWaveStarted;
            StartCoroutine(RunIntroSequence());
        }

        void OnDestroy()
        {
            if (_waves != null)
                _waves.OnWaveStarted -= OnWaveStarted;
        }

        void OnWaveStarted(int waveIndexZeroBased)
        {
            if (waveIndexZeroBased == 1)
                GameplayHudToast.Show(this, "峡道收窄时别贴边太久——地上会突然「爆灰」，看见橙圈先拉开。", 5f, 188);
        }

        IEnumerator RunIntroSequence()
        {
            yield return null;

            float prevScale = Time.timeScale;
            Time.timeScale = 0f;

            yield return GameplayProloguePanel.ShowLine("两侧的灰壁挤过来，像要把路掐灭。", 3.4f);
            yield return GameplayProloguePanel.ShowLine("脚下有时会鼓起一圈热——那不是错觉。", 3.5f);

            Time.timeScale = prevScale > 0f ? prevScale : 1f;

            _level.StartDeferredWaves();

            GameplayHudToast.Show(this, "焚道：橙圈为爆灰预警，炸开时造成伤害并击退；黑烟会轻微遮眼。", 6f, 175);
        }
    }
}
