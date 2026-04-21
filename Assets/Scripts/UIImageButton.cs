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

    public void OnPointerClick(PointerEventData eventData)
    {
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
}