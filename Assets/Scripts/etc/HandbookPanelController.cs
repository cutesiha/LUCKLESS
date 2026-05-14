using UnityEngine;
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
        Image clickImage = GetHandbookClickImage();
        BattleInventoryOpener opener = clickImage != null ? clickImage.GetComponent<BattleInventoryOpener>() : null;

        if (opener != null)
        {
            opener.OpenInventory();
            return;
        }

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

        EnsureBattleInventoryOpener(targetImage);
    }

    private void EnsureBattleInventoryOpener(Image targetImage)
    {
        BattleInventoryOpener opener = targetImage.GetComponent<BattleInventoryOpener>();
        if (opener == null)
        {
            opener = targetImage.gameObject.AddComponent<BattleInventoryOpener>();
        }

        opener.SetInventoryPanel(GetInventoryPanel());
    }

    private void EnsureHandbookCanvasOnTop()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = 4500;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
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
}
