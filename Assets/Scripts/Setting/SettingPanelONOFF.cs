using UnityEngine;

namespace Setting
{
    [DefaultExecutionOrder(-100)]
    public class SettingPanelONOFF : MonoBehaviour
    {
        public GameObject settingsPanel;
        public SettingUI settingUI;

        private void Update()
        {
            if (settingsPanel != null && settingUI != null && Input.GetKeyDown(KeyCode.Escape))
            {
                if (settingsPanel.activeSelf)
                {
                    settingUI.OnClickCancel();
                }
                else
                {
                    OpenPanel();
                }
            }
        }

        public void OpenPanel()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

    }
}
