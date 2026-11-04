using System.Collections;
using Cooking.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I;

    public AK.Wwise.Event playBGM;
    public AK.Wwise.Event stopBGM;

    bool bgmStarted;

    void Awake()
    {
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (I == this)
            StartCoroutine(StartWhenBankReady());
    }

    IEnumerator StartWhenBankReady()
    {
        yield return null;
        if (I != this)
            yield break;

        ApplyPhaseFor(SceneManager.GetActiveScene().name);
    }

    public void ApplyPhaseFor(string sceneName)
    {
        string state = "Menu";
        if (sceneName == SceneNames.Cooking)
            state = "Day";
        else if (sceneName == SceneNames.Outing)
            state = "Night";

        AkUnitySoundEngine.SetState("Music_GamePhase", state);
        if (bgmStarted || !playBGM.IsValid())
            return;

        playBGM.Post(gameObject);
        bgmStarted = true;
    }

    public static void Post(AK.Wwise.Event e)
    {
        if (I != null && e != null && e.IsValid())
            e.Post(I.gameObject);
    }

    public static void SetSwitch(string group, string value)
    {
        if (I != null)
            AkUnitySoundEngine.SetSwitch(group, value, I.gameObject);
    }
}
