using UnityEngine;

namespace Setting
{
    public class SettingPanelONOFF : MonoBehaviour
    {
        public GameObject settingsPanel;
        public SettingUI settingUI;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
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
            settingsPanel.SetActive(true);
            Time.timeScale = 0f; 
        }

    }
}
