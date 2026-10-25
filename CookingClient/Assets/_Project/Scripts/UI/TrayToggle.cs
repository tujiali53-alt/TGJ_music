using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class TrayToggle : MonoBehaviour
    {
        [SerializeField] GameObject trayPanel;
        [SerializeField] Button inventoryButton;
        [SerializeField] Button closeButton;

        void Awake()
        {
            if (trayPanel == null)
            {
                Transform found = transform.Find("TrayPanel");
                if (found != null)
                    trayPanel = found.gameObject;
            }

            if (inventoryButton == null)
                inventoryButton = FindButton("InteractionPanel/InventoryButton");

            if (closeButton == null)
                closeButton = FindButton("TrayPanel/closeButton");

            if (trayPanel == null)
                Debug.LogError("Canvas 下需要有 TrayPanel。", this);

            if (inventoryButton == null)
                Debug.LogError("需要 InteractionPanel/InventoryButton，并且带 Button。", this);
            else
                inventoryButton.onClick.AddListener(OpenTray);

            if (closeButton == null)
                Debug.LogError("需要 TrayPanel/closeButton，并且带 Button。", this);
            else
                closeButton.onClick.AddListener(CloseTray);

            if (trayPanel != null)
                trayPanel.SetActive(false);
        }

        Button FindButton(string path)
        {
            Transform found = transform.Find(path);
            if (found == null)
                return null;

            return found.GetComponent<Button>();
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
