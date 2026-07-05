using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject optionPanel;
    public GameObject loadPanel;
    public GameObject exitPopup;

    [Header("Audio")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    private void Start()
    {
        EnsurePanelAnimator(optionPanel);
        EnsurePanelAnimator(loadPanel);
        EnsurePanelAnimator(exitPopup);

        optionPanel.SetActive(false);
        loadPanel.SetActive(false);
        exitPopup.SetActive(false);

        if (bgmSlider != null)
        {
            bgmSlider.value = PlayerPrefs.GetFloat("BGM", 1f);
            bgmSlider.onValueChanged.AddListener(SetBGM);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = PlayerPrefs.GetFloat("SFX", 1f);
            sfxSlider.onValueChanged.AddListener(SetSFX);
        }
    }

    public void OnClickStart()
    {
        NewGameReset.ResetProgressForNewGame();
        SceneManager.LoadScene("Prologue");
    }

    public void OnClickLoad()
    {
        OpenPanel(loadPanel);
    }

    public void CloseLoad()
    {
        ClosePanel(loadPanel);
    }

    public void OnClickOption()
    {
        OpenPanel(optionPanel);
    }

    public void CloseOption()
    {
        ClosePanel(optionPanel);
    }

    public void OnClickExit()
    {
        OpenPanel(exitPopup);
    }

    public void ExitYes()
    {
        Application.Quit();
        Debug.Log("게임 종료");
    }

    public void ExitNo()
    {
        ClosePanel(exitPopup);
    }

    private void OpenPanel(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();

        UIPanelBoingAnimator animator = EnsurePanelAnimator(panel);
        if (animator != null)
        {
            animator.PlayOpen();
        }
    }

    private void ClosePanel(GameObject panel)
    {
        if (panel == null || !panel.activeInHierarchy)
        {
            return;
        }

        UIPanelBoingAnimator animator = EnsurePanelAnimator(panel);
        if (animator == null)
        {
            panel.SetActive(false);
            return;
        }

        animator.PlayClose(() => panel.SetActive(false));
    }

    private UIPanelBoingAnimator EnsurePanelAnimator(GameObject panel)
    {
        if (panel == null)
        {
            return null;
        }

        UIPanelBoingAnimator animator = panel.GetComponent<UIPanelBoingAnimator>();
        if (animator == null)
        {
            animator = panel.AddComponent<UIPanelBoingAnimator>();
        }

        return animator;
    }

    private void SetBGM(float value)
    {
        PlayerPrefs.SetFloat("BGM", value);
        PlayerPrefs.Save();
    }

    private void SetSFX(float value)
    {
        PlayerPrefs.SetFloat("SFX", value);
        PlayerPrefs.Save();
    }
}
