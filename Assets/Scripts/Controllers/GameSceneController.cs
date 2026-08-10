using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Управляет окном подтверждения выхода
/// и возвращением в главное меню.
/// </summary>
public class GameSceneController : MonoBehaviour
{
    [Header("Окно подтверждения")]

    [SerializeField]
    private GameObject exitConfirmPanel;

    [Header("Главное меню")]

    [SerializeField]
    private string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        if (exitConfirmPanel != null)
        {
            exitConfirmPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Открывает окно подтверждения.
    /// </summary>
    public void OpenExitConfirmation()
    {
        if (exitConfirmPanel == null)
        {
            Debug.LogError(
                "Не назначено поле Exit Confirm Panel."
            );

            return;
        }

        exitConfirmPanel.SetActive(true);
    }

    /// <summary>
    /// Закрывает окно и возвращает игрока к документу.
    /// </summary>
    public void CloseExitConfirmation()
    {
        if (exitConfirmPanel != null)
        {
            exitConfirmPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Загружает сцену главного меню.
    /// </summary>
    public void ReturnToMainMenu()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogError(
                "Название сцены главного меню не указано."
            );

            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}