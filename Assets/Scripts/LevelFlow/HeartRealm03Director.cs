using System.Collections;
using BrushSpirit.UI;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>绘心 03「悬枢阶」：漂移雾与沉台提示。</summary>
    public class HeartRealm03Director : MonoBehaviour
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
                GameplayHudToast.Show(this, "有些跳板会沉入纸背——变色半透明时别站太久。", 5.3f, 188);
        }

        IEnumerator RunIntroSequence()
        {
            yield return null;

            float prevScale = Time.timeScale;
            Time.timeScale = 0f;

            yield return GameplayProloguePanel.ShowLine("枢轴悬在半空，像一卷被扯松的齿。", 3.3f);
            yield return GameplayProloguePanel.ShowLine("灰雾会自己走动——别追着它跑。", 3.2f);

            Time.timeScale = prevScale > 0f ? prevScale : 1f;

            _level.StartDeferredWaves();

            GameplayHudToast.Show(this, "悬枢阶：裂帛更密；褪色域整片平移，优先占高地。", 5.5f, 180);
        }
    }
}
