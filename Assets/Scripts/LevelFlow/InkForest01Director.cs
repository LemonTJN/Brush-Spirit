using System.Collections;
using BrushSpirit.UI;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>
    /// 墨林 01「林缘」：卷首语、暂停时间、延迟开波、教学 Toast、第二波 K 提示。
    /// 挂在与 <see cref="LevelController"/> 同一物体上，由 <see cref="GameRuntimeBootstrap"/> 仅在本关添加。
    /// </summary>
    public class InkForest01Director : MonoBehaviour
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
                GameplayHudToast.Show(this, "敌人围上来时，可以试试墨爆 K 清出空隙。", 4.2f, 185);
        }

        IEnumerator RunIntroSequence()
        {
            yield return null;

            float prevScale = Time.timeScale;
            Time.timeScale = 0f;

            yield return GameplayProloguePanel.ShowLine("绘卷裂了一角，墨色从缝里渗进来。", 3.4f);
            yield return GameplayProloguePanel.ShowLine("有人说那是林子的影子——可影子不该会咬人。", 3.6f);

            Time.timeScale = prevScale > 0f ? prevScale : 1f;

            _level.StartDeferredWaves();

            GameplayHudToast.Show(this, "墨林·林缘：用 J 普攻击退墨影；它们有时会落下无色的器，走过即可拾起。", 5.5f, 180);
        }
    }
}
