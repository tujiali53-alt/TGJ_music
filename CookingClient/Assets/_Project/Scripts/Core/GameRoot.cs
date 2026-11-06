using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cooking.Core
{
    public sealed class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        public const string DefaultPlayerNameZh = "小鼻嘎";
        public const string DefaultPlayerNameEn = "Lil Tu";

        public GameSession Session { get; private set; }
        public SceneFlow Flow { get; private set; }

        public string PlayerDisplayName
        {
            get
            {
                if (Session != null && !string.IsNullOrWhiteSpace(Session.PlayerName))
                    return Session.PlayerName.Trim();

                return GameLanguage.Current == DiaryLanguage.En
                    ? DefaultPlayerNameEn
                    : DefaultPlayerNameZh;
            }
        }

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

            // Day 7 has no night; bed should not advance past the final day.
            if (Session.DayIndex >= 7)
                return false;

            Session.DayIndex += 1;
            AudioManager.SetDayStage(Session.DayIndex);   // 先设置BGM版本，再进入白天
            Session.Phase = DayPhase.Day;
            Session.ClearCookingScores();
            Flow.Load(SceneNames.Cooking);
            return true;
        }

        public void ReturnToMenu()
        {
            if (Flow != null && Flow.IsLoading)
                return;

            Instance = null;
            SceneManager.LoadScene(SceneNames.Menu);
            Destroy(gameObject);
        }
    }
}
