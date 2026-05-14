using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BattleInventoryOpener : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;

    private Button button;
    private Image targetImage;
    private Color normalColor;

    private void Awake()
    {
        EnsureButton();
    }

    private void OnEnable()
    {
        EnsureButton();
    }

    public void SetInventoryPanel(GameObject panel)
    {
        inventoryPanel = panel;
        EnsureButton();
    }

    private void EnsureButton()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }

        targetImage = GetComponent<Image>();
        targetImage.raycastTarget = true;
        normalColor = targetImage.color;

        button.transition = Selectable.Transition.None;
        button.targetGraphic = targetImage;
        button.onClick.RemoveListener(OpenInventory);
        button.onClick.AddListener(OpenInventory);

        SetupHover();
    }

    public void OpenInventory()
    {
        if (inventoryPanel == null)
        {
            inventoryPanel = FindInventoryPanel();
        }

        if (inventoryPanel == null)
        {
            return;
        }

        InventoryPanelController inventoryController = inventoryPanel.GetComponent<InventoryPanelController>();
        if (inventoryController != null)
        {
            inventoryController.ShowInventoryPanel();
            return;
        }

        inventoryPanel.transform.localScale = Vector3.one;
        inventoryPanel.SetActive(true);
        inventoryPanel.transform.SetAsLastSibling();
    }

    private void SetupHover()
    {
        EventTrigger trigger = GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<EventTrigger>();
        }

        trigger.triggers.RemoveAll(entry =>
            entry.eventID == EventTriggerType.PointerEnter || entry.eventID == EventTriggerType.PointerExit);

        AddPointerEvent(trigger, EventTriggerType.PointerEnter, () =>
        {
            if (targetImage != null)
            {
                targetImage.color = Color.Lerp(normalColor, new Color(1f, 0.12f, 0.78f, normalColor.a), 0.38f);
            }
        });

        AddPointerEvent(trigger, EventTriggerType.PointerExit, () =>
        {
            if (targetImage != null)
            {
                targetImage.color = normalColor;
            }
        });
    }

    private void AddPointerEvent(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(_ => callback());
        trigger.triggers.Add(entry);
    }

    private GameObject FindInventoryPanel()
    {
        string[] names = { "InventoryPanel1", "InventroyPanel1", "InventoryPanel", "InventroyPanel" };

        for (int i = 0; i < names.Length; i++)
        {
            GameObject panel = GameObject.Find(names[i]);
            if (panel != null)
            {
                return panel;
            }
        }

        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < objects.Length; i++)
        {
            for (int j = 0; j < names.Length; j++)
            {
                if (objects[i].name == names[j] && objects[i].scene.IsValid())
                {
                    return objects[i];
                }
            }
        }

        return null;
    }
}
