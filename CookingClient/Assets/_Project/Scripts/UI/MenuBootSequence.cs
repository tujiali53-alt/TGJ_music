using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class MenuBootSequence : MonoBehaviour
    {
        [SerializeField] GameObject activeBg;
        [SerializeField] GameObject startBg;
        [SerializeField] GameObject hudPanel;
        [SerializeField] Image fadeOverlay;
        [SerializeField] float fadeDuration = 1f;

        bool started;
        Button activeButton;

        void Awake()
        {
            if (activeBg == null)
            {
                Transform found = transform.Find("ActiveBg");
                if (found != null)
                    activeBg = found.gameObject;
            }

            if (startBg == null)
            {
                Transform found = transform.Find("StartBg");
                if (found != null)
                    startBg = found.gameObject;
            }

            if (hudPanel == null)
            {
                Transform found = transform.Find("HUDPanel");
                if (found != null)
                    hudPanel = found.gameObject;
            }

            if (fadeOverlay == null)
            {
                Transform found = transform.Find("FadeOverlay");
                if (found != null)
                    fadeOverlay = found.GetComponent<Image>();
            }

            if (activeBg == null)
                Debug.LogError("MeunScene 的 Canvas 下需要已经放好 ActiveBg。", this);
            if (startBg == null)
                Debug.LogError("MeunScene 的 Canvas 下需要已经放好 StartBg。", this);
            if (hudPanel == null)
                Debug.LogError("MeunScene 的 Canvas 下需要已经放好 HUDPanel。", this);
            if (fadeOverlay == null)
                Debug.LogError("MeunScene 的 Canvas 下需要已经放好带 Image 的 FadeOverlay。", this);

            if (activeBg != null)
            {
                activeButton = activeBg.GetComponent<Button>();
                if (activeButton == null)
                    Debug.LogError("ActiveBg 上需要已经挂好 Button。", activeBg);
                else
                    activeButton.onClick.AddListener(BeginFade);
            }

            ShowBootState();
        }

        void OnDestroy()
        {
            if (activeButton != null)
                activeButton.onClick.RemoveListener(BeginFade);
        }

        void ShowBootState()
        {
            if (activeBg != null)
                activeBg.SetActive(true);
            if (startBg != null)
                startBg.SetActive(false);
            if (hudPanel != null)
                hudPanel.SetActive(false);
            if (fadeOverlay != null)
            {
                fadeOverlay.gameObject.SetActive(false);
                Color color = fadeOverlay.color;
                color.a = 0f;
                fadeOverlay.color = color;
            }
        }

        void BeginFade()
        {
            if (started)
                return;

            started = true;
            if (activeButton != null)
                activeButton.interactable = false;

            StartCoroutine(FadeThenReveal());
        }

        IEnumerator FadeThenReveal()
        {
            if (fadeOverlay == null)
            {
                RevealMenu();
                yield break;
            }

            fadeOverlay.gameObject.SetActive(true);
            Color color = fadeOverlay.color;
            color.r = 0f;
            color.g = 0f;
            color.b = 0f;
            color.a = 0f;
            fadeOverlay.color = color;

            float duration = Mathf.Max(0.01f, fadeDuration);
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
            RevealMenu();

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                color.a = 1f - t;
                fadeOverlay.color = color;
                yield return null;
            }

            color.a = 0f;
            fadeOverlay.color = color;
            fadeOverlay.gameObject.SetActive(false);
        }

        void RevealMenu()
        {
            if (activeBg != null)
                activeBg.SetActive(false);
            if (startBg != null)
                startBg.SetActive(true);
            if (hudPanel != null)
                hudPanel.SetActive(true);
        }
    }
}
