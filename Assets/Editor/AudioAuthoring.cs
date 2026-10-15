using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Shatterline;
using AudioConfiguration = Shatterline.AudioConfiguration;

// Unity's public runtime mixer API has no asset constructor. Reflection is confined
// to this Editor utility; the resulting .mixer is a normal saved Unity asset.
public static class AudioAuthoring
{
    private const string MixerPath = "Assets/Audio/Mixers/AM_Shatterline.mixer";
    private const string ConfigPath = "Assets/Audio/CFG_Audio.asset";
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    public static AudioConfiguration EnsureAssets()
    {
        System.IO.Directory.CreateDirectory("Assets/Audio/Mixers");
        AssetDatabase.Refresh();
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null)
        {
            Type controllerType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("UnityEditor.Audio.AudioMixerController")).FirstOrDefault(t => t != null);
            if (controllerType == null)
                foreach (string assembly in new[] { "UnityEditor.AudioModule", "UnityEditor" })
                {
                    try { controllerType = Assembly.Load(assembly).GetType("UnityEditor.Audio.AudioMixerController"); }
                    catch (Exception) { }
                    if (controllerType != null) break;
                }
            if (controllerType == null) throw new InvalidOperationException("Unity AudioMixer editor module is unavailable.");
            var create = controllerType.GetMethod("CreateMixerControllerAtPath", Members);
            if (create == null) throw new InvalidOperationException("AudioMixer asset creation API is unavailable in this Unity version.");
            mixer = (AudioMixer)create.Invoke(null, new object[] { MixerPath });
            object master = controllerType.GetProperty("masterGroup", Members).GetValue(mixer, null);
            object sfx = controllerType.GetMethod("CreateNewGroup", Members).Invoke(mixer, new object[] { "SFX", false });
            object ui = controllerType.GetMethod("CreateNewGroup", Members).Invoke(mixer, new object[] { "UI", false });
            var attach = controllerType.GetMethod("AddChildToParent", Members);
            attach.Invoke(mixer, new[] { sfx, master }); attach.Invoke(mixer, new[] { ui, master });
            var exposed = controllerType.GetProperty("exposedParameters", Members);
            Type parameterType = exposed.PropertyType.GetElementType();
            Array parameters = Array.CreateInstance(parameterType, 3);
            object[] groups = { master, sfx, ui };
            string[] names = { "MasterVolume", "SfxVolume", "UiVolume" };
            for (int i = 0; i < groups.Length; i++)
            {
                object parameter = Activator.CreateInstance(parameterType);
                object guid = groups[i].GetType().GetMethod("GetGUIDForVolume", Members).Invoke(groups[i], null);
                parameterType.GetField("guid", Members).SetValue(parameter, guid);
                parameterType.GetField("name", Members).SetValue(parameter, names[i]);
                parameters.SetValue(parameter, i);
            }
            exposed.SetValue(mixer, parameters, null);
            EditorUtility.SetDirty(mixer); AssetDatabase.SaveAssets();
        }
        var config = AssetDatabase.LoadAssetAtPath<AudioConfiguration>(ConfigPath);
        if (config == null) { config = ScriptableObject.CreateInstance<AudioConfiguration>(); AssetDatabase.CreateAsset(config, ConfigPath); }
        config.Mixer = mixer;
        config.SfxGroup = mixer.FindMatchingGroups("SFX").FirstOrDefault();
        config.UiGroup = mixer.FindMatchingGroups("UI").FirstOrDefault();
        if (config.SfxGroup == null || config.UiGroup == null)
            throw new InvalidOperationException("AudioMixer needs SFX and UI groups.");
        config.Pickup = Load("pickup"); config.Damage = Load("damage"); config.Dash = Load("dash");
        config.Checkpoint = Load("checkpoint"); config.Blaster = Load("blaster");
        EditorUtility.SetDirty(config); AssetDatabase.SaveAssets();
        return config;
    }
    private static AudioClip Load(string name)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/" + name + ".wav");
        if (clip == null) throw new InvalidOperationException("Missing audio clip: " + name);
        return clip;
    }
    public static AudioSystem AddToScene(AudioConfiguration config)
    {
        var root = new GameObject("Audio / Master > SFX + UI");
        var sfx = root.AddComponent<AudioSource>(); var ui = root.AddComponent<AudioSource>();
        var system = root.AddComponent<AudioSystem>(); system.Configure(config, sfx, ui);
        return system;
    }
}
