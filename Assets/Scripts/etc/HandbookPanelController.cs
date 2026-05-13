using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HandbookPanelController : MonoBehaviour
{
    private static readonly string[] InventoryPanelNames =
    {
        "InventoryPanel1",
        "InventroyPanel1",
        "InventoryPanel",
        "InventroyPanel"
    };

    [SerializeField] private Image handbookClickImage;
    [SerializeField] private GameObject inventoryPanel;
    private bool handbookInteractionsReady;

    private void OnEnable()
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

        targetPanel.SetActive(true);
        targetPanel.transform.SetAsLastSibling();
        InventoryPanelController inventoryController = targetPanel.GetComponent<InventoryPanelController>();

        if (inventoryController != null)
        {
            inventoryController.SetupInventoryPanel();
        }
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
        if (handbookInteractionsReady)
        {
            return;
        }

        Image targetImage = GetHandbookClickImage();

        if (targetImage == null)
        {
            return;
        }

        SetupPinkHover(targetImage, GetDarkPinkHoverColor);
        AddClickEvent(targetImage, OpenInventoryPanel);
        handbookInteractionsReady = true;
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

    private Color GetDarkPinkHoverColor(Color originalColor)
    {
        Color tintedColor = Color.Lerp(originalColor, new Color(1f, 0.12f, 0.78f, originalColor.a), 0.38f);
        return Color.Lerp(tintedColor, Color.black, 0.12f);
    }
}
