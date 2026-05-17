using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HandbookPanelController : MonoBehaviour
{
    private static readonly Color HandbookHoverTint = new Color(0.72f, 0.12f, 0.42f, 1f);
    private const float HandbookHoverTintAmount = 0.28f;
    private const float HandbookHoverDarkenAmount = 0.12f;

    private static readonly string[] InventoryPanelNames =
    {
        "InventoryPanel1",
        "InventroyPanel1",
        "InventoryPanel",
        "InventroyPanel"
    };

    [SerializeField] private Image handbookClickImage;
    [SerializeField] private GameObject inventoryPanel;

    [Header("SFX")]
    [SerializeField] private AudioSource clickSfxSource;
    [SerializeField] private AudioClip clickClip;

    private int lastClickSfxFrame = -1;

    private void OnEnable()
    {
        SetupHandbookInteractions();
    }

    private void Start()
    {
        SetupHandbookInteractions();
    }

    public void SetInventoryPanel(GameObject panel)
    {
        inventoryPanel = panel;
    }

    public void OpenInventoryPanel()
    {
        GameObject targetPanel = GetInventoryPanel();

        if (targetPanel == null)
        {
            return;
        }

        InventoryPanelController inventoryController = targetPanel.GetComponent<InventoryPanelController>();

        if (inventoryController != null)
        {
            inventoryController.ShowInventoryPanel();
            return;
        }

        targetPanel.SetActive(true);
        targetPanel.transform.SetAsLastSibling();
    }

    private GameObject GetInventoryPanel()
    {
        if (inventoryPanel != null)
        {
            return inventoryPanel;
        }

        for (int i = 0; i < InventoryPanelNames.Length; i++)
        {
            inventoryPanel = GameObject.Find(InventoryPanelNames[i]);

            if (inventoryPanel != null)
            {
                return inventoryPanel;
            }
        }

        for (int i = 0; i < InventoryPanelNames.Length; i++)
        {
            inventoryPanel = FindSceneObjectByName(InventoryPanelNames[i]);

            if (inventoryPanel != null)
            {
                return inventoryPanel;
            }
        }

        return inventoryPanel;
    }

    private GameObject FindSceneObjectByName(string objectName)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i].name == objectName && objects[i].scene.IsValid())
            {
                return objects[i];
            }
        }

        return null;
    }

    private void SetupHandbookInteractions()
    {
        EnsureHandbookCanvasOnTop();

        Image targetImage = GetHandbookClickImage();

        if (targetImage == null)
        {
            return;
        }

        SetupPinkHover(targetImage, GetDarkPinkHoverColor);
        AddClickEvent(targetImage, OpenInventoryPanel);
        EnsureBattleInventoryOpener(targetImage);
        EnsureClickSfx(targetImage);
    }

    private void EnsureHandbookCanvasOnTop()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = 2000;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        transform.SetAsLastSibling();
    }

    private void EnsureBattleInventoryOpener(Image targetImage)
    {
        BattleInventoryOpener[] openers = targetImage.GetComponents<BattleInventoryOpener>();
        BattleInventoryOpener opener = openers.Length > 0 ? openers[0] : null;
        if (opener == null)
        {
            opener = targetImage.gameObject.AddComponent<BattleInventoryOpener>();
        }

        for (int i = 1; i < openers.Length; i++)
        {
            Destroy(openers[i]);
        }

        opener.SetHoverStyle(HandbookHoverTint, HandbookHoverTintAmount, HandbookHoverDarkenAmount);
        opener.SetInventoryPanel(GetInventoryPanel());
    }

    private Image GetHandbookClickImage()
    {
        if (handbookClickImage != null)
        {
            return handbookClickImage;
        }

        Transform zeroPortrait = transform.Find("ZeroPortrait");

        if (zeroPortrait != null && zeroPortrait.TryGetComponent(out Image zeroImage))
        {
            handbookClickImage = zeroImage;
            return handbookClickImage;
        }

        Image[] images = GetComponentsInChildren<Image>(true);
        Image largestImage = null;
        float largestArea = 0f;

        for (int i = 0; i < images.Length; i++)
        {
            RectTransform rectTransform = images[i].rectTransform;
            float area = rectTransform.rect.width * rectTransform.rect.height;

            if (area > largestArea)
            {
                largestArea = area;
                largestImage = images[i];
            }
        }

        handbookClickImage = largestImage;
        return handbookClickImage;
    }

    private void SetupPinkHover(Image image, System.Func<Color, Color> getHoverColor)
    {
        if (image == null)
        {
            return;
        }

        image.raycastTarget = true;
        EventTrigger trigger = image.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger = image.gameObject.AddComponent<EventTrigger>();
        }

        Color originalColor = image.color;
        trigger.triggers.RemoveAll(entry =>
            entry.eventID == EventTriggerType.PointerEnter || entry.eventID == EventTriggerType.PointerExit);
        AddPointerEvent(trigger, EventTriggerType.PointerEnter, () => image.color = getHoverColor(originalColor));
        AddPointerEvent(trigger, EventTriggerType.PointerExit, () => image.color = originalColor);
    }

    private void AddClickEvent(Image image, UnityEngine.Events.UnityAction callback)
    {
        image.raycastTarget = true;
        EventTrigger trigger = image.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger = image.gameObject.AddComponent<EventTrigger>();
        }

        trigger.triggers.RemoveAll(entry => entry.eventID == EventTriggerType.PointerClick);
        AddPointerEvent(trigger, EventTriggerType.PointerClick, callback);
    }

    private void AddPointerEvent(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(_ => callback());
        trigger.triggers.Add(entry);
    }

    private void EnsureClickSfx(Image targetImage)
    {
        Button button = targetImage.GetComponent<Button>();

        if (button == null)
        {
            button = targetImage.gameObject.AddComponent<Button>();
        }

        button.transition = Selectable.Transition.None;
        button.targetGraphic = targetImage;
        button.onClick.RemoveListener(PlayClickSfx);
        button.onClick.AddListener(PlayClickSfx);
    }

    private void PlayClickSfx()
    {
        if (lastClickSfxFrame == Time.frameCount)
        {
            return;
        }

        lastClickSfxFrame = Time.frameCount;
        UIClickSoundPlayer.Play(gameObject, ref clickSfxSource, clickClip);
    }

    private Color GetDarkPinkHoverColor(Color originalColor)
    {
        Color tint = HandbookHoverTint;
        tint.a = originalColor.a;
        Color tintedColor = Color.Lerp(originalColor, tint, HandbookHoverTintAmount);
        Color hoverColor = Color.Lerp(tintedColor, Color.black, HandbookHoverDarkenAmount);
        hoverColor.a = originalColor.a;
        return hoverColor;
    }
}
