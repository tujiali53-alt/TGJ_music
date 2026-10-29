using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I;

    public AK.Wwise.Event playBGM;
    public AK.Wwise.Event stopBGM;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }  // 防止重复保留多个AudioManager
        I = this;
        DontDestroyOnLoad(gameObject);                   // 切换Scene不销毁AudioManager，以便音乐不中断
    }

    void Start()
    {
        if (I == this) playBGM.Post(gameObject);          // 整局只发这一次
    }

    // 所有音频都挂在AudioManager上，防止原物体被销毁中断音频
    public static void Post(AK.Wwise.Event e)
    {
        if (I != null && e != null && e.IsValid()) e.Post(I.gameObject);  // 防止Event没填时报错
    }

    public static void SetSwitch(string group, string value)
    {
        if (I != null) AkUnitySoundEngine.SetSwitch(group, value, I.gameObject);
    }
}