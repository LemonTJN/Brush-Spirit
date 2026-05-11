using BrushSpirit;
using BrushSpirit.LevelFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrushSpirit.Core
{
    /// <summary>
    /// 在「每次场景加载」后挂对应引导器。
    /// 注意：RuntimeInitializeLoadType.AfterSceneLoad 只在进入 Play 后**首场景**回调一次，
    /// 从 Menu 再 LoadScene("InkForest") 时不会再次执行，会导致墨林关只有摄像机蓝底。
    /// 因此改为订阅 SceneManager.sceneLoaded。
    /// </summary>
    public static class BootstrapLoader
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!Application.isPlaying) return;
            TryBootstrap(scene);
        }

        static bool IsInkForestScene(string sceneName)
        {
            return sceneName == "InkForest" || sceneName == "InkForest_01" || sceneName == "InkForest_02" ||
                   sceneName == "InkForest_03";
        }

        static void TryBootstrap(Scene loadedScene)
        {
            var sceneName = loadedScene.name;
            if (sceneName == "Menu" && Object.FindObjectOfType<MenuRuntimeBootstrap>() == null)
            {
                var go = new GameObject("MenuRuntimeBootstrap");
                go.AddComponent<MenuRuntimeBootstrap>();
            }
            else if (IsInkForestScene(sceneName) && !HasLevelControllerInScene(loadedScene))
            {
                var prefab = Resources.Load<GameObject>("GameRuntimeBootstrap");
                if (prefab != null)
                {
                    Object.Instantiate(prefab).name = "GameRuntimeBootstrap";
                }
                else
                {
                    var go = new GameObject("GameRuntimeBootstrap");
                    go.AddComponent<GameRuntimeBootstrap>();
                }
            }
        }

        /// <summary>
        /// 仅判断「当前活动场景」内是否已有关卡逻辑；避免 FindObjectOfType(GameRuntimeBootstrap) 误判导致 02/03 关不生成 WaveRoot。
        /// </summary>
        static bool HasLevelControllerInScene(Scene scene)
        {
            if (!scene.IsValid()) return false;
            var all = Object.FindObjectsOfType<LevelController>();
            for (int i = 0; i < all.Length; i++)
            {
                var lc = all[i];
                if (lc != null && lc.gameObject.scene == scene)
                    return true;
            }

            return false;
        }
    }
}
