using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public static class GameAudioSettings
{
    public const string MasterKey = "Option_Master";
    public const string BgmKey = "Option_BGM";
    public const string VoiceKey = "Option_Voice";
    public const string SfxKey = "Option_SFX";

    private const float DefaultMaster = 0.8f;
    private const float DefaultBgm = 0.7f;
    private const float DefaultVoice = 1f;
    private const float DefaultSfx = 0.75f;

    private static readonly Dictionary<int, float> BgmBaseVolumes = new Dictionary<int, float>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeAfterSceneLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyCurrentSettings();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyCurrentSettings();
    }

    public static float MasterVolume
    {
        get { return GetPercent(MasterKey, DefaultMaster); }
    }

    public static float BgmVolume
    {
        get { return GetPercent(BgmKey, DefaultBgm); }
    }

    public static float VoiceVolume
    {
        get { return GetPercent(VoiceKey, DefaultVoice); }
    }

    public static float SfxVolume
    {
        get { return GetPercent(SfxKey, DefaultSfx); }
    }

    public static float GetBgmSourceVolume(float baseVolume)
    {
        return baseVolume * BgmVolume;
    }

    public static float GetVoiceSourceVolume(float baseVolume)
    {
        return baseVolume * VoiceVolume;
    }

    public static void Save(int master, int bgm, int voice, int sfx)
    {
        PlayerPrefs.SetInt(MasterKey, Mathf.Clamp(master, 0, 100));
        PlayerPrefs.SetInt(BgmKey, Mathf.Clamp(bgm, 0, 100));
        PlayerPrefs.SetInt(VoiceKey, Mathf.Clamp(voice, 0, 100));
        PlayerPrefs.SetInt(SfxKey, Mathf.Clamp(sfx, 0, 100));
        PlayerPrefs.Save();
    }

    public static void ApplyCurrentSettings(AudioSource[] explicitBgmSources = null, AudioMixer audioMixer = null)
    {
        AudioListener.volume = MasterVolume;
        ApplyBgmSources(explicitBgmSources);

        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", LinearToDecibel(MasterVolume));
            audioMixer.SetFloat("VoiceVolume", LinearToDecibel(VoiceVolume));
            audioMixer.SetFloat("BGMVolume", LinearToDecibel(BgmVolume));
        }
    }

    public static void ApplyBgmSource(AudioSource source, float baseVolume)
    {
        if (source == null)
        {
            return;
        }

        BgmBaseVolumes[source.GetInstanceID()] = baseVolume;
        source.volume = GetBgmSourceVolume(baseVolume);
    }

    private static void ApplyBgmSources(AudioSource[] explicitBgmSources)
    {
        bool appliedExplicit = false;

        if (explicitBgmSources != null)
        {
            for (int i = 0; i < explicitBgmSources.Length; i++)
            {
                AudioSource source = explicitBgmSources[i];
                if (source == null)
                {
                    continue;
                }

                ApplyStoredBgmSource(source);
                appliedExplicit = true;
            }
        }

        if (appliedExplicit)
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER
        AudioSource[] sources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        AudioSource[] sources = Object.FindObjectsOfType<AudioSource>(true);
#endif

        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source != null && ShouldTreatAsBgm(source))
            {
                ApplyStoredBgmSource(source);
            }
        }
    }

    private static void ApplyStoredBgmSource(AudioSource source)
    {
        int id = source.GetInstanceID();
        if (!BgmBaseVolumes.TryGetValue(id, out float baseVolume))
        {
            baseVolume = source.volume;
            BgmBaseVolumes[id] = baseVolume;
        }

        source.volume = GetBgmSourceVolume(baseVolume);
    }

    private static bool ShouldTreatAsBgm(AudioSource source)
    {
        if (source.clip == null)
        {
            return false;
        }

        return !IsDialogueVoiceSource(source);
    }

    private static bool IsDialogueVoiceSource(AudioSource source)
    {
#if UNITY_2023_1_OR_NEWER
        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        MonoBehaviour[] behaviours = Object.FindObjectsOfType<MonoBehaviour>(true);
#endif

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
            {
                continue;
            }

            FieldInfo[] fields = behaviour.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int j = 0; j < fields.Length; j++)
            {
                FieldInfo field = fields[j];
                if (field.FieldType == typeof(AudioSource) &&
                    field.Name.IndexOf("voice", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                    ReferenceEquals(field.GetValue(behaviour), source))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static float GetPercent(string key, float defaultValue)
    {
        int defaultInt = Mathf.RoundToInt(defaultValue * 100f);
        return Mathf.Clamp(PlayerPrefs.GetInt(key, defaultInt), 0, 100) / 100f;
    }

    private static float LinearToDecibel(float value)
    {
        if (value <= 0.0001f)
        {
            return -80f;
        }

        return Mathf.Log10(value) * 20f;
    }
}
