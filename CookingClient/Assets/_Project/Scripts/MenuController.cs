using Cooking.Core;
using Cooking.Data;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField] GameObject settingPanel;
    [SerializeField] GameObject creditPanel;
    [SerializeField] GameObject instructionPanel;
    [SerializeField] GameObject chineseSelectedImage;
    [SerializeField] GameObject englishSelectedImage;
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;

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
            creditPanel.SetActive(false);
        else
            Debug.LogError("MeunScene 的 Canvas 下需要已经放好 CreditPanel。", this);

        if (instructionPanel != null)
            instructionPanel.SetActive(false);
        else
            Debug.LogError("MeunScene 的 Canvas 下需要已经放好 InsructionPanel。", this);

        Bind("HUDPanel/StartButton", StartGame);
        Bind("HUDPanel/SettingButton", OpenSettings);
        Bind("HUDPanel/ExitGameButton", ExitGame);
        Bind("SettingPanel/CloseButton", CloseSettings);

        if (!Bind("HUDPanel/CreditButton", OpenCredits))
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

        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        else
            Debug.LogError("MeunScene 的 SettingPanel 下需要已经放好 BGMSlider。", this);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        else
            Debug.LogError("MeunScene 的 SettingPanel 下需要已经放好 SoundeffectsSlider。", this);

        RefreshLanguageMarks();
        SyncSlidersFromAudio();
    }

    void OnDestroy()
    {
        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
    }

    public void StartGame()
    {
        AudioManager.PlayClick();
        GameRoot root = GameRoot.Instance;
        if (root == null || root.Flow == null)
        {
            Debug.LogError("MeunScene 里需要已经放好带 SceneFlow 的 GameRoot。");
            return;
        }

        root.NewGame();
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
        if (creditPanel != null)
            creditPanel.SetActive(true);
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
        RefreshLanguageMarks();
    }

    public void SelectEnglish()
    {
        AudioManager.PlayClick();
        GameLanguage.Set(DiaryLanguage.En);
        RefreshLanguageMarks();
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
}
