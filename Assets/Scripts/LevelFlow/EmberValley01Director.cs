using System.Collections;
using BrushSpirit.UI;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>
    /// 烬谷 01「烬口」：峡谷入口序章、延迟开波、余烬机制教学 Toast。
    /// </summary>
    public class EmberValley01Director : MonoBehaviour
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
                GameplayHudToast.Show(this, "余烬带会持续灼伤，绕开地上的暗红裂纹。", 4.5f, 185);
        }

        IEnumerator RunIntroSequence()
        {
            yield return null;

            float prevScale = Time.timeScale;
            Time.timeScale = 0f;

            yield return GameplayProloguePanel.ShowLine("风从裂谷里吹出来，带着熄了很久的温度。", 3.5f);
            yield return GameplayProloguePanel.ShowLine("灰底下埋着火——别踩亮的地方。", 3.4f);

            Time.timeScale = prevScale > 0f ? prevScale : 1f;

            _level.StartDeferredWaves();

            GameplayHudToast.Show(this, "烬口：亮灰与暗红裂纹为余烬带，停留会伤血；用 J/K 清怪后走向出口一侧。", 5.8f, 178);
        }
    }
}
