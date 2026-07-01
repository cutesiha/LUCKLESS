using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UIImageButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum ButtonType
    {
        Start,
        Load,
        Option,
        Exit,
        ExitYes,
        ExitNo,
        OptionClose,
        LoadClose
    }

    public ButtonType buttonType;
    public StartMenu menu;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip clickClip;

    private Graphic hoverGraphic;
    private Color normalColor;
    private readonly Color closeHoverColor = new Color(0.62f, 0.62f, 0.62f, 1f);

    private void Awake()
    {
        EnsureSfxSource();
        EnsureHoverGraphic();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClickSfx();

        switch (buttonType)
        {
            case ButtonType.Start:
                menu.OnClickStart();
                break;

            case ButtonType.Load:
                menu.OnClickLoad();
                break;

            case ButtonType.Option:
                menu.OnClickOption();
                break;

            case ButtonType.Exit:
                menu.OnClickExit();
                break;

            case ButtonType.ExitYes:
                menu.ExitYes();
                break;

            case ButtonType.ExitNo:
                menu.ExitNo();
                break;

            case ButtonType.OptionClose:
                menu.CloseOption();
                break;

            case ButtonType.LoadClose:
                menu.CloseLoad();
                break;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsCloseButton())
        {
            return;
        }

        EnsureHoverGraphic();
        if (hoverGraphic != null)
        {
            hoverGraphic.color = closeHoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsCloseButton())
        {
            return;
        }

        EnsureHoverGraphic();
        if (hoverGraphic != null)
        {
            hoverGraphic.color = normalColor;
        }
    }

    private void PlayClickSfx()
    {
        EnsureSfxSource();

        if (sfxSource == null || clickClip == null)
        {
            return;
        }

        float volume = GameAudioSettings.SfxVolume;
        sfxSource.PlayOneShot(clickClip, volume);
    }

    private void EnsureSfxSource()
    {
        if (sfxSource != null)
        {
            return;
        }

        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    private bool IsCloseButton()
    {
        return buttonType == ButtonType.OptionClose
            || buttonType == ButtonType.LoadClose
            || buttonType == ButtonType.ExitNo;
    }

    private void EnsureHoverGraphic()
    {
        if (hoverGraphic != null)
        {
            return;
        }

        hoverGraphic = GetComponent<TextMeshProUGUI>();
        if (hoverGraphic == null)
        {
            hoverGraphic = GetComponent<Image>();
        }
        if (hoverGraphic == null)
        {
            hoverGraphic = GetComponentInChildren<TextMeshProUGUI>(true);
        }
        if (hoverGraphic == null)
        {
            hoverGraphic = GetComponentInChildren<Image>(true);
        }

        if (hoverGraphic != null)
        {
            normalColor = hoverGraphic.color;

            TextMeshProUGUI text = hoverGraphic as TextMeshProUGUI;
            if (text != null && IsCloseButton())
            {
                text.fontSize = Mathf.Max(text.fontSize, 46f);
                RectTransform rect = text.rectTransform;
                rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 78f), Mathf.Max(rect.sizeDelta.y, 72f));
            }
        }
    }
}
