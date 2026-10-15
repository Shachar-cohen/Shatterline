namespace Shatterline
{
    // Presentation snapshot; no UI object is responsible for gameplay decisions.
    public struct RunUiState
    {
        public int Act, Health, Energy, CoreHealth, BossHealth;
        public bool Carrying, Uploading, ShowBoss, Paused, Finished, Won, Transitioning, PowerChoice, Story, HitFlash;
        public bool ShowHint, ResultSaved, Assisted;
        public float UploadProgress, StationHealth;
        public string Objective, Status, UploadText, Hint, StoryTitle, StoryText, TransitionText;
        public string Error, ResultText, ResultMessage;
        public string[] Choices;
    }
}
