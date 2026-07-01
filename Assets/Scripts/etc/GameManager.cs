// GameManager.cs
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // ── LUX / 불운 ──────────────────────────────
    public int currentLux = 0;
    // 제로는 LUX가 없는 캐릭터 — 시작값 0이 맞음

    // ── 씬간 선택 저장 ───────────────────────────
    // 이다 펜 처리 선택 (0=완전제거 1=방치 2=도주허용 -1=미선택)
    public int slum17Choice = -1;

    // 다음에 로드할 씬 이름 (ExitTrigger에서 씀)
    public string nextScene = "BattleScene";

    // ── 캐릭터 선택 ──────────────────────────────
    public string selectedCharacter = "zero";

    void Awake()
    {
        // 씬 바뀌어도 유지
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ── LUX 조작 ─────────────────────────────────
    public void AddLux(int amount)
    {
        currentLux = Mathf.Max(0, currentLux + amount);
    }

    // ── 런(Run) 리셋 ─────────────────────────────
    // 사망하거나 새 런 시작할 때 호출
    public void ResetRun()
    {
        currentLux = 0;
        slum17Choice = -1;
        nextScene = "BattleScene";
    }
}
