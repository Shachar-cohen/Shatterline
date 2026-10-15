using UnityEngine;
using UnityEngine.UI;

namespace Shatterline
{
    // All panels, buttons and callbacks are saved in MainMenu.unity.
    public sealed class MainMenu : MonoBehaviour
    {
        public GameObject HomePanel, InstructionsPanel, SettingsPanel, CreditsPanel, LeaderboardPanel;
        public Text CreditsText, MasterLabel, SfxLabel, UiLabel;
        public Text[] LeaderboardCells;
        public Slider MasterSlider, SfxSlider, UiSlider;
        public AudioSystem Audio;
        public ProjectCredits Credits;
        private void Awake() { SceneFlow.RestoreClock(); Select(HomePanel); }
        private void Start() { SyncSliders(); }
        private void LateUpdate()
        {
            if (Audio == null) return;
            MasterLabel.text = "MASTER " + Mathf.RoundToInt(RunState.Volume * 100) + "%";
            SfxLabel.text = "SFX " + Mathf.RoundToInt(Audio.SfxVolume * 100) + "%";
            UiLabel.text = "UI " + Mathf.RoundToInt(Audio.UiVolume * 100) + "%";
        }
        private void Select(GameObject selected)
        {
            HomePanel.SetActive(selected == HomePanel);
            InstructionsPanel.SetActive(selected == InstructionsPanel);
            SettingsPanel.SetActive(selected == SettingsPanel);
            CreditsPanel.SetActive(selected == CreditsPanel);
            LeaderboardPanel.SetActive(selected == LeaderboardPanel);
        }
        private void Click() { if (Application.isPlaying && Audio != null) Audio.Click(); }
        private void SyncSliders()
        {
            if (Audio == null) return;
            MasterSlider.SetValueWithoutNotify(RunState.Volume);
            SfxSlider.SetValueWithoutNotify(Audio.SfxVolume);
            UiSlider.SetValueWithoutNotify(Audio.UiVolume);
        }
        public void ShowHome() { Select(HomePanel); Click(); }
        public void ShowInstructions() { Select(InstructionsPanel); Click(); }
        public void ShowSettings() { Select(SettingsPanel); SyncSliders(); Click(); }
        public void ShowCredits() { Select(CreditsPanel); CreditsText.text = Credits != null ? Credits.DisplayText : "SHATTERLINE"; Click(); }
        public void ShowLeaderboard() { Select(LeaderboardPanel); var columns = Leaderboard.TopTenCells(); for (int i = 0; i < columns.Length; i++) LeaderboardCells[i].text = columns[i]; Click(); }
        public void StartRun() { SceneFlow.StartRun(); }
        public void ExitGame() { SceneFlow.Quit(); }
    }
}
