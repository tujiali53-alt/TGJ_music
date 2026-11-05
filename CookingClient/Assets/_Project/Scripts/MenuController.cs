using System.Collections;
using Cooking.Core;
using Cooking.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField] GameObject settingPanel;
    [SerializeField] GameObject creditPanel;
    [SerializeField] GameObject instructionPanel;
    [SerializeField] GameObject enterNamePanel;
    [SerializeField] GameObject chineseSelectedImage;
    [SerializeField] GameObject englishSelectedImage;
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] RectTransform menuCursor;
    [SerializeField] TMP_Text creditBodyText;
    [SerializeField] TMP_InputField nameInput;
    [SerializeField] Image fadeOverlay;
    [SerializeField] UiTextCatalog uiText;
    [SerializeField] float enterGameFadeDuration = 2f;

    bool startingGame;

    void Awake()
    {
        if (settingPanel == null)
        {
            Transform panel = transform.Find("SettingPanel");
            if (panel != null)
                settingPanel = panel.gameObject;
        }

        if (creditPanel == null)
        {
            Transform panel = transform.Find("CreditPanel");
            if (panel != null)
                creditPanel = panel.gameObject;
        }

        if (instructionPanel == null)
        {
            Transform panel = transform.Find("InsructionPanel");
            if (panel != null)
                instructionPanel = panel.gameObject;
        }

        if (enterNamePanel == null)
        {
            Transform panel = transform.Find("EnterNamePanel");
            if (panel != null)
                enterNamePanel = panel.gameObject;
        }

        if (nameInput == null && enterNamePanel != null)
        {
            Transform found = enterNamePanel.transform.Find("InputTMP");
            if (found == null)
                found = enterNamePanel.transform.Find("InputField (TMP)");
            if (found != null)
                nameInput = found.GetComponent<TMP_InputField>();
        }

        if (fadeOverlay == null)
        {
            Transform found = transform.Find("FadeOverlay");
            if (found != null)
                fadeOverlay = found.GetComponent<Image>();
        }

        if (chineseSelectedImage == null)
        {
            Transform found = transform.Find("SettingPanel/ChineseButton/Image");
            if (found != null)
                chineseSelectedImage = found.gameObject;
        }

        if (englishSelectedImage == null)
        {
            Transform found = transform.Find("SettingPanel/EnglishButton/Image");
            if (found != null)
                englishSelectedImage = found.gameObject;
        }

        if (bgmSlider == null)
        {
            Transform found = transform.Find("SettingPanel/BGMSlider");
            if (found != null)
                bgmSlider = found.GetComponent<Slider>();
        }

        if (sfxSlider == null)
        {
            Transform found = transform.Find("SettingPanel/SoundeffectsSlider");
            if (found != null)
                sfxSlider = found.GetComponent<Slider>();
        }

        if (settingPanel != null)
            settingPanel.SetActive(false);
        else
            Debug.LogError("MeunScene 的 Canvas 下需要已经放好 SettingPanel。", this);

        if (creditPanel != null)
        {
            creditPanel.SetActive(false);
            if (creditBodyText == null)
            {
                Transform found = creditPanel.transform.Find("Text (TMP)");
                if (found != null)
                    creditBodyText = found.GetComponent<TMP_Text>();
            }
        }
        else
            Debug.LogError("MeunScene 的 Canvas 下需要已经放好 CreditPanel。", this);

        if (creditBodyText == null)
            Debug.LogError("CreditPanel 下需要有 Text (TMP)。", this);

        if (uiText == null)
            Debug.LogError("MenuController 需要绑定 UiTextCatalog。", this);

        if (instructionPanel != null)
            instructionPanel.SetActive(false);
        else
            Debug.LogError("MeunScene 的 Canvas 下需要已经放好 InsructionPanel。", this);

        if (enterNamePanel != null)
            enterNamePanel.SetActive(false);
        else
            Debug.LogError("MeunScene 的 Canvas 下需要已经放好 EnterNamePanel。", this);

        if (nameInput == null)
            Debug.LogError("EnterNamePanel 下需要有 InputTMP（或 InputField (TMP)）。", this);

        if (fadeOverlay == null)
            Debug.LogError("MeunScene 的 Canvas 下需要已经放好带 Image 的 FadeOverlay。", this);

        ResolveMenuCursor();

        Bind("HUDPanel/StartButton", OpenEnterName);
        if (!BindAim("HUDPanel/SettingButton", OpenSettings))
            Debug.LogError("MeunScene 的 HUDPanel 下需要已经放好带 Button 的 SettingButton。", this);
        if (!BindAim("HUDPanel/ExitGameButton", ExitGame))
            Debug.LogError("MeunScene 的 HUDPanel 下需要已经放好带 Button 的 ExitGameButton。", this);
        Bind("SettingPanel/CloseButton", CloseSettings);

        if (!BindAim("HUDPanel/CreditButton", OpenCredits))
            Debug.LogError("MeunScene 的 HUDPanel 下需要已经放好带 Button 的 CreditButton。", this);

        Bind("CreditPanel/CloseButton", CloseCredits);

        if (!Bind("SettingPanel/InstructionsButton", OpenInstructions))
            Debug.LogError("MeunScene 的 SettingPanel 下需要已经放好带 Button 的 InstructionsButton。", this);

        if (!Bind("InsructionPanel/CloseButton", CloseInstructions))
            Debug.LogError("MeunScene 的 InsructionPanel 下需要已经放好带 Button 的 CloseButton。", this);

        if (!Bind("SettingPanel/ChineseButton", SelectChinese))
            Debug.LogError("MeunScene 的 SettingPanel 下需要已经放好带 Button 的 ChineseButton。", this);

        if (!Bind("SettingPanel/EnglishButton", SelectEnglish))
            Debug.LogError("MeunScene 的 SettingPanel 下需要已经放好带 Button 的 EnglishButton。", this);

        if (!Bind("EnterNamePanel/ComfirmButton", ConfirmNameAndStart))
            Debug.LogError("EnterNamePanel 下需要已经放好带 Button 的 ComfirmButton。", this);

        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        else
            Debug.LogError("MeunScene 的 SettingPanel 下需要已经放好 BGMSlider。", this);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        else
            Debug.LogError("MeunScene 的 SettingPanel 下需要已经放好 SoundeffectsSlider。", this);

        RefreshLanguageMarks();
        ApplyCreditBody();
        SyncSlidersFromAudio();
        GameLanguage.Changed += OnLanguageChanged;
    }

    void OnDestroy()
    {
        GameLanguage.Changed -= OnLanguageChanged;
        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
    }

    void OnLanguageChanged()
    {
        RefreshLanguageMarks();
        ApplyCreditBody();
    }

    public void OpenEnterName()
    {
        if (startingGame)
            return;

        AudioManager.PlayClick();
        if (settingPanel != null)
            settingPanel.SetActive(false);
        if (creditPanel != null)
            creditPanel.SetActive(false);
        if (instructionPanel != null)
            instructionPanel.SetActive(false);
        if (enterNamePanel != null)
            enterNamePanel.SetActive(true);
        if (nameInput != null)
            nameInput.ActivateInputField();
    }

    public void ConfirmNameAndStart()
    {
        if (startingGame)
            return;

        AudioManager.PlayClick();
        string playerName = nameInput != null ? nameInput.text.Trim() : "";
        StartCoroutine(FadeThenStartGame(playerName));
    }

    IEnumerator FadeThenStartGame(string playerName)
    {
        startingGame = true;

        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            Color color = fadeOverlay.color;
            color.r = 0f;
            color.g = 0f;
            color.b = 0f;
            color.a = 0f;
            fadeOverlay.color = color;

            float duration = Mathf.Max(0.01f, enterGameFadeDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                color.a = t;
                fadeOverlay.color = color;
                yield return null;
            }

            color.a = 1f;
            fadeOverlay.color = color;
        }

        GameRoot root = GameRoot.Instance;
        if (root == null || root.Flow == null)
        {
            Debug.LogError("MeunScene 里需要已经放好带 SceneFlow 的 GameRoot。");
            startingGame = false;
            yield break;
        }

        root.NewGame();
        if (root.Session != null)
            root.Session.PlayerName = playerName;
        root.Flow.Load(SceneNames.Outing);
    }

    public void OpenSettings()
    {
        AudioManager.PlayClick();
        if (instructionPanel != null)
            instructionPanel.SetActive(false);
        SyncSlidersFromAudio();
        RefreshLanguageMarks();
        if (settingPanel != null)
            settingPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        AudioManager.PlayCancel();
        if (settingPanel != null)
            settingPanel.SetActive(false);
    }

    public void OpenCredits()
    {
        AudioManager.PlayClick();
        ApplyCreditBody();
        if (creditPanel != null)
            creditPanel.SetActive(true);
    }

    void ApplyCreditBody()
    {
        if (creditBodyText == null || uiText == null)
            return;

        creditBodyText.text = uiText.Pick("ui.credit_body", GameLanguage.Current);
    }

    public void CloseCredits()
    {
        AudioManager.PlayCancel();
        if (creditPanel != null)
            creditPanel.SetActive(false);
    }

    public void OpenInstructions()
    {
        AudioManager.PlayClick();
        if (settingPanel != null)
            settingPanel.SetActive(false);
        if (instructionPanel != null)
            instructionPanel.SetActive(true);
    }

    public void CloseInstructions()
    {
        AudioManager.PlayCancel();
        if (instructionPanel != null)
            instructionPanel.SetActive(false);
        if (settingPanel != null)
            settingPanel.SetActive(true);
    }

    public void SelectChinese()
    {
        AudioManager.PlayClick();
        GameLanguage.Set(DiaryLanguage.Zh);
    }

    public void SelectEnglish()
    {
        AudioManager.PlayClick();
        GameLanguage.Set(DiaryLanguage.En);
    }

    public void ExitGame()
    {
        AudioManager.PlayClick();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void RefreshLanguageMarks()
    {
        bool chinese = GameLanguage.Current == DiaryLanguage.Zh;
        if (chineseSelectedImage != null)
            chineseSelectedImage.SetActive(chinese);
        if (englishSelectedImage != null)
            englishSelectedImage.SetActive(!chinese);
    }

    void SyncSlidersFromAudio()
    {
        if (bgmSlider != null)
            bgmSlider.SetValueWithoutNotify(AudioManager.MusicVolume);
        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(AudioManager.SfxVolume);
    }

    void OnBgmChanged(float value)
    {
        AudioManager.SetMusicVolume(value);
    }

    void OnSfxChanged(float value)
    {
        AudioManager.SetSfxVolume(value);
    }

    void ResolveMenuCursor()
    {
        if (menuCursor == null)
        {
            Transform found = transform.Find("HUDPanel/Cursor");
            if (found != null)
                menuCursor = found as RectTransform;
        }

        if (menuCursor == null)
            Debug.LogError("MeunScene 的 HUDPanel 下需要已经放好 Cursor。", this);
    }

    bool Bind(string path, UnityEngine.Events.UnityAction action)
    {
        Transform target = transform.Find(path);
        if (target == null)
            return false;

        Button button = target.GetComponent<Button>();
        if (button == null)
            return false;

        button.onClick.AddListener(action);
        return true;
    }

    bool BindAim(string path, UnityEngine.Events.UnityAction action)
    {
        Transform target = transform.Find(path);
        if (target == null)
            return false;

        Button button = target.GetComponent<Button>();
        if (button == null)
            return false;

        RectTransform rect = target as RectTransform;
        button.onClick.AddListener(() =>
        {
            PointCursorAt(rect);
            action();
        });
        return true;
    }

    const float MenuCursorY = -307f;

    void PointCursorAt(RectTransform button)
    {
        if (menuCursor == null || button == null)
            return;

        float buttonWidth = button.rect.width > 0f ? button.rect.width : button.sizeDelta.x;
        float centerX = button.anchoredPosition.x + (0.5f - button.pivot.x) * buttonWidth;

        // X 对准按钮水平中心，Y 固定为场景里的 -307。
        menuCursor.anchoredPosition = new Vector2(centerX, MenuCursorY);
        menuCursor.gameObject.SetActive(true);
    }
}
