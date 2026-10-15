using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shatterline
{
    public static class SceneFlow
    {
        public const string MainMenu = "MainMenu";
        public static readonly string[] Acts = { "RiftOne", "RiftTwo", "RiftCore" };
        public static void RestoreClock()
        { Time.timeScale = 1f; AudioListener.pause = false; Cursor.visible = true; }
        public static void StartRun()
        { RestoreClock(); RunState.NewRun(); SceneManager.LoadScene(Acts[0]); }
        public static void ReturnToMenu()
        { RestoreClock(); SceneManager.LoadScene(MainMenu); }
        public static void NextAct(int act)
        {
            if (act < 1 || act >= Acts.Length) return;
            RestoreClock(); SceneManager.LoadScene(Acts[act]);
        }
        public static void Quit()
        {
            PlayerPrefs.Save(); RestoreClock();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
