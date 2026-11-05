using System.Collections;
using Cooking.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

// 所有音频都挂在 AudioManager 上播放，防止原物体被销毁时中断音频。
// 代码里不写 Wwise Event，在 Inspector 里选，防止改名失效。
public class AudioManager : MonoBehaviour
{
    public static AudioManager I;

    const string MusicVolumePrefs = "cooking.volume.music";
    const string SfxVolumePrefs = "cooking.volume.sfx";

    [Header("BGM")]
    public AK.Wwise.Event playBGM;
    public AK.Wwise.Event stopBGM;
    [Tooltip("Music_BGM_Stage：第 1 格 = 第 1 天，第 2 格 = 第 2 天……天数超过格数时沿用最后一格")]
    public AK.Wwise.State[] dayStages;

    [Header("夜晚环境音")]
    public AK.Wwise.Event playAmbience;
    public AK.Wwise.Event stopAmbience;

    [Header("UI")]
    public AK.Wwise.Event uiClick;
    public AK.Wwise.Event uiCancel;
    public AK.Wwise.Event uiPageTurn;

    [Header("烹饪")]
    public AK.Wwise.Event playCutting;
    public AK.Wwise.Event playEat;
    public AK.Wwise.Event playBoiling;
    public AK.Wwise.Event stopBoiling;
    public AK.Wwise.Event playFrying;
    public AK.Wwise.Event stopFrying;

    [Header("调料")]
    public AK.Wwise.Event playOil;
    public AK.Wwise.Event playSalt;
    public AK.Wwise.Event playPepper;
    public AK.Wwise.Event playSugar;
    public AK.Wwise.Event playSauce;

    [Header("夜晚探索")]
    public AK.Wwise.Event playDiceRoll;

    [Header("音量 RTPC（0–100）")]
    public AK.Wwise.RTPC musicVolumeRtpc;
    public AK.Wwise.RTPC sfxVolumeRtpc;
    [Tooltip("当前 SoundBank 若仍是旧总线曲线（0→-100、50→-4、100→0），勾选此项，滑条才会按听感线性变化。Wwise 已改成 0→-96、100→0 并重新 Generate 后请取消勾选。")]
    public bool legacyBusVolumeCurve = true;

    bool bgmStarted;
    bool ambienceOn;
    static float musicVolume = 1f;
    static float sfxVolume = 1f;

    public static float MusicVolume => musicVolume;
    public static float SfxVolume => sfxVolume;

    void Awake()
    {
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
        musicVolume = PlayerPrefs.GetFloat(MusicVolumePrefs, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumePrefs, 1f);
        ApplyVolumes();
    }

    void Start()
    {
        if (I == this)
            StartCoroutine(StartWhenBankReady());
    }

    IEnumerator StartWhenBankReady()
    {
        // Awake 时 Init/MainAudio 可能还没就绪；等引擎初始化后再多等两帧写 RTPC。
        while (!AkUnitySoundEngine.IsInitialized())
            yield return null;

        yield return null;
        yield return null;
        if (I != this)
            yield break;

        ApplyVolumes();
        ApplyPhaseFor(SceneManager.GetActiveScene().name);
    }

