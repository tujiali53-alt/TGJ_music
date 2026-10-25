using Cooking.Core;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField] GameObject settingPanel;

    void Awake()
    {
        if (settingPanel == null)
        {
            Transform panel = transform.Find("SettingPanel");
            if (panel != null)
                settingPanel = panel.gameObject;
        }

        Bind("HUDPanel/StartButton", StartGame);
        Bind("HUDPanel/SettingButton", OpenSettings);
        Bind("HUDPanel/ExitGameButton", ExitGame);
        Bind("SettingPanel/CloseButton", CloseSettings);
    }

    public void StartGame()
    {
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
        if (settingPanel != null)
            settingPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingPanel != null)
            settingPanel.SetActive(false);
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void Bind(string path, UnityEngine.Events.UnityAction action)
    {
        Transform target = transform.Find(path);
        if (target == null)
            return;

        Button button = target.GetComponent<Button>();
        if (button == null)
            return;

        button.onClick.AddListener(action);
    }
}
