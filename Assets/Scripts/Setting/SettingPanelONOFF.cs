using UnityEngine;

namespace Setting
{
    public class SettingPanelONOFF : MonoBehaviour
    {
        public GameObject settingsPanel;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && settingsPanel.activeSelf)
            {
                ClosePanel();
            }
            else if (Input.GetKeyDown(KeyCode.Escape) && !settingsPanel.activeSelf)
            {
                OpenPanel();
            }
        }
        public void OpenPanel()
        {
            settingsPanel.SetActive(true);

            Time.timeScale = 0f;
        }
        public void ClosePanel()
        {
            settingsPanel.SetActive(false);

            Time.timeScale = 1f;
        }
        public void TogglePanel()
        {
            if (settingsPanel.activeSelf)
                ClosePanel();
            else
                OpenPanel();
        }
    }
}
