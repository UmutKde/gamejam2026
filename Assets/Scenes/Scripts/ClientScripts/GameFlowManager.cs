using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("gameScene");
    }

    public void QuitGame()
    {
        Debug.Log("Oyundan Çýkýldý");
        Application.Quit();
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("uiScene");
    }
}