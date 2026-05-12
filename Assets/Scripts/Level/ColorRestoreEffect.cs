using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BrushSpirit.LevelFlow
{
    public class ColorRestoreEffect : MonoBehaviour
    {
        public Image fullscreenTint;
        public SpriteRenderer[] extraTargets;
        public float duration = 4f;

        [Tooltip("为 true 时用下列四色做插值（烬谷焰心赤色复苏）；否则用墨林默认绿调。")]
        public bool useCustomPalette;

        [Tooltip("为 true 时全色谱多段插值（绘心终章万色归来）；与 useCustomPalette 互斥时优先本项。")]
        public bool useSpectrumRestore;

        public Color customStartTint = new Color(0.12f, 0.12f, 0.14f, 0.82f);
        public Color customEndTint = new Color(0.35f, 0.72f, 0.42f, 0.12f);
        public Color customStartSprite = new Color(0.35f, 0.35f, 0.38f);
        public Color customEndSprite = new Color(0.55f, 0.82f, 0.58f);

        Color _startTint = new Color(0.12f, 0.12f, 0.14f, 0.82f);
        Color _endTint = new Color(0.35f, 0.72f, 0.42f, 0.12f);
        Color _startSprite = new Color(0.35f, 0.35f, 0.38f);
        Color _endSprite = new Color(0.55f, 0.82f, 0.58f);

        public void Play(System.Action onComplete)
        {
            if (useSpectrumRestore)
            {
                StartCoroutine(RunSpectrum(onComplete));
                return;
            }

            if (useCustomPalette)
            {
                _startTint = customStartTint;
                _endTint = customEndTint;
                _startSprite = customStartSprite;
                _endSprite = customEndSprite;
            }

            StartCoroutine(Run(onComplete));
        }

        static Color LerpColors(Color[] keys, float t01)
        {
            if (keys == null || keys.Length == 0) return Color.white;
            if (keys.Length == 1) return keys[0];
            float f = t01 * (keys.Length - 1);
            int i = Mathf.Min(keys.Length - 2, Mathf.FloorToInt(f));
            float localU = f - i;
            return Color.Lerp(keys[i], keys[i + 1], localU);
        }

        IEnumerator RunSpectrum(System.Action onComplete)
        {
            var tintKeys = new[]
            {
                new Color(0.1f, 0.09f, 0.12f, 0.85f),
                new Color(0.15f, 0.22f, 0.18f, 0.55f),
                new Color(0.22f, 0.14f, 0.2f, 0.38f),
                new Color(0.25f, 0.2f, 0.12f, 0.22f),
                new Color(0.2f, 0.35f, 0.42f, 0.12f),
                new Color(0.85f, 0.88f, 0.92f, 0.06f)
            };
            var spriteKeys = new[]
            {
                new Color(0.22f, 0.2f, 0.24f),
                new Color(0.32f, 0.38f, 0.34f),
                new Color(0.42f, 0.3f, 0.36f),
                new Color(0.48f, 0.4f, 0.32f),
                new Color(0.35f, 0.52f, 0.58f),
                new Color(0.88f, 0.86f, 0.9f)
            };

            if (fullscreenTint != null)
                fullscreenTint.gameObject.SetActive(true);

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                if (fullscreenTint != null)
                    fullscreenTint.color = LerpColors(tintKeys, u);
                if (extraTargets != null)
                {
                    Color sc = LerpColors(spriteKeys, u);
                    for (int i = 0; i < extraTargets.Length; i++)
                    {
                        if (extraTargets[i] != null)
                            extraTargets[i].color = sc;
                    }
                }

                yield return null;
            }

            onComplete?.Invoke();
        }

        IEnumerator Run(System.Action onComplete)
        {
            if (fullscreenTint != null)
                fullscreenTint.gameObject.SetActive(true);

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                if (fullscreenTint != null)
                    fullscreenTint.color = Color.Lerp(_startTint, _endTint, u);
                if (extraTargets != null)
                {
                    for (int i = 0; i < extraTargets.Length; i++)
                    {
                        if (extraTargets[i] != null)
                            extraTargets[i].color = Color.Lerp(_startSprite, _endSprite, u);
                    }
                }

                yield return null;
            }

            onComplete?.Invoke();
        }
    }
}
