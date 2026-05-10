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

        Color _startTint = new Color(0.12f, 0.12f, 0.14f, 0.82f);
        Color _endTint = new Color(0.35f, 0.72f, 0.42f, 0.12f);
        Color _startSprite = new Color(0.35f, 0.35f, 0.38f);
        Color _endSprite = new Color(0.55f, 0.82f, 0.58f);

        public void Play(System.Action onComplete)
        {
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
