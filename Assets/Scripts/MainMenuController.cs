using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Управляет кнопками главного меню.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Название игровой сцены")]

    [SerializeField]
    private string gameSceneName = "GameScene";

    /// <summary>
    /// Загружает игровую сцену.
    /// </summary>
    public void StartNewGame()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError(
                "Название игровой сцены не указано."
            );

            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Закрывает игру.
    /// В редакторе Unity останавливает режим Play.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}