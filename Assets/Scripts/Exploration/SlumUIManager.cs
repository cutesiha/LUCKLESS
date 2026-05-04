// SlumUIManager.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System;

public class SlumUIManager : MonoBehaviour
{
    public static SlumUIManager Instance;

    [Header("하단 HUD")]
    public TextMeshProUGUI luxText;
    public TextMeshProUGUI locationText;   // "빈민가 17구역"

    [Header("상호작용 프롬프트")]
    public GameObject promptPanel;
    public TextMeshProUGUI promptText;

    [Header("선택지")]
    public GameObject choicePanel;
    public TextMeshProUGUI choiceTitle;
    public Button[] choiceButtons;        // 버튼 3개

    [Header("페이드")]
    public Image fadeImage;

    void Awake()
    {
        Instance = this;
        locationText.text = "빈민가 17구역";
        choicePanel.SetActive(false);
        promptPanel.SetActive(false);
    }

    void Update()
    {
        // GameManager가 null이면 에러 — 안전하게 처리
        if (GameManager.Instance != null)
            luxText.text = $"LUX  {GameManager.Instance.currentLux}";
    }

    public void ShowPrompt(string text)
    {
        promptPanel.SetActive(true);
        promptText.text = text;
    }

    public void HidePrompt() => promptPanel.SetActive(false);

    public void ShowChoicePanel(string title, string[] options, Action<int> onSelect)
    {
        choicePanel.SetActive(true);
        choiceTitle.text = title;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int idx = i;
            choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = options[i];
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() =>
            {
                choicePanel.SetActive(false);
                onSelect(idx);
            });
        }
    }

    public void FadeOut(Action onComplete)
    {
        StartCoroutine(FadeRoutine(onComplete));
    }

    IEnumerator FadeRoutine(Action onComplete)
    {
        float t = 0f;
        while (t < 0.8f)
        {
            t += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, t / 0.8f);
            yield return null;
        }
        onComplete?.Invoke();
    }
}