using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cooking.Core
{
    public sealed class SceneFlow : MonoBehaviour
    {
        public bool IsLoading { get; private set; }

        void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        public void Load(string sceneName)
        {
            if (IsLoading)
                return;

            if (SceneManager.GetActiveScene().name == sceneName)
                return;

            IsLoading = true;
            SceneManager.LoadScene(sceneName);
        }

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            IsLoading = false;
        }
    }
}
