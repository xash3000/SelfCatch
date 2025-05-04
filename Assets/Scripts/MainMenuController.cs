using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Management")]
    [Tooltip("Name of the scene to load when Start is pressed")]
    [SerializeField] private string gameSceneName = "GameScene";
    
    public void OnStartButtonPressed()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("MainMenuController: Game scene name is not set.", this);
            return;
        }
        SceneManager.LoadScene(gameSceneName);
    }
    
    public void OnQuitButtonPressed()
    {
#if UNITY_EDITOR
        // Stop play mode in the editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}