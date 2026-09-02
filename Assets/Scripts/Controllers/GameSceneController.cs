using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Управляет окном подтверждения выхода
/// и возвратом в главное меню.
/// Также обрабатывает Android Back / Escape.
/// </summary>
public class GameSceneController : MonoBehaviour
{
    [Header("Окно подтверждения")]

    [SerializeField]
    private GameObject exitConfirmPanel;

    [Header("Кодекс")]

    [SerializeField]
    private CodexUIController codexUIController;

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

    private void Update()
    {
        if (Keyboard.current == null ||
            !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        HandleBackAction();
    }

    private void HandleBackAction()
    {
        if (exitConfirmPanel != null &&
            exitConfirmPanel.activeSelf)
        {
            CloseExitConfirmation();
            return;
        }

        if (codexUIController != null &&
            codexUIController.IsOpen)
        {
            codexUIController.CloseCodex();
            return;
        }

        OpenExitConfirmation();
    }

    /// <summary>
    /// Открывает окно подтверждения выхода.
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