    public void ApplyPhaseFor(string sceneName)
    {
        string state = "Menu";
        if (sceneName == SceneNames.Cooking)
            state = "Day";
        else if (sceneName == SceneNames.Outing)
            state = "Night";

        // Music_GamePhase 由各场景上挂的 AkState 设置，这里不再设 State
        if (state != "Day")
        {
            Post(stopBoiling);
            Post(stopFrying);
        }
        else
        {
            // 正常流程已在 GameRoot.SleepAndEnterDay() 里设过；这里防止直接从厨房启动测试出现问题
            GameRoot root = GameRoot.Instance;
            if (root != null && root.Session != null)
                SetDayStage(root.Session.DayIndex);
        }

        // 进入户外场景时这个函数会被调用两次，用 ambienceOn 防止播放两次环境音
        if (state == "Night" && !ambienceOn)
        {
            Post(playAmbience);
            ambienceOn = true;
        }
        else if (state != "Night" && ambienceOn)
        {
            Post(stopAmbience);
            ambienceOn = false;
        }

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

    // 按天数切换白天 BGM 版本
    public static void SetDayStage(int day)
    {
        if (I == null || I.dayStages == null || I.dayStages.Length == 0)
            return;

        int index = Mathf.Clamp(day - 1, 0, I.dayStages.Length - 1);
        AK.Wwise.State stage = I.dayStages[index];
        if (stage != null)
            stage.SetValue();
    }

    public static void PlayCut(string ingredientId)
    {
        string value = CutSwitch(ingredientId);
        if (!string.IsNullOrEmpty(value))
            SetSwitch("Ingredient", value);

        if (I != null)
            Post(I.playCutting);
    }

    public static void PlayEat(string recipeId)
    {
        string dish = DishSwitch(recipeId);
        if (string.IsNullOrEmpty(dish))
            dish = "Food_Default";   // 做出失败料理时用默认吃饭声
        SetSwitch("Dish", dish);

        if (I != null)
            Post(I.playEat);
    }

    public static void PlaySeasoning(string seasoningId)
    {
        if (I == null)
            return;

        if (seasoningId == "season_oil")
            Post(I.playOil);
        else if (seasoningId == "season_salt")
            Post(I.playSalt);
        else if (seasoningId == "season_pepper")
            Post(I.playPepper);
        else if (seasoningId == "season_sugar")
            Post(I.playSugar);
        else if (seasoningId == "season_sauce")
            Post(I.playSauce);
    }

    public static void PlayClick()
    {
        if (I != null)
            Post(I.uiClick);
    }

    public static void PlayCancel()
    {
        if (I != null)
            Post(I.uiCancel);
    }

    public static void PlayPage()
    {
        if (I != null)
            Post(I.uiPageTurn);
    }

    public static void SetBoiling(bool on)
    {
        if (I != null)
            Post(on ? I.playBoiling : I.stopBoiling);
    }

    public static void SetFrying(bool on)
    {
        if (I != null)
            Post(on ? I.playFrying : I.stopFrying);
    }

    public static void PlayDiceRoll()
    {
        if (I != null)
            Post(I.playDiceRoll);
    }

    public static void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumePrefs, musicVolume);
        PlayerPrefs.Save();
        ApplyMusicVolume();
    }

    public static void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumePrefs, sfxVolume);
        PlayerPrefs.Save();
        ApplySfxVolume();
    }

    static void ApplyVolumes()
    {
        ApplyMusicVolume();
        ApplySfxVolume();
    }

    static void ApplyMusicVolume()
    {
        ApplyRtpc(I != null ? I.musicVolumeRtpc : null, musicVolume);
    }

    static void ApplySfxVolume()
    {
        ApplyRtpc(I != null ? I.sfxVolumeRtpc : null, sfxVolume);
    }

    static void ApplyRtpc(AK.Wwise.RTPC rtpc, float normalized)
    {
        if (rtpc == null || !rtpc.IsValid() || !AkUnitySoundEngine.IsInitialized())
            return;

        float unit = Mathf.Clamp01(normalized);
        float rtpcValue = (I != null && I.legacyBusVolumeCurve)
            ? SliderToLegacyBusRtpc(unit)
            : unit * 100f;

        // 总线音量只用全局 RTPC；bypass 内部插值，避免拖滑条时听感滞后。
        AkUnitySoundEngine.SetRTPCValue(
            rtpc.Id,
            rtpcValue,
            AkUnitySoundEngine.AK_INVALID_GAME_OBJECT,
            0,
            AkCurveInterpolation.AkCurveInterpolation_Linear,
            true);
    }

    // 旧总线曲线：(0,-100) (50,-4) (100,0)。把滑条 0–1 先映射到目标 dB，再反查 RTPC。
    static float SliderToLegacyBusRtpc(float unit)
    {
        float desiredDb = Mathf.Lerp(-96f, 0f, unit);
        if (desiredDb <= -4f)
            return 50f * (desiredDb + 100f) / 96f;

        return 50f + 50f * (desiredDb + 4f) / 4f;
    }

    static string CutSwitch(string ingredientId)
    {
        if (ingredientId == "ing_mushroom")
            return "Mushroom";
        if (ingredientId == "ing_tomato")
            return "Tomato";
        if (ingredientId == "ing_doneegg")
            return "DoneEgg";
        if (ingredientId == "ing_cornfish")
            return "CornFish";
        if (ingredientId == "ing_berry")
            return "Berry";
        if (ingredientId == "ing_pumpkin")
            return "Pumpkin";
        if (ingredientId == "ing_onion")
            return "Onion";
        if (ingredientId == "ing_beef")
            return "Beef";
        if (ingredientId == "ing_cucumber")
            return "Cucumber";
        if (ingredientId == "ing_yam")
            return "Yam";
        if (ingredientId == "ing_shrimp")
            return "Shrimp";
        return "";
    }

    static string DishSwitch(string recipeId)
    {
        if (recipeId == "recipe_day1")
            return "Food_Soup";
        if (recipeId == "recipe_day2")
            return "Food_RainbowEgg";
        if (recipeId == "recipe_day3")
            return "Food_FriedFish";
        if (recipeId == "recipe_day4")
            return "Food_JellyShip";
        if (recipeId == "recipe_day5")
            return "Food_SeafoodRice";
        if (recipeId == "recipe_day6")
            return "Food_YamPie";
        if (recipeId == "recipe_day7")
            return "Food_Hotpot";
        return "";
    }

    public static void SetSwitch(string group, string value)
    {
        if (I != null)
            AkUnitySoundEngine.SetSwitch(group, value, I.gameObject);
    }
}
