using UnityEngine;
using TMPro;
using System.Collections;

[System.Serializable]
public class DialogueLine
{
    public string speaker;

    [TextArea(2, 5)]
    public string text;
}

public class Typewriter : MonoBehaviour
{
    public GameObject prologueCanvas;
    public GameObject platformCanvas;

    public TextMeshProUGUI nameText;      // 이름 텍스트
    public TextMeshProUGUI dialogueText;  // 대사 텍스트

    public float typingSpeed = 0.05f;
    public DialogueLine[] lines;

    int index = 0;
    bool isTyping = false;
    Coroutine typingCoroutine;

    void Start()
    {
        StartTyping();
    }

    void StartTyping()
    {
        typingCoroutine = StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        isTyping = true;

        nameText.text = lines[index].speaker;
        dialogueText.text = "";

        foreach (char c in lines[index].text)
        {
            dialogueText.text += c;

            if (c == '\n')
            {
                yield return new WaitForSeconds(0.4f);
            }
            else
            {
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        isTyping = false;
    }

    public void OnNextInput()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = lines[index].text;
            isTyping = false;
        }
        else
        {
            if (index == 5)
            {
                prologueCanvas.SetActive(false);
                platformCanvas.SetActive(true);
                return;
            }

            if (index < lines.Length - 1)
            {
                index++;
                StartTyping();
            }
            else
            {
                Debug.Log("프롤로그 끝");
            }
        }
    }
}