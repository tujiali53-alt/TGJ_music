using System.Collections;
using Cooking.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I;

    const string MusicVolumePrefs = "cooking.volume.music";
    const string SfxVolumePrefs = "cooking.volume.sfx";
    const string MusicVolumeRtpc = "Volume_Music";
    const string SfxVolumeRtpc = "Volume_SFX";

    public AK.Wwise.Event playBGM;
    public AK.Wwise.Event stopBGM;

    bool bgmStarted;
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

        AkUnitySoundEngine.SetState("Music_GamePhase", state);
        if (state != "Day")
        {
            Post("Stop_Boiling");
            Post("Stop_Frying");
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

    public static void Post(string eventName)
    {
        if (I == null || string.IsNullOrEmpty(eventName))
            return;

        AkUnitySoundEngine.PostEvent(eventName, I.gameObject);
    }

    public static void PlayCut(string ingredientId)
    {
        string value = CutSwitch(ingredientId);
        if (!string.IsNullOrEmpty(value))
            SetSwitch("Ingredient", value);

        Post("Play_Cutting");
    }

    public static void PlayEat(string recipeId)
    {
        string dish = DishSwitch(recipeId);
        if (!string.IsNullOrEmpty(dish))
            SetSwitch("Dish", dish);

        Post("Play_Eat");
    }

    public static void PlaySeasoning(string seasoningId)
    {
        if (seasoningId == "season_oil")
            Post("Play_Oil");
        else if (seasoningId == "season_salt")
            Post("Play_Salt");
        else if (seasoningId == "season_pepper")
            Post("Play_Pepper");
        else if (seasoningId == "season_sugar")
            Post("Play_Sugar");
        else if (seasoningId == "season_sauce")
            Post("Play_Sauce");
        else if (seasoningId == "season_spice")
            Post("Play_Spice");
    }

    public static void PlayClick()
    {
        Post("Play_UI_Click");
    }

    public static void PlayCancel()
    {
        Post("Play_UI_Cancel");
    }

    public static void PlayPage()
    {
        Post("Play_UI_PageTurn");
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
        AkUnitySoundEngine.SetRTPCValue(MusicVolumeRtpc, musicVolume * 100f);
    }

    static void ApplySfxVolume()
    {
        AkUnitySoundEngine.SetRTPCValue(SfxVolumeRtpc, sfxVolume * 100f);
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
