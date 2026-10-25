using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;

namespace Cooking.Core
{
    public sealed class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        public GameSession Session { get; private set; }
        public SceneFlow Flow { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Session = GameSession.NewGame();
            Flow = GetComponent<SceneFlow>();
            if (Flow == null)
                Debug.LogError("GameRoot 上需要已经挂好 SceneFlow。", this);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void NewGame()
        {
            Session = GameSession.NewGame();
        }

        public void BeginDirectDay()
        {
            Session = GameSession.NewGame();
            Session.Phase = DayPhase.Day;
        }

        public bool SleepAndEnterDay()
        {
            if (Session == null || Flow == null || Flow.IsLoading)
                return false;

            if (Session.Phase != DayPhase.Night)
                return false;

            Session.DayIndex += 1;
            Session.Phase = DayPhase.Day;
            Flow.Load(SceneNames.Cooking);
            return true;
        }
    }
}
