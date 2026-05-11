using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HandbookPanelController : MonoBehaviour
{
    private const string InventoryCloseButtonName = "InventoryCloseButton";

    private static readonly string[] InventoryPanelNames =
    {
        "InventoryPanel1",
        "InventroyPanel1",
        "InventoryPanel",
        "InventroyPanel"
    };

    [SerializeField] private Image handbookClickImage;
    [SerializeField] private GameObject inventoryPanel;

    private readonly List<Coroutine> floatRoutines = new List<Coroutine>();
    private bool handbookInteractionsReady;

    private void OnEnable()
    {
        SetupHandbookInteractions();
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
        EnsureInventoryCloseButton(targetPanel);
        SetupInventoryImageEffects(targetPanel);
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

    private void SetupInventoryImageEffects(GameObject targetPanel)
    {
        StopFloatRoutines();

        Image[] images = targetPanel.GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            SetupPinkHover(image, GetLightPinkHoverColor);

            if (image.gameObject.name == "Image")
            {
                continue;
            }

            RectTransform rectTransform = image.rectTransform;
            Vector2 basePosition = rectTransform.anchoredPosition;
            float amplitude = 8f + (i % 4) * 2.5f;
            float speed = 0.65f + (i % 5) * 0.18f;
            float phase = i * 0.71f;
            floatRoutines.Add(StartCoroutine(FloatInventoryImage(rectTransform, basePosition, amplitude, speed, phase)));
        }
    }

    private void EnsureInventoryCloseButton(GameObject targetPanel)
    {
        EnsureEventSystem();

        RectTransform panelTransform = targetPanel.transform as RectTransform;

        if (panelTransform == null)
        {
            return;
        }

        Transform existingButton = targetPanel.transform.Find(InventoryCloseButtonName);
        TextMeshProUGUI closeText;
        Button closeButton;

        if (existingButton != null)
        {
            closeText = existingButton.GetComponent<TextMeshProUGUI>();
            closeButton = existingButton.GetComponent<Button>();
        }
        else
        {
            GameObject closeObject = new GameObject(InventoryCloseButtonName, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(Button));
            closeObject.transform.SetParent(panelTransform, false);

            RectTransform closeTransform = closeObject.GetComponent<RectTransform>();
            closeTransform.anchorMin = new Vector2(1f, 1f);
            closeTransform.anchorMax = new Vector2(1f, 1f);
            closeTransform.pivot = new Vector2(0.5f, 0.5f);
            closeTransform.anchoredPosition = new Vector2(-48f, -44f);
            closeTransform.sizeDelta = new Vector2(80f, 80f);

            closeText = closeObject.GetComponent<TextMeshProUGUI>();
            closeButton = closeObject.GetComponent<Button>();
        }

        if (closeText != null)
        {
            closeText.text = "X";
            closeText.fontSize = 50f;
            closeText.color = Color.white;
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.raycastTarget = true;
        }

        if (closeButton != null)
        {
            closeButton.transition = Selectable.Transition.None;
            closeButton.targetGraphic = closeText;
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => CloseInventoryPanel(targetPanel));
        }

        if (existingButton != null)
        {
            existingButton.SetAsLastSibling();
        }
        else
        {
            targetPanel.transform.Find(InventoryCloseButtonName)?.SetAsLastSibling();
        }
    }

    private void CloseInventoryPanel(GameObject targetPanel)
    {
        StopFloatRoutines();
        targetPanel.SetActive(false);
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
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

    private void StopFloatRoutines()
    {
        for (int i = 0; i < floatRoutines.Count; i++)
        {
            if (floatRoutines[i] != null)
            {
                StopCoroutine(floatRoutines[i]);
            }
        }

        floatRoutines.Clear();
    }

    private IEnumerator FloatInventoryImage(RectTransform rectTransform, Vector2 basePosition, float amplitude, float speed, float phase)
    {
        while (rectTransform != null && rectTransform.gameObject.activeInHierarchy)
        {
            float offsetY = Mathf.Sin(Time.unscaledTime * speed + phase) * amplitude;
            rectTransform.anchoredPosition = basePosition + Vector2.up * offsetY;
            yield return null;
        }
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

    private Color GetLightPinkHoverColor(Color originalColor)
    {
        return Color.Lerp(originalColor, new Color(1f, 0.72f, 0.86f, originalColor.a), 0.35f);
    }

    private Color GetDarkPinkHoverColor(Color originalColor)
    {
        Color tintedColor = Color.Lerp(originalColor, new Color(0.72f, 0.34f, 0.5f, originalColor.a), 0.38f);
        return Color.Lerp(tintedColor, Color.black, 0.12f);
    }
}
