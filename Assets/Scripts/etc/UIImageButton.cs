using UnityEngine;
using UnityEngine.EventSystems;

public class UIImageButton : MonoBehaviour, IPointerClickHandler
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

    private void Awake()
    {
        EnsureSfxSource();
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
}
