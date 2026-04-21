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
        SceneManager.LoadScene("Prologue");
    }

    public void OnClickLoad()
    {
        loadPanel.SetActive(true);
    }

    public void CloseLoad()
    {
        loadPanel.SetActive(false);
    }

    public void OnClickOption()
    {
        optionPanel.SetActive(true);
    }

    public void CloseOption()
    {
        optionPanel.SetActive(false);
    }

    public void OnClickExit()
    {
        exitPopup.SetActive(true);
    }

    public void ExitYes()
    {
        Application.Quit();
        Debug.Log("게임 종료");
    }

    public void ExitNo()
    {
        exitPopup.SetActive(false);
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