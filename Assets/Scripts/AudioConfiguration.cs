using UnityEngine;
using UnityEngine.Audio;

namespace Shatterline
{
    [CreateAssetMenu(menuName = "SHATTERLINE/Audio Configuration")]
    public sealed class AudioConfiguration : ScriptableObject
    {
        public AudioMixer Mixer;
        public AudioMixerGroup SfxGroup;
        public AudioMixerGroup UiGroup;
        public AudioClip Pickup, Damage, Dash, Checkpoint, Blaster;
        public AudioClip Clip(string id)
        {
            switch (id)
            {
                case "pickup": return Pickup;
                case "damage": return Damage;
                case "dash": return Dash;
                case "checkpoint": return Checkpoint;
                case "blaster": return Blaster;
                default: Debug.LogWarning("Unknown audio cue: " + id); return null;
            }
        }
    }
}
