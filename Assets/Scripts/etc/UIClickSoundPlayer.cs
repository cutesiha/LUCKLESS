using UnityEngine;

public static class UIClickSoundPlayer
{
    private const int SampleRate = 44100;
    private static AudioClip fallbackClickClip;

    public static void Play(GameObject sourceObject, ref AudioSource audioSource, AudioClip clickClip)
    {
        if (sourceObject == null)
        {
            return;
        }

        audioSource = EnsureSource(sourceObject, audioSource);
        AudioClip clipToPlay = clickClip != null ? clickClip : GetFallbackClickClip();

        if (audioSource == null || clipToPlay == null)
        {
            return;
        }

        audioSource.PlayOneShot(clipToPlay, GameAudioSettings.SfxVolume);
    }

    private static AudioSource EnsureSource(GameObject sourceObject, AudioSource audioSource)
    {
        if (audioSource == null)
        {
            audioSource = sourceObject.GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = sourceObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        return audioSource;
    }

    private static AudioClip GetFallbackClickClip()
    {
        if (fallbackClickClip != null)
        {
            return fallbackClickClip;
        }

        int sampleCount = Mathf.RoundToInt(SampleRate * 0.055f);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)SampleRate;
            float progress = i / (float)Mathf.Max(1, sampleCount - 1);
            float frequency = Mathf.Lerp(1900f, 820f, progress);
            float envelope = Mathf.Exp(-time * 72f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * 0.34f;
        }

        fallbackClickClip = AudioClip.Create("GeneratedUIClick", sampleCount, 1, SampleRate, false);
        fallbackClickClip.SetData(samples, 0);
        return fallbackClickClip;
    }
}
