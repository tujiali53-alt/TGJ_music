using UnityEngine;

// 挂在 Player 上（必须和 Animator、PlayerMotor 在同一个物体上，Animation Event 才能找到这个函数）
// 走路动画里的 Animation Event 调用 Footstep()，这里转发给 Wwise
public class PlayerFootsteps : MonoBehaviour
{
    public AK.Wwise.Event footstep;                 // Wwise 里的脚步 Event

    [SerializeField] float minInterval = 0.15f;     // 两步之间的最短间隔：换方向时动画会从头播，防止两步贴得太近
    float lastStepTime = -1f;

    // Animation Event 里填的 Function 名必须是 Footstep
    public void Footstep()
    {
        if (Time.time - lastStepTime < minInterval) return;
        lastStepTime = Time.time;
        AudioManager.Post(footstep);                // 和其他音效一样走 AudioManager
    }
}
