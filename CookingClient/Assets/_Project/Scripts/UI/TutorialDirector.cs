using System.Collections;
using System.Collections.Generic;
using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class TutorialDirector : MonoBehaviour
    {
        public static TutorialDirector Instance { get; private set; }

        [SerializeField] TutorialCatalog catalog;
        [SerializeField] TutorialDialogPlayer dialogPlayer;
        [SerializeField] GameObject tutorialPanel;
        [SerializeField] Button tutorialCloseButton;
        [SerializeField] float tutorialCloseDelay = 2f;
        [SerializeField] GameObject guideMask;
        [SerializeField] Button eatButton;
        [SerializeField] MealEatController mealEat;
        [SerializeField] RecipeDiaryPanels recipePanels;
        [SerializeField] RecipeNotice recipeNotice;

        bool playing;
        bool plateGuideActive;
        readonly List<Selectable> disabledSelectables = new List<Selectable>();

        public bool IsPlaying => playing || (dialogPlayer != null && dialogPlayer.IsActive);
        public bool BlocksRecipeAutoOpen => Needs(TutorialIds.Day1Wakeup);
        public bool BlocksPlateUntilOptimalDone =>
            IsTutorialDay1() && !Has(TutorialIds.Day1Optimal) && Has(TutorialIds.Day1AfterCut);

        void Awake()
        {
            Instance = this;
            ResolveRefs();

            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
            if (tutorialCloseButton != null)
            {
                tutorialCloseButton.gameObject.SetActive(false);
                tutorialCloseButton.onClick.AddListener(OnTutorialPanelClosed);
            }

            if (guideMask != null)
                guideMask.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (tutorialCloseButton != null)
                tutorialCloseButton.onClick.RemoveListener(OnTutorialPanelClosed);
        }

        void Start()
        {
            StartCoroutine(Boot());
        }

        IEnumerator Boot()
        {
            yield return null;
            GameSession session = Session();
            if (session == null || Has(TutorialIds.Completed))
                yield break;

            if (session.DayIndex == 0 && session.Phase == DayPhase.Night && Needs(TutorialIds.Night0Intro))
                BeginNight0Intro();
            else if (session.DayIndex == 1 && session.Phase == DayPhase.Day && Needs(TutorialIds.Day1Wakeup))
                BeginDay1Wakeup();
        }

        public void NotifyNightResultClosed()
        {
            GameSession session = Session();
            if (session == null || session.DayIndex != 0 || session.Phase != DayPhase.Night)
                return;

            if (Needs(TutorialIds.Night0PostForage) && Has(TutorialIds.Night0Intro))
                PlaySegment(TutorialIds.Night0PostForage, null);
        }

        public void NotifyRecipeClosed()
        {
            if (!IsTutorialDay1() || Needs(TutorialIds.Day1Wakeup) || !Needs(TutorialIds.Day1AfterRecipe))
                return;

            PlaySegment(TutorialIds.Day1AfterRecipe, null);
        }

        public void NotifyCut()
        {
            if (!IsTutorialDay1() || !Needs(TutorialIds.Day1AfterCut))
                return;

            if (!Has(TutorialIds.Day1AfterRecipe))
                return;

            PlaySegment(TutorialIds.Day1AfterCut, null);
        }

        public void NotifyReachedOptimal(IngredientInstance item)
        {
            if (!IsTutorialDay1() || !Needs(TutorialIds.Day1Optimal))
                return;

            if (!Has(TutorialIds.Day1AfterCut))
                return;

            if (item == null || item.DefinitionId != "ing_mushroom")
                return;

            CookingPot pot = FindAnyObjectByType<CookingPot>(FindObjectsInactive.Include);
            if (pot != null)
                pot.TurnOffAllFires();

            PlaySegment(TutorialIds.Day1Optimal, null);
        }

        public bool AllowPlateDrop()
        {
            if (!IsTutorialDay1())
                return true;

            if (Needs(TutorialIds.Day1Optimal))
                return false;

            return true;
        }

        public void NotifyPlated()
        {
            if (!IsTutorialDay1() || !Needs(TutorialIds.Day1Plate))
                return;

            if (!Has(TutorialIds.Day1Optimal))
                return;

            PlaySegment(TutorialIds.Day1Plate, null);
        }

        public void NotifyEatResultReady()
        {
            if (!IsTutorialDay1() || !Needs(TutorialIds.Day1AfterEat))
                return;

            if (!Has(TutorialIds.Day1Plate))
                return;

            PlaySegment(TutorialIds.Day1AfterEat, () =>
            {
                Remember(TutorialIds.Completed);
            });
        }

        void BeginNight0Intro()
        {
            PlaySegment(TutorialIds.Night0Intro, () =>
            {
                StartCoroutine(ShowTutorialPanel());
            });
        }

        void BeginDay1Wakeup()
        {
            PlaySegment(TutorialIds.Day1Wakeup, () =>
            {
                if (recipeNotice != null)
                    recipeNotice.OpenNowForTutorial();
                else if (recipePanels != null)
                    recipePanels.OpenRecipe();
            });
        }

        IEnumerator ShowTutorialPanel()
        {
            if (tutorialPanel == null)
            {
                Debug.LogError("outingScene 需要 Canvas/tutorialPanel。", this);
                yield break;
            }

            tutorialPanel.SetActive(true);
            if (tutorialCloseButton != null)
                tutorialCloseButton.gameObject.SetActive(false);

            float delay = Mathf.Max(0f, tutorialCloseDelay);
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            if (tutorialCloseButton != null)
                tutorialCloseButton.gameObject.SetActive(true);
        }

        void OnTutorialPanelClosed()
        {
            AudioManager.PlayClick();
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
            if (tutorialCloseButton != null)
                tutorialCloseButton.gameObject.SetActive(false);
        }

        void PlaySegment(string segmentId, System.Action after)
        {
            if (playing || catalog == null)
                return;

            if (Has(segmentId))
            {
                if (after != null)
                    after();
                return;
            }

            TutorialSegment segment = catalog.Find(segmentId);
            if (segment == null || segment.lines == null || segment.lines.Count == 0)
            {
                Remember(segmentId);
                if (after != null)
                    after();
                return;
            }

            if (dialogPlayer == null)
            {
                Debug.LogError("TutorialDirector 需要 TutorialDialogPlayer。", this);
                return;
            }

            playing = true;
            dialogPlayer.Play(segment.lines, () =>
            {
                playing = false;
                Remember(segmentId);
                if (after != null)
                    after();
            });
        }

        void ShowPlateGuide()
        {
            plateGuideActive = true;
            if (guideMask != null)
            {
                guideMask.SetActive(true);
                Transform parent = guideMask.transform.parent;
                if (parent != null)
                {
                    Transform interaction = parent.Find("InteractionPanel");
                    if (interaction != null)
                        interaction.SetAsLastSibling();
                }
            }

            disabledSelectables.Clear();
            Selectable[] all = FindObjectsByType<Selectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                Selectable selectable = all[i];
                if (selectable == null || !selectable.interactable)
                    continue;

                if (eatButton != null && selectable == eatButton)
                    continue;

                if (IsUnderDialog(selectable.transform))
                    continue;

                selectable.interactable = false;
                disabledSelectables.Add(selectable);
            }
        }

        void HidePlateGuide()
        {
            plateGuideActive = false;
            if (guideMask != null)
                guideMask.SetActive(false);

            for (int i = 0; i < disabledSelectables.Count; i++)
            {
                if (disabledSelectables[i] != null)
                    disabledSelectables[i].interactable = true;
            }

            disabledSelectables.Clear();
        }

        bool IsUnderDialog(Transform target)
        {
            Transform t = target;
            while (t != null)
            {
                if (t.name == "DialogPanel" || t.name == "PlayerDialogue")
                    return true;
                t = t.parent;
            }

            return false;
        }

        bool IsTutorialDay1()
        {
            GameSession session = Session();
            return session != null
                && session.DayIndex == 1
                && session.Phase == DayPhase.Day
                && !Has(TutorialIds.Completed);
        }

        bool Needs(string id)
        {
            return !Has(id);
        }

        bool Has(string id)
        {
            GameSession session = Session();
            return session != null && session.HasSeenTutorial(id);
        }

        void Remember(string id)
        {
            GameSession session = Session();
            if (session != null)
                session.RememberTutorial(id);
        }

        static GameSession Session()
        {
            GameRoot root = GameRoot.Instance;
            return root != null ? root.Session : null;
        }

        void ResolveRefs()
        {
            if (dialogPlayer == null)
                dialogPlayer = GetComponent<TutorialDialogPlayer>();

            if (tutorialPanel == null)
            {
                Transform found = transform.Find("tutorialPanel");
                if (found == null && transform.parent != null)
                    found = transform.parent.Find("tutorialPanel");
                if (found != null)
                    tutorialPanel = found.gameObject;
            }

            if (tutorialPanel != null && tutorialCloseButton == null)
            {
                Transform found = tutorialPanel.transform.Find("closedButton");
                if (found != null)
                    tutorialCloseButton = found.GetComponent<Button>();
            }

            if (guideMask == null)
            {
                Transform found = transform.Find("TutorialGuideMask");
                if (found == null && transform.parent != null)
                    found = transform.parent.Find("TutorialGuideMask");
                if (found != null)
                    guideMask = found.gameObject;
            }

            if (mealEat == null)
                mealEat = FindAnyObjectByType<MealEatController>(FindObjectsInactive.Include);

            if (eatButton == null)
            {
                Transform found = FindNamed("EattingButton");
                if (found != null)
                    eatButton = found.GetComponent<Button>();
            }

            if (recipePanels == null)
                recipePanels = FindAnyObjectByType<RecipeDiaryPanels>(FindObjectsInactive.Include);

            if (recipeNotice == null)
                recipeNotice = FindAnyObjectByType<RecipeNotice>(FindObjectsInactive.Include);

            if (dialogPlayer != null)
            {
                Transform interaction = FindNamed("InteractionPanel");
                Transform dialog = null;
                if (interaction != null)
                    dialog = interaction.Find("DialogPanel");

                TMP_Text body = null;
                TMP_Text name = null;
                Image profile = null;
                if (dialog != null)
                {
                    Transform player = dialog.Find("PlayerDialogue");
                    if (player != null)
                    {
                        Transform bodyT = player.Find("BodyText");
                        Transform nameT = player.Find("UserNameText");
                        Transform profileT = player.Find("PlayerProfileImage");
                        if (bodyT != null)
                            body = bodyT.GetComponent<TMP_Text>();
                        if (nameT != null)
                            name = nameT.GetComponent<TMP_Text>();
                        if (profileT != null)
                            profile = profileT.GetComponent<Image>();
                    }
                }

                dialogPlayer.Bind(
                    interaction != null ? interaction.gameObject : null,
                    dialog != null ? dialog.gameObject : null,
                    body,
                    name,
                    profile);
            }
        }

        static Transform FindNamed(string objectName)
        {
            GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int t = 0; t < transforms.Length; t++)
                {
                    if (transforms[t].name == objectName)
                        return transforms[t];
                }
            }

            return null;
        }
    }
}
