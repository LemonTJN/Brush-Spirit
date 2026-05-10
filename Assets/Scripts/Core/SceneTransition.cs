using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BrushSpirit.Core
{
    /// <summary>场景切换淡入淡出（全屏遮罩）。</summary>
    public class SceneTransition : MonoBehaviour
    {
        const float FadeDuration = 0.45f;
        static SceneTransition _inst;
        Canvas _canvas;
        Image _fade;
        bool _busy;

        public static void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            EnsureExists();
            if (!_inst._busy)
                _inst.StartCoroutine(_inst.Run(sceneName));
        }

        static void EnsureExists()
        {
            if (_inst != null) return;
            var go = new GameObject("SceneTransition");
            _inst = go.AddComponent<SceneTransition>();
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            if (_inst != null && _inst != this)
            {
                Destroy(gameObject);
                return;
            }

            _inst = this;
            DontDestroyOnLoad(gameObject);
            BuildFadeCanvas();
        }

        void BuildFadeCanvas()
        {
            _canvas = gameObject.GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 2000;
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            var imgGo = new GameObject("Fade");
            imgGo.transform.SetParent(transform, false);
            var rect = imgGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _fade = imgGo.AddComponent<Image>();
            _fade.color = new Color(0f, 0f, 0f, 0f);
            _fade.raycastTarget = true;
        }

        IEnumerator Run(string sceneName)
        {
            _busy = true;
            float t = 0f;
            while (t < FadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / FadeDuration);
                _fade.color = new Color(0f, 0f, 0f, a);
                yield return null;
            }

            _fade.color = new Color(0f, 0f, 0f, 1f);
            if (_fade != null) _fade.raycastTarget = true;
            var op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;
            while (op.progress < 0.9f)
                yield return null;
            op.allowSceneActivation = true;
            while (!op.isDone)
                yield return null;

            t = 0f;
            while (t < FadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(t / FadeDuration);
                _fade.color = new Color(0f, 0f, 0f, a);
                yield return null;
            }

            _fade.color = new Color(0f, 0f, 0f, 0f);
            if (_fade != null) _fade.raycastTarget = false;
            _busy = false;
        }
    }
}
