using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
    public void GoToBattleScene()
    {
        SceneManager.LoadScene("BattleScene");
    }
}