using Cooking.Core;
using Cooking.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class SettingsPanelController : MonoBehaviour
    {
        [SerializeField] GameObject settingPanel;
        [SerializeField] GameObject instructionPanel;
        [SerializeField] GameObject chineseSelectedImage;
        [SerializeField] GameObject englishSelectedImage;
        [SerializeField] Slider bgmSlider;
        [SerializeField] Slider sfxSlider;
        [SerializeField] Button openSettingsButton;

        void Awake()
        {
            if (settingPanel == null)
            {
                Transform found = transform.Find("SettingPanel");
                if (found != null)
                    settingPanel = found.gameObject;
            }

            if (instructionPanel == null)
            {
                Transform found = transform.Find("InsructionPanel");
                if (found != null)
                    instructionPanel = found.gameObject;
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

            if (openSettingsButton == null)
                openSettingsButton = FindButton("InteractionPanel/SettingButton");

            if (settingPanel != null)
                settingPanel.SetActive(false);
            else
                Debug.LogError("Canvas 下需要已经放好 SettingPanel。", this);

            if (instructionPanel != null)
                instructionPanel.SetActive(false);
            else
                Debug.LogError("Canvas 下需要已经放好 InsructionPanel。", this);

            if (openSettingsButton == null)
                Debug.LogError("需要 InteractionPanel/SettingButton，并且带 Button。", this);
            else
                openSettingsButton.onClick.AddListener(OpenSettings);

            if (!Bind("SettingPanel/CloseButton", CloseSettings))
                Debug.LogError("SettingPanel 下需要已经放好带 Button 的 CloseButton。", this);

            if (!Bind("SettingPanel/InstructionsButton", OpenInstructions))
                Debug.LogError("SettingPanel 下需要已经放好带 Button 的 InstructionsButton。", this);

            if (!Bind("InsructionPanel/CloseButton", CloseInstructions))
                Debug.LogError("InsructionPanel 下需要已经放好带 Button 的 CloseButton。", this);

            if (!Bind("SettingPanel/ChineseButton", SelectChinese))
                Debug.LogError("SettingPanel 下需要已经放好带 Button 的 ChineseButton。", this);

            if (!Bind("SettingPanel/EnglishButton", SelectEnglish))
                Debug.LogError("SettingPanel 下需要已经放好带 Button 的 EnglishButton。", this);

            if (bgmSlider != null)
                bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            else
                Debug.LogError("SettingPanel 下需要已经放好 BGMSlider。", this);

            if (sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            else
                Debug.LogError("SettingPanel 下需要已经放好 SoundeffectsSlider。", this);

            RefreshLanguageMarks();
            SyncSlidersFromAudio();
        }

        void OnDestroy()
        {
            if (openSettingsButton != null)
                openSettingsButton.onClick.RemoveListener(OpenSettings);

            if (bgmSlider != null)
                bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);

            if (sfxSlider != null)
                sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
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

        Button FindButton(string path)
        {
            Transform found = transform.Find(path);
            if (found == null)
                return null;

            return found.GetComponent<Button>();
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
}
