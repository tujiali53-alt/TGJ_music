using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cooking.Core
{
    public static class PlayBootstrap
    {
        static bool configuredFirstScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterFirstScene()
        {
            if (configuredFirstScene)
                return;

            configuredFirstScene = true;
            if (SceneManager.GetActiveScene().name != SceneNames.Cooking)
                return;

            GameRoot root = GameRoot.Instance;
            if (root == null)
            {
                Debug.LogError("MainCooking 里需要已经放好 GameRoot。");
                return;
            }

            root.BeginDirectDay();
        }
    }
}
