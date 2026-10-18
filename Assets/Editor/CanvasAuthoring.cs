using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Shatterline;

// Creates real saved Canvas objects once in Edit mode. Runtime presenters only update them.
public static class CanvasAuthoring
{
    private static readonly Color Dark = new Color(.035f, .052f, .082f, .94f);
    private static readonly Color Orange = new Color(.95f, .34f, .15f);
    private static Font font;
    private static Font Font
    {
        get
        {
            if (font != null) return font;
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch (System.Exception) { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
            return font;
        }
    }
    private static RectTransform Root(string title)
    {
        var root = new GameObject(title, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        return root.GetComponent<RectTransform>();
    }
    private static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform)); var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); return rect;
    }
    private static RectTransform CenterPanel(Transform parent, string name, float w, float h)
    {
        var panel = Rect(parent, name, 0, 0, w, h);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f); panel.anchoredPosition = Vector2.zero;
        var image = panel.gameObject.AddComponent<Image>(); image.color = Dark; return panel;
    }
    private static GameObject Backdrop(Transform root, string name, Color color)
    {
        var rect = Rect(root, name, 0, 0, 0, 0); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero; rect.gameObject.AddComponent<Image>().color = color;
        return rect.gameObject;
    }
    private static Text Label(Transform parent, string name, string value, float x, float y, float w, float h, int size = 18)
    {
        var text = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Text>();
        text.font = Font; text.text = value; text.fontSize = size; text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft; text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate; text.raycastTarget = false; return text;
    }
    private static Button Button(Transform parent, string name, string value, float x, float y, float w, float h, UnityAction action)
    {
        var rect = Rect(parent, name, x, y, w, h); var image = rect.gameObject.AddComponent<Image>(); image.color = Orange;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = new Color(1f, .68f, .5f); colors.pressedColor = new Color(.75f, .5f, .4f); button.colors = colors;
        var text = Label(rect, "Label", value, 8, 5, w - 16, h - 10, 19); text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true; text.resizeTextMinSize = 14; text.resizeTextMaxSize = 19;
        if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action);
        return button;
    }
    private static Slider Slider(Transform parent, string name, float x, float y, float w, UnityAction<float> action)
    {
        var rect = Rect(parent, name, x, y, w, 20); var back = rect.gameObject.AddComponent<Image>(); back.color = new Color(.25f, .3f, .38f);
        var slider = rect.gameObject.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1;
        var fill = Rect(rect, "Fill", 0, 0, 0, 0); var fillImage = fill.gameObject.AddComponent<Image>(); fillImage.color = Orange;
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.pivot = new Vector2(0.5f, 0.5f);
        fill.anchoredPosition = Vector2.zero; fill.sizeDelta = Vector2.zero;
        var handle = Rect(rect, "Handle", 0, 0, 16, 4); var handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = Color.white;
        handle.anchorMin = Vector2.zero; handle.anchorMax = Vector2.one; handle.pivot = new Vector2(0.5f, 0.5f);
        handle.anchoredPosition = Vector2.zero; handle.sizeDelta = new Vector2(16, 4);
        slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handleImage;
        slider.SetValueWithoutNotify(.7f); UnityEventTools.AddPersistentListener(slider.onValueChanged, action); return slider;
    }
    private static Image Bar(Transform parent, string name, float x, float y, float w, float h, Color tint)
    {
        var back = Rect(parent, name + " background", x, y, w, h); back.gameObject.AddComponent<Image>().color = new Color(.38f, .2f, .24f);
        var fill = Rect(back, "Fill", 0, 0, w, h).gameObject.AddComponent<Image>(); fill.color = tint;
        fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = 1;
        return fill;
    }
    private static Text[] Table(Transform parent, float x, float y, float width)
    {
        float[] proportions = { .07f, .35f, .20f, .18f, .20f }; var cells = new Text[55];
        string[] headings = { "#", "PLAYER", "SCORE", "TIME", "DEATHS" };
        for (int row = 0; row < 11; row++)
        {
            float offset = 0;
            for (int column = 0; column < 5; column++)
            {
                float w = width * proportions[column];
                var text = Label(parent, "Row " + row + " " + headings[column], row == 0 ? headings[column] : "",
                    x + offset, y + row * 26, w - 5, 24, 14);
                text.resizeTextForBestFit = true; text.resizeTextMinSize = 9; text.resizeTextMaxSize = 14;
                if (row == 0) text.fontStyle = FontStyle.Bold;
                cells[row * 5 + column] = text; offset += w;
            }
        }
        return cells;
    }
    public static void MainMenu(MainMenu menu, AudioSystem audio, ProjectCredits credits)
    {
        var root = Root("Canvas / Main Menu"); Backdrop(root, "Menu shade", Dark);
        menu.Audio = audio; menu.Credits = credits;
        Label(root, "Title", "SHATTERLINE", 75, 65, 1000, 105, 65).fontStyle = FontStyle.Bold;
        Label(root, "Tagline", "THREE ACTS. ONE CITY. ONE SIGNAL LEFT TO SAVE.", 81, 178, 1100, 65, 21);
        var home = Rect(root, "Home", 81, 235, 1120, 480); menu.HomePanel = home.gameObject;
        Label(home, "Description", "An original 2.5D action adventure through a collapsing future. Leap across fractured rooftops, break through barriers, and silence the guardian before time runs out.", 0, 0, 565, 130, 21);
        Button(home, "Start", "START RUN", 0, 151, 335, 52, menu.StartRun);
        Button(home, "Instructions", "HOW TO PLAY", 0, 206, 335, 50, menu.ShowInstructions);
        Button(home, "Settings", "SETTINGS", 0, 261, 335, 50, menu.ShowSettings);
        Button(home, "Credits", "CREDITS", 0, 316, 335, 50, menu.ShowCredits);
        Button(home, "Leaderboard", "LEADERBOARD", 0, 371, 335, 50, menu.ShowLeaderboard);
        Button(home, "Exit", "EXIT", 0, 426, 335, 50, menu.ExitGame);
        var instructions = Rect(root, "How to Play", 81, 257, 1120, 455); menu.InstructionsPanel = instructions.gameObject;
        Label(instructions, "Controls", GameHud.ControlsText + "\n\nTap W for a low jump; hold for a high jump. Release quickly and press again for the double jump. Press S airborne to fast drop.\n\nInside climb zones, press R to climb up and Down Arrow to descend. Press ESC to pause or resume at any time.\n\nCarry two cyan cores to terminals per act. Protect each terminal until 100% and clear its attackers. Break the three relays and defeat the Act III guardian to open the final route.\n\nF deploys or recalls the drone: -25% movement, 25 energy per intercept, 8-second cooldown. LOW/HIGH grids pulse transparently while OPEN and block while LIVE.", 0, 0, 1120, 370, 16);
        Button(instructions, "Back", "BACK", 0, 394, 335, 52, menu.ShowHome);
        var settings = Rect(root, "Settings", 81, 257, 1120, 455); menu.SettingsPanel = settings.gameObject;
        menu.MasterLabel = Label(settings, "Master label", "MASTER", 0, 20, 260, 30, 21);
        menu.MasterSlider = Slider(settings, "Master volume", 290, 24, 400, audio.SetMaster);
        menu.SfxLabel = Label(settings, "SFX label", "SFX", 0, 85, 260, 30, 21);
        menu.SfxSlider = Slider(settings, "SFX volume", 290, 89, 400, audio.SetSfx);
        menu.UiLabel = Label(settings, "UI label", "UI", 0, 150, 260, 30, 21);
        menu.UiSlider = Slider(settings, "UI volume", 290, 154, 400, audio.SetUi);
        Button(settings, "Back", "BACK", 0, 394, 335, 52, menu.ShowHome);
        var creditPanel = Rect(root, "Credits", 81, 257, 1120, 455); menu.CreditsPanel = creditPanel.gameObject;
        menu.CreditsText = Label(creditPanel, "Credits", credits.DisplayText, 0, 0, 1120, 375, 18);
        Button(creditPanel, "Back", "BACK", 0, 394, 335, 52, menu.ShowHome);
        var leaderboard = Rect(root, "Leaderboard", 81, 257, 1120, 455); menu.LeaderboardPanel = leaderboard.gameObject;
        Label(leaderboard, "Heading", "LOCAL TOP 10 / BEST COMPLETED RUN PER NAME", 0, 0, 1000, 45, 21);
        menu.LeaderboardCells = Table(leaderboard, 0, 85, 850);
        Button(leaderboard, "Back", "BACK", 0, 394, 335, 52, menu.ShowHome);
        instructions.gameObject.SetActive(false); settings.gameObject.SetActive(false);
        creditPanel.gameObject.SetActive(false); leaderboard.gameObject.SetActive(false);
    }
    public static GameHud Gameplay(RunGame game, AudioSystem audio)
    {
        var root = Root("Canvas / Gameplay and Flow"); var hud = root.gameObject.AddComponent<GameHud>(); hud.Game = game; hud.Audio = audio;
        hud.HitFlash = Backdrop(root, "Hit feedback", new Color(.9f, .08f, .11f, .14f)); hud.HitFlash.SetActive(false);
        var status = Rect(root, "Status", 16, 15, 306, 75); status.gameObject.AddComponent<Image>().color = Dark;
        hud.HealthText = Label(status, "Health", "ACT " + game.Level + " HP 100 LIVES 3", 11, 4, 290, 22, 14);
        hud.HealthFill = Bar(status, "HP", 11, 29, 210, 7, Color.red);
        hud.EnergyText = Label(status, "Energy", "EN 55", 228, 21, 72, 25, 14);
        hud.StatusText = Label(status, "Progress", "CORES 0/2", 11, 44, 292, 25, 14);
        var core = Rect(root, "Carried core", 16, 90, 306, 33); core.gameObject.AddComponent<Image>().color = Dark; hud.CorePanel = core.gameObject;
        hud.CoreText = Label(core, "Integrity", "CORE", 11, 0, 288, 22, 14); hud.CoreFill = Bar(core, "Core HP", 11, 25, 282, 6, Orange);
        var objective = Rect(root, "Objective", 0, 0, 420, 36); objective.anchorMin = objective.anchorMax = new Vector2(.5f, 1); objective.pivot = new Vector2(.5f, 1); objective.anchoredPosition = new Vector2(0, -16); objective.gameObject.AddComponent<Image>().color = Dark;
        hud.ObjectiveText = Label(objective, "Objective", "DELIVER TWO CORES", 12, 7, 396, 23, 14);
        var upload = Rect(objective, "Terminal upload", 0, 40, 420, 47); upload.gameObject.AddComponent<Image>().color = Dark; hud.UploadPanel = upload.gameObject;
        hud.UploadText = Label(upload, "Upload status", "UPLOAD", 10, 2, 400, 20, 14);
        hud.UploadFill = Bar(upload, "Upload progress", 10, 25, 398, 5, Color.red); hud.StationFill = Bar(upload, "Station HP", 10, 35, 398, 5, Orange);
        var hint = Rect(objective, "Hint", 0, 90, 420, 47); hint.gameObject.AddComponent<Image>().color = Dark; hud.HintPanel = hint.gameObject;
        hud.HintText = Label(hint, "Hint", "", 10, 5, 400, 38, 14);
        var boss = Rect(root, "Guardian health", 0, 0, 220, 48); boss.anchorMin = boss.anchorMax = new Vector2(1, 1); boss.pivot = new Vector2(1, 1); boss.anchoredPosition = new Vector2(-16, -15); boss.gameObject.AddComponent<Image>().color = Dark; hud.BossPanel = boss.gameObject;
        hud.BossText = Label(boss, "Health", "GUARDIAN 16/16", 10, 3, 205, 23, 14); hud.BossFill = Bar(boss, "Boss HP", 10, 28, 198, 7, Color.red);
        var controls = Rect(root, "Bottom controls", 18, 0, 1244, 37); controls.anchorMin = controls.anchorMax = new Vector2(0, 0); controls.pivot = new Vector2(0, 0); controls.anchoredPosition = new Vector2(18, 16); controls.gameObject.AddComponent<Image>().color = Dark;
        var bottomText = Label(controls, "Controls", "A/D MOVE   W TAP/HOLD/DOUBLE   S CROUCH (AIR: FAST DROP)   R CLIMB / DOWN ARROW DESCEND   SPACE DASH   ESC PAUSE   F DRONE   SHIFT SPECIAL   LEFT FIRE   RIGHT ARC (II+)   Q SHIELD (II+)   E WAVE (III+)", 9, 4, 1226, 30, 13);
        bottomText.resizeTextForBestFit = true; bottomText.resizeTextMinSize = 10; bottomText.resizeTextMaxSize = 13;
        var pause = CenterPanel(root, "Pause or Game Over", 620, 540); hud.PausePanel = pause.gameObject;
        hud.PauseTitle = Label(pause, "Title", "PAUSED", 30, 20, 560, 55, 31);
        hud.PauseText = Label(pause, "Description", GameHud.ControlsText, 30, 80, 560, 78, 14);
        hud.ResumeButton = Button(pause, "Resume", "RESUME", 30, 160, 560, 47, hud.ResumeGame);
        Button(pause, "Restart", "RESTART RUN", 30, 216, 560, 47, hud.RestartRun);
        Button(pause, "Main Menu", "MAIN MENU", 30, 272, 560, 47, hud.ReturnToMenu);
        Button(pause, "Exit", "EXIT GAME", 30, 328, 560, 47, hud.QuitGame);
        var pauseAudio = Rect(pause, "Audio Settings", 30, 390, 560, 115); hud.PauseSettings = pauseAudio.gameObject;
        hud.MasterLabel = Label(pauseAudio, "Master", "MASTER", 0, 0, 190, 24, 14); hud.MasterSlider = Slider(pauseAudio, "Master slider", 200, 3, 340, hud.SetMaster);
        hud.SfxLabel = Label(pauseAudio, "SFX", "SFX", 0, 36, 190, 24, 14); hud.SfxSlider = Slider(pauseAudio, "SFX slider", 200, 39, 340, hud.SetSfx);
        hud.UiLabel = Label(pauseAudio, "UI", "UI", 0, 72, 190, 24, 14); hud.UiSlider = Slider(pauseAudio, "UI slider", 200, 75, 340, hud.SetUi);
        var win = CenterPanel(root, "Victory and Leaderboard", 1000, 588); hud.WinPanel = win.gameObject;
        Label(win, "Title", "THE SIGNAL SURVIVES", 34, 22, 930, 55, 31);
        Label(win, "Subtitle", "The loop is broken. The city has a future.", 36, 80, 920, 34);
        hud.ResultText = Label(win, "Result breakdown", "FINAL SCORE", 38, 132, 475, 275, 18);
        Label(win, "Leaderboard title", "LOCAL TOP 10", 535, 131, 425, 35);
        hud.LeaderboardCells = Table(win, 535, 169, 425);
        var save = Rect(win, "Save score", 38, 412, 475, 75); hud.SaveControls = save.gameObject;
        Label(save, "Name prompt", "Enter your name to save your best run (max 16):", 0, 0, 470, 27, 14);
        var entry = Rect(save, "Player name", 0, 31, 260, 39); entry.gameObject.AddComponent<Image>().color = new Color(.14f, .2f, .3f);
        hud.PlayerName = entry.gameObject.AddComponent<InputField>(); hud.PlayerName.characterLimit = 16;
        hud.PlayerName.targetGraphic = entry.GetComponent<Image>();
        entry.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        var entryText = Label(entry, "Text", "", 8, 4, 244, 31, 18); hud.PlayerName.textComponent = entryText; entryText.horizontalOverflow = HorizontalWrapMode.Overflow;
        hud.SaveButton = Button(save, "Save", "SAVE SCORE", 272, 29, 200, 43, hud.SaveScore);
        hud.ResultMessage = Label(win, "Save result", "", 38, 489, 940, 26, 14);
        Button(win, "Restart", "RESTART RUN", 38, 529, 291, 45, hud.RestartRun);
        Button(win, "Main Menu", "MAIN MENU", 354, 529, 291, 45, hud.ReturnToMenu);
        Button(win, "Exit", "EXIT GAME", 669, 529, 291, 45, hud.QuitGame);
        var transition = CenterPanel(root, "Act Transition", 760, 290); hud.TransitionPanel = transition.gameObject;
        hud.TransitionText = Label(transition, "Transition", "DISTRICT CLEARED", 50, 40, 660, 210, 24);
        var power = CenterPanel(root, "Choose One-Time Power", 780, 334); hud.PowerPanel = power.gameObject;
        Label(power, "Title", "CHOOSE A ONE-TIME POWER", 27, 16, 730, 49, 31);
        Label(power, "Description", "8 coins earned. Pick ONE. Press SHIFT to use it when you need it.", 27, 68, 727, 38);
        Button(power, "EMP", "EMP BLAST / Clear nearby mines, barriers and enemies", 29, 109, 720, 58, hud.ChooseEmp);
        Button(power, "Aegis", "AEGIS SHIELD / Ignore damage for 5 seconds", 29, 179, 720, 58, hud.ChooseAegis);
        Button(power, "Repair", "FIELD REPAIR / Restore 45 HP", 29, 249, 720, 58, hud.ChooseRepair);
        var story = CenterPanel(root, "Ink Dialogue", 680, 390); hud.StoryPanel = story.gameObject;
        hud.StoryTitle = Label(story, "Title", "INTERCEPTED TRANSMISSION", 30, 20, 620, 46, 29);
        hud.StoryText = Label(story, "Text", "", 30, 79, 620, 105);
        hud.ContinueButton = Button(story, "Continue", "CONTINUE [ENTER]", 32, 315, 616, 52, hud.ContinueDialogue);
        hud.ChoiceButtons = new Button[3]; hud.ChoiceTexts = new Text[3];
        UnityAction[] choose = { hud.ChooseDialogue0, hud.ChooseDialogue1, hud.ChooseDialogue2 };
        for (int i = 0; i < 3; i++)
        {
            hud.ChoiceButtons[i] = Button(story, "Choice " + (i + 1), "CHOICE", 32, 162 + i * 67, 616, 58, choose[i]);
            hud.ChoiceTexts[i] = hud.ChoiceButtons[i].GetComponentInChildren<Text>();
        }
        var error = CenterPanel(root, "Startup Error", 860, 270); hud.ErrorPanel = error.gameObject;
        Label(error, "Title", "LEVEL COULD NOT LOAD", 40, 30, 780, 50, 31);
        hud.ErrorText = Label(error, "Error", "", 40, 93, 780, 100);
        Button(error, "Main Menu", "MAIN MENU", 235, 210, 390, 45, hud.ReturnToMenu);
        foreach (var panel in new[] { core.gameObject, upload.gameObject, hint.gameObject, boss.gameObject,
            pause.gameObject, win.gameObject, transition.gameObject, power.gameObject, story.gameObject, error.gameObject }) panel.SetActive(false);
        return hud;
    }
}
