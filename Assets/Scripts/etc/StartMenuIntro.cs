using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StartMenuIntro : MonoBehaviour
{
    [Header("Intro")]
    [SerializeField] private RectTransform title;
    [SerializeField] private CanvasGroup clickPrompt;
    [SerializeField] private float titleRiseDuration = 1.65f;
    [SerializeField] private float titleStartOffset = 1350f;
    [SerializeField] private float clickBlinkSpeed = 1.55f;

    [Header("Buttons")]
    [SerializeField] private RectTransform[] menuButtons;
    [SerializeField] private float buttonSlideDuration = 0.82f;
    [SerializeField] private float buttonStagger = 0.18f;
    [SerializeField] private float buttonStartOffset = 2200f;

    private Vector2 titleTarget;
    private Vector2[] buttonTargets;
    private bool canOpenButtons;
    private bool buttonsOpening;
    private Coroutine introRoutine;

    private void Awake()
    {
        if (title != null)
        {
            titleTarget = title.anchoredPosition;
        }

        if (clickPrompt != null)
        {
            clickPrompt.interactable = false;
            clickPrompt.blocksRaycasts = false;
        }

        buttonTargets = new Vector2[menuButtons.Length];

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null)
            {
                continue;
            }

            buttonTargets[i] = menuButtons[i].anchoredPosition;
        }
    }

    private void OnEnable()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
        }

        ResetIntroState();
        introRoutine = StartCoroutine(PlayIntro());
    }

    private void ResetIntroState()
    {
        canOpenButtons = false;
        buttonsOpening = false;

        if (title != null)
        {
            title.anchoredPosition = titleTarget + Vector2.down * titleStartOffset;
            title.gameObject.SetActive(true);
        }

        if (clickPrompt != null)
        {
            clickPrompt.alpha = 0f;
            clickPrompt.gameObject.SetActive(false);
        }

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null)
            {
                continue;
            }

            menuButtons[i].anchoredPosition = buttonTargets[i] + Vector2.right * buttonStartOffset;
            menuButtons[i].gameObject.SetActive(false);
        }
    }

    private IEnumerator PlayIntro()
    {
        if (title != null)
        {
            yield return Move(title, title.anchoredPosition, titleTarget, titleRiseDuration);
        }

        if (clickPrompt != null)
        {
            clickPrompt.gameObject.SetActive(true);
        }

        canOpenButtons = true;
    }

    private void Update()
    {
        if (clickPrompt != null && clickPrompt.gameObject.activeSelf && canOpenButtons && !buttonsOpening)
        {
            clickPrompt.alpha = 0.35f + Mathf.PingPong(Time.unscaledTime * clickBlinkSpeed, 0.65f);
        }

        if (!canOpenButtons || buttonsOpening)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            StartCoroutine(OpenButtons());
        }
    }

    private IEnumerator OpenButtons()
    {
        buttonsOpening = true;
        canOpenButtons = false;

        if (clickPrompt != null)
        {
            clickPrompt.gameObject.SetActive(false);
        }

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null)
            {
                continue;
            }

            menuButtons[i].gameObject.SetActive(true);
            StartCoroutine(Move(menuButtons[i], menuButtons[i].anchoredPosition, buttonTargets[i], buttonSlideDuration));
            yield return new WaitForSecondsRealtime(buttonStagger);
        }
    }

    private IEnumerator Move(RectTransform target, Vector2 from, Vector2 to, float duration)
    {
        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            target.anchoredPosition = Vector2.LerpUnclamped(from, to, t);

            if (elapsed >= duration)
            {
                break;
            }

            yield return null;
        }

        target.anchoredPosition = to;
    }
}
