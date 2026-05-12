using System.Collections;
using BrushSpirit.UI;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>绘心 02「裂帛廊」：裂帛预警教学。</summary>
    public class HeartRealm02Director : MonoBehaviour
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
                GameplayHudToast.Show(this, "裂帛会沿整条廊道扫过去——贴坡或上链桥躲，别和绊索硬冲。", 5.4f, 188);
            if (waveIndexZeroBased == 2)
                GameplayHudToast.Show(this, "从第二波起，裂帛扫向会和刷怪侧「唱反调」：先看怪从哪边来。", 5.2f, 186);
        }

        IEnumerator RunIntroSequence()
        {
            yield return null;

            float prevScale = Time.timeScale;
            Time.timeScale = 0f;

            yield return GameplayProloguePanel.ShowLine("两侧的纸缘挤过来，像要把路掐进卷轴里。", 3.4f);
            yield return GameplayProloguePanel.ShowLine("脚下有时会亮起一道细长的警告——那不是装饰。", 3.5f);

            Time.timeScale = prevScale > 0f ? prevScale : 1f;

            _level.StartDeferredWaves();

            GameplayHudToast.Show(this, "裂帛廊：地面是倒置的∧脊、两侧略低；中空窄台与链桥错层，链桥仍会刷墨影。", 5.8f, 182);
        }
    }
}
