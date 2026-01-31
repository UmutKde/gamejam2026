using UnityEngine;
using UnityEngine.SceneManagement; // Sahne geçiþleri için þart

public class GameFlowManager : MonoBehaviour
{
    // 1. SINEMATÝK SAHNESÝ ÝÇÝN
    // Sinematiði geç butonuna veya videonun bittiði event'e baðla
    public void SkipToMenu()
    {
        SceneManager.LoadScene("uiScene");
    }

    // 2. ANA MENÜ (UI SCENE) ÝÇÝN
    public void PlayGame()
    {
        SceneManager.LoadScene("gameScene");
    }

    public void OpenSettings()
    {
        SceneManager.LoadScene("settingsScene");
    }

    public void QuitGame()
    {
        Debug.Log("Oyundan Çýkýldý"); // Editörde çalýþmaz, build alýnca çalýþýr
        Application.Quit();
    }

    // 3. AYARLAR VE OYUN ÝÇÝNDEN GERÝ DÖNMEK ÝÇÝN
    public void ReturnToMenu()
    {
        SceneManager.LoadScene("uiScene");
    }
}