using UnityEngine;

public static class BattleScoreStore
{
    public const int MaxScore = 500;
    public const string DefaultMissionId = "Mission1";
    public const string KarimHasanMissionId = "Mission2";
    public const string ActiveBattleMissionKey = "ActiveBattleMission";
    public const string PendingPostVictoryDialogueKey = "PendingPostVictoryDialogue";
    public const string IdapenFirstVictoryDialogueShownKey = "IdapenFirstVictoryDialogueShown";
    public const string IdapenFirstVictoryJustWonKey = "IdapenFirstVictoryJustWon";
    public const string KarimFirstVictoryDialogueShownKey = "KarimFirstVictoryDialogueShown";
    public const string KarimFirstVictoryJustWonKey = "KarimFirstVictoryJustWon";

    private const string CurrentPrefix = "BattleScore.Current.";
    private const string BestPrefix = "BattleScore.Best.";

    public static int CalculateScore(int turnCount)
    {
        int usedTurns = Mathf.Max(1, turnCount);
        return Mathf.Clamp(MaxScore - (usedTurns - 1) * 25, 0, MaxScore);
    }

    public static void SaveScore(string missionId, int score)
    {
        if (string.IsNullOrWhiteSpace(missionId))
        {
            missionId = DefaultMissionId;
        }

        score = Mathf.Clamp(score, 0, MaxScore);
        PlayerPrefs.SetInt(CurrentPrefix + missionId, score);

        int bestScore = GetBestScore(missionId);
        if (score > bestScore)
        {
            PlayerPrefs.SetInt(BestPrefix + missionId, score);
        }

        PlayerPrefs.Save();
    }

    public static int GetCurrentScore(string missionId)
    {
        return PlayerPrefs.GetInt(CurrentPrefix + missionId, 0);
    }

    public static int GetBestScore(string missionId)
    {
        return PlayerPrefs.GetInt(BestPrefix + missionId, 0);
    }
}
