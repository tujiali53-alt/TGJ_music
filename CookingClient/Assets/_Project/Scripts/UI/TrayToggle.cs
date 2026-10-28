using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class TrayToggle : MonoBehaviour
    {
        [SerializeField] GameObject trayPanel;
        [SerializeField] Button closeButton;

        static TrayToggle current;

        void Awake()
        {
            current = this;

            if (trayPanel == null)
            {
                Transform found = transform.Find("TrayPanel");
                if (found != null)
                    trayPanel = found.gameObject;
            }

            if (closeButton == null)
                closeButton = FindButton("TrayPanel/closeButton");

            if (trayPanel == null)
                Debug.LogError("Canvas 下需要有 TrayPanel。", this);

            if (closeButton == null)
                Debug.LogError("需要 TrayPanel/closeButton，并且带 Button。", this);
            else
                closeButton.onClick.AddListener(CloseTray);

            if (trayPanel != null)
                trayPanel.SetActive(false);
        }

        void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(CloseTray);

            if (current == this)
                current = null;
        }

        Button FindButton(string path)
        {
            Transform found = transform.Find(path);
            if (found == null)
                return null;

            return found.GetComponent<Button>();
        }

        public static void OpenCurrent()
        {
            if (current != null)
                current.OpenTray();
        }

        public void OpenTray()
        {
            if (trayPanel != null)
                trayPanel.SetActive(true);
        }

        public void CloseTray()
        {
            if (trayPanel != null)
                trayPanel.SetActive(false);
        }
    }
}
