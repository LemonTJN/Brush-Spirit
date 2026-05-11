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
            if (useCustomPalette)
            {
                _startTint = customStartTint;
                _endTint = customEndTint;
                _startSprite = customStartSprite;
                _endSprite = customEndSprite;
            }

            StartCoroutine(Run(onComplete));
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
