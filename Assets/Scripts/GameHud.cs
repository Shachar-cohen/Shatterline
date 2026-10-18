using UnityEngine;
using UnityEngine.UI;

namespace Shatterline
{
    // Scene-authored uGUI view. Object references and button events are visible in Inspector.
    public sealed class GameHud : MonoBehaviour
    {
        public RunGame Game;
        public AudioSystem Audio;
        public Text HealthText, EnergyText, StatusText, CoreText, ObjectiveText, UploadText, BossText, HintText;
        public Image HealthFill, CoreFill, UploadFill, StationFill, BossFill;
        public GameObject CorePanel, UploadPanel, BossPanel, HintPanel, HitFlash;
        public GameObject PausePanel, WinPanel, TransitionPanel, PowerPanel, StoryPanel, ErrorPanel;
        public Text PauseTitle, PauseText, TransitionText, StoryTitle, StoryText, ErrorText, ResultText, ResultMessage;
        public Text[] LeaderboardCells;
        public Button ResumeButton, ContinueButton, SaveButton;
        public Button[] ChoiceButtons;
        public Text[] ChoiceTexts;
        public InputField PlayerName;
        public GameObject SaveControls, PauseSettings;
        public Slider MasterSlider, SfxSlider, UiSlider;
        public Text MasterLabel, SfxLabel, UiLabel;
        private bool displayedWin, displayedPause;

        private void LateUpdate()
        {
            if (Game == null) return;
            var state = Game.CaptureUi();
            HealthText.text = "ACT " + state.Act + "   HP " + state.Health + "   LIVES " + RunState.Lives;
            EnergyText.text = "EN " + state.Energy; StatusText.text = state.Status;
            HealthFill.fillAmount = state.Health / 100f;
            CorePanel.SetActive(state.Carrying);
            CoreText.text = "CORE " + state.CoreHealth + "%   HEAT " + Game.CarrierHeatPercent + "%";
            CoreFill.fillAmount = state.CoreHealth / 100f;
            ObjectiveText.text = state.Objective;
            UploadPanel.SetActive(state.Uploading); UploadText.text = state.UploadText;
            UploadFill.fillAmount = state.UploadProgress; StationFill.fillAmount = state.StationHealth / 100f;
            BossPanel.SetActive(state.ShowBoss); BossText.text = "GUARDIAN " + state.BossHealth + "/16";
            BossFill.fillAmount = state.BossHealth / 16f;
            HintPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, state.Uploading ? -90f : -42f);
            HintPanel.SetActive(state.ShowHint); HintText.text = state.Hint;
            HitFlash.SetActive(state.HitFlash);
            PausePanel.SetActive(state.Paused || state.Finished && !state.Won);
            PauseTitle.text = state.Finished ? "THE SIGNAL IS LOST" : "PAUSED";
            PauseText.text = state.Finished ? "Score: " + RunState.Score + "  |  Deaths: " + RunState.Deaths +
                "\nThe city will need another runner." : ControlsText;
            ResumeButton.gameObject.SetActive(state.Paused); PauseSettings.SetActive(state.Paused);
            if (state.Paused && !displayedPause)
            {
                MasterSlider.SetValueWithoutNotify(RunState.Volume);
                SfxSlider.SetValueWithoutNotify(Audio.SfxVolume);
                UiSlider.SetValueWithoutNotify(Audio.UiVolume);
            }
            displayedPause = state.Paused;
            MasterLabel.text = "MASTER " + Mathf.RoundToInt(RunState.Volume * 100) + "%";
            SfxLabel.text = "SFX " + Mathf.RoundToInt(Audio.SfxVolume * 100) + "%";
            UiLabel.text = "UI " + Mathf.RoundToInt(Audio.UiVolume * 100) + "%";
            WinPanel.SetActive(state.Finished && state.Won && !state.Story);
            if (state.Finished && state.Won && !displayedWin) RefreshLeaderboard();
            displayedWin = state.Finished && state.Won;
            ResultText.text = state.ResultText;
            ResultMessage.text = state.Assisted ? "Developer-assisted run: leaderboard disabled." : state.ResultMessage;
            SaveControls.SetActive(!state.ResultSaved && !state.Assisted);
            TransitionPanel.SetActive(state.Transitioning); TransitionText.text = state.TransitionText;
            PowerPanel.SetActive(state.PowerChoice);
            StoryPanel.SetActive(state.Story); StoryTitle.text = state.StoryTitle; StoryText.text = state.StoryText;
            ContinueButton.gameObject.SetActive(state.Choices.Length == 0);
            for (int i = 0; i < ChoiceButtons.Length; i++)
            {
                bool visible = state.Story && i < state.Choices.Length;
                ChoiceButtons[i].gameObject.SetActive(visible);
                if (visible) ChoiceTexts[i].text = state.Choices[i];
            }
            ErrorPanel.SetActive(!string.IsNullOrEmpty(state.Error)); ErrorText.text = state.Error;
        }
        public const string ControlsText =
            "A/D MOVE  ·  W TAP/HOLD/DOUBLE JUMP  ·  S CROUCH (AIR: FAST DROP)\n" +
            "R CLIMB UP  ·  DOWN ARROW CLIMB DOWN  ·  SPACE DASH  ·  ESC PAUSE\n" +
            "LEFT FIRE  ·  RIGHT ARC (II+)  ·  Q SHIELD (II+)  ·  E WAVE (III+)\n" +
            "F DRONE (25 EN/BLOCK)  ·  SHIFT POWER  ·  T UNSTUCK";
        public void ResumeGame() { if (Game != null) Game.ResumeGame(); }
        public void RestartRun() { if (Game != null) Game.RestartRun(); }
        public void ReturnToMenu() { if (Game != null) Game.ReturnToMenu(); }
        public void QuitGame() { if (Game != null) Game.QuitGame(); }
        public void ChooseEmp() { if (Game != null) Game.ChooseEmp(); }
        public void ChooseAegis() { if (Game != null) Game.ChooseAegis(); }
        public void ChooseRepair() { if (Game != null) Game.ChooseRepair(); }
        public void ContinueDialogue() { if (Game != null) Game.ContinueDialogue(); }
        public void ChooseDialogue0() { if (Game != null) Game.ChooseDialogue0(); }
        public void ChooseDialogue1() { if (Game != null) Game.ChooseDialogue1(); }
        public void ChooseDialogue2() { if (Game != null) Game.ChooseDialogue2(); }
        public void SetMaster(float value) { if (Audio != null) Audio.SetMaster(value); }
        public void SetSfx(float value) { if (Audio != null) Audio.SetSfx(value); }
        public void SetUi(float value) { if (Audio != null) Audio.SetUi(value); }
        public void SaveScore() { Game.SaveResult(PlayerName.text); RefreshLeaderboard(); }
        public void RefreshLeaderboard() { var columns = Leaderboard.TopTenCells(); for (int i = 0; i < columns.Length; i++) LeaderboardCells[i].text = columns[i]; }
    }
}
