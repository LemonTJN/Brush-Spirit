using System.Collections;
using BrushSpirit.UI;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>绘心 01「褪色庭」序章与教学。</summary>
    public class HeartRealm01Director : MonoBehaviour
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
                GameplayHudToast.Show(this, "大块褪色像菌斑：中间烫脚、边缘发黏；把墨影拽到亮处再打更疼。", 5.4f, 188);
        }

        IEnumerator RunIntroSequence()
        {
            yield return null;

            float prevScale = Time.timeScale;
            Time.timeScale = 0f;

            yield return GameplayProloguePanel.ShowLine("卷心本该最亮，现在只剩一块发冷的灰。", 3.5f);
            yield return GameplayProloguePanel.ShowLine("颜色不是熄了——是被墨魇拧成丝，拴在王座边上。", 3.6f);

            Time.timeScale = prevScale > 0f ? prevScale : 1f;

            _level.StartDeferredWaves();

            GameplayHudToast.Show(this, "绘心·褪色庭：认月牙浅洼、认菌斑形状、认双摆节奏；J/K 清场。", 5.8f, 178);
        }
    }
}
