// DialogueManager.cs
using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI 연결")]
    public GameObject dialoguePanel;       // 대화창 전체
    public TextMeshProUGUI speakerText;    // 화자 이름
    public TextMeshProUGUI bodyText;       // 대사 내용
    public GameObject continuePrompt;      // "Z키 계속" 표시

    private bool isTyping = false;
    private bool skipRequested = false;

    void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false);
    }

    // 대화 시작: string 배열로 대사를 넘김
    public IEnumerator PlayDialogue(string speaker, string[] lines, System.Action onFinish = null)
    {
        dialoguePanel.SetActive(true);
        continuePrompt.SetActive(false);
        speakerText.text = speaker;

        foreach (string line in lines)
        {
            yield return StartCoroutine(TypeLine(line));

            // 다음 입력 대기
            continuePrompt.SetActive(true);
            yield return new WaitUntil(() =>
                Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return)
            );
            continuePrompt.SetActive(false);
            yield return null;  // 한 프레임 대기 (입력 중복 방지)
        }

        dialoguePanel.SetActive(false);
        onFinish?.Invoke();
    }

    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        skipRequested = false;
        bodyText.text = "";

        foreach (char c in line)
        {
            if (skipRequested)
            {
                bodyText.text = line;
                break;
            }
            bodyText.text += c;
            yield return new WaitForSeconds(0.03f);
        }

        isTyping = false;
    }

    void Update()
    {
        if (isTyping && (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return)))
            skipRequested = true;
    }
}