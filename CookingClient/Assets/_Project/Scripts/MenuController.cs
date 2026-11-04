using Cooking.Core;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField] GameObject settingPanel;
    [SerializeField] GameObject creditPanel;

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

        if (creditPanel != null)
            creditPanel.SetActive(false);
        else
            Debug.LogError("MeunScene 的 Canvas 下需要已经放好 CreditPanel。", this);

        Bind("HUDPanel/StartButton", StartGame);
        Bind("HUDPanel/SettingButton", OpenSettings);
        Bind("HUDPanel/ExitGameButton", ExitGame);
        Bind("SettingPanel/CloseButton", CloseSettings);

        if (!Bind("HUDPanel/CreditButton", OpenCredits))
            Debug.LogError("MeunScene 的 HUDPanel 下需要已经放好带 Button 的 CreditButton。", this);

        Bind("CreditPanel/CloseButton", CloseCredits);
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

    public void ExitGame()
    {
        AudioManager.PlayClick();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
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
