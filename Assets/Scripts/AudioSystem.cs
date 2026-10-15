using UnityEngine;

namespace Shatterline
{
    // One scene-local service; every source is routed through a saved mixer group.
    public sealed class AudioSystem : MonoBehaviour
    {
        public static AudioSystem Current { get; private set; }
        [SerializeField] private AudioConfiguration configuration;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;
        public AudioConfiguration Configuration { get { return configuration; } }
        public float SfxVolume { get; private set; } = 1f;
        public float UiVolume { get; private set; } = 1f;
        public bool IsConfigured { get { return configuration != null && configuration.Mixer != null &&
            sfxSource != null && uiSource != null && sfxSource.outputAudioMixerGroup == configuration.SfxGroup &&
            uiSource.outputAudioMixerGroup == configuration.UiGroup; } }

        public void Configure(AudioConfiguration config, AudioSource sfx, AudioSource ui)
        {
            configuration = config; sfxSource = sfx; uiSource = ui;
            sfx.outputAudioMixerGroup = config.SfxGroup; ui.outputAudioMixerGroup = config.UiGroup;
            sfx.playOnAwake = ui.playOnAwake = false;
            sfx.spatialBlend = ui.spatialBlend = 0f;
            sfx.volume = ui.volume = 1f;
            ui.ignoreListenerPause = true;
        }
        private void Awake()
        {
            Current = this;
            RunState.Volume = PlayerPrefs.GetFloat("Shatterline.Audio.Master", RunState.Volume);
            SfxVolume = PlayerPrefs.GetFloat("Shatterline.Audio.SFX", 1f);
            UiVolume = PlayerPrefs.GetFloat("Shatterline.Audio.UI", 1f);
        }
        private void Start()
        {
            if (!IsConfigured) { Debug.LogError("AudioSystem has missing Inspector mixer/source bindings."); return; }
            AudioListener.volume = 1f;
            SetMaster(PlayerPrefs.GetFloat("Shatterline.Audio.Master", RunState.Volume));
            SetSfx(PlayerPrefs.GetFloat("Shatterline.Audio.SFX", 1f));
            SetUi(PlayerPrefs.GetFloat("Shatterline.Audio.UI", 1f));
        }
        private static float Decibels(float value)
        { return value <= .0001f ? -80f : Mathf.Log10(value) * 20f; }
        private void SetParameter(string name, float value)
        {
            if (configuration != null && configuration.Mixer != null &&
                !configuration.Mixer.SetFloat(name, Decibels(value)))
                Debug.LogError("AudioMixer exposed parameter is missing: " + name);
        }
        public void SetMaster(float value)
        {
            RunState.Volume = Mathf.Clamp01(value);
            SetParameter("MasterVolume", RunState.Volume);
            PlayerPrefs.SetFloat("Shatterline.Audio.Master", RunState.Volume);
        }
        public void SetSfx(float value)
        {
            SfxVolume = Mathf.Clamp01(value); SetParameter("SfxVolume", SfxVolume);
            PlayerPrefs.SetFloat("Shatterline.Audio.SFX", SfxVolume);
        }
        public void SetUi(float value)
        {
            UiVolume = Mathf.Clamp01(value); SetParameter("UiVolume", UiVolume);
            PlayerPrefs.SetFloat("Shatterline.Audio.UI", UiVolume);
        }
        public void Play(string id)
        {
            if (!IsConfigured) return;
            AudioClip clip = configuration.Clip(id);
            if (clip != null) sfxSource.PlayOneShot(clip);
        }
        public void Click()
        {
            if (IsConfigured && configuration.Pickup != null) uiSource.PlayOneShot(configuration.Pickup, .35f);
        }
        private void OnDestroy()
        {
            if (Current == this) Current = null;
            PlayerPrefs.Save();
        }
    }
}
