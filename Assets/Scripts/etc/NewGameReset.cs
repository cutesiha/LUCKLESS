using UnityEngine;

public static class NewGameReset
{
    public static void ResetProgressForNewGame()
    {
        int master = PlayerPrefs.GetInt(GameAudioSettings.MasterKey, 80);
        int bgm = PlayerPrefs.GetInt(GameAudioSettings.BgmKey, 100);
        int voice = PlayerPrefs.GetInt(GameAudioSettings.VoiceKey, 100);
        int sfx = PlayerPrefs.GetInt(GameAudioSettings.SfxKey, 75);
        float legacyBgm = PlayerPrefs.GetFloat("BGM", 1f);
        float legacySfx = PlayerPrefs.GetFloat("SFX", 1f);

        PlayerPrefs.DeleteAll();

        PlayerPrefs.SetInt(GameAudioSettings.MasterKey, master);
        PlayerPrefs.SetInt(GameAudioSettings.BgmKey, bgm);
        PlayerPrefs.SetInt(GameAudioSettings.VoiceKey, voice);
        PlayerPrefs.SetInt(GameAudioSettings.SfxKey, sfx);
        PlayerPrefs.SetFloat("BGM", legacyBgm);
        PlayerPrefs.SetFloat("SFX", legacySfx);
        PlayerPrefs.Save();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetRun();
            GameManager.Instance.selectedCharacter = "zero";
        }
    }
}
