using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Управляет главным меню и регистрацией имени игрока.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Игровая сцена")]

    [SerializeField]
    private string gameSceneName = "GameScene";

    [Header("Имя сотрудника")]

    [SerializeField]
    private TMP_InputField playerNameInput;

    [SerializeField]
    private Button startGameButton;

    [Header("Сохранения")]

    [SerializeField]
    private Button continueGameButton;

    private bool isUpdatingInput;

    private void Start()
    {
        if (playerNameInput == null)
        {
            Debug.LogError(
                "MainMenuController: не назначено поле Player Name Input."
            );

            return;
        }

        playerNameInput.characterLimit = 24;

        string savedPlayerName = PlayerProfile.PlayerName;

        if (!string.IsNullOrWhiteSpace(savedPlayerName))
        {
            playerNameInput.SetTextWithoutNotify(savedPlayerName);
        }

        playerNameInput.onValueChanged.AddListener(
            OnPlayerNameChanged
        );

        RefreshStartButton();
        RefreshContinueButton();
    }

    private void OnDestroy()
    {
        if (playerNameInput != null)
        {
            playerNameInput.onValueChanged.RemoveListener(
                OnPlayerNameChanged
            );
        }
    }

    private void OnPlayerNameChanged(string value)
    {
        if (isUpdatingInput)
        {
            return;
        }

        string sanitizedName =
            PlayerProfile.SanitizePlayerName(value);

        if (sanitizedName != value)
        {
            isUpdatingInput = true;

            playerNameInput.SetTextWithoutNotify(
                sanitizedName
            );

            playerNameInput.caretPosition =
                sanitizedName.Length;

            isUpdatingInput = false;
        }

        RefreshStartButton();
    }

    private void RefreshStartButton()
    {
        if (startGameButton == null)
        {
            return;
        }

        string playerName =
            playerNameInput != null
                ? PlayerProfile.SanitizePlayerName(
                    playerNameInput.text
                )
                : string.Empty;

        startGameButton.interactable =
            !string.IsNullOrWhiteSpace(playerName);
    }

    private void RefreshContinueButton()
    {
        if (continueGameButton == null)
        {
            return;
        }

        continueGameButton.interactable =
            CampaignProgress.CanContinue();
    }

    public void StartNewGame()
    {
        if (playerNameInput == null)
        {
            Debug.LogError(
                "MainMenuController: не назначено поле Player Name Input."
            );

            return;
        }

        string playerName =
            PlayerProfile.SanitizePlayerName(
                playerNameInput.text
            );

        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerNameInput.ActivateInputField();
            return;
        }

        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError(
                "Название игровой сцены не указано."
            );

            return;
        }

        PlayerProfile.SetPlayerName(playerName);

        if (!SaveManager.DeleteSave())
        {
            Debug.LogError(
                "MainMenuController: не удалось очистить старое сохранение."
            );
            return;
        }

        GameSessionRequest.RequestNewGame();

        SceneManager.LoadScene(gameSceneName);
    }

    public void ContinueGame()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError(
                "Название игровой сцены не указано."
            );

            return;
        }

        if (!CampaignProgress.CanContinue())
        {
            Debug.LogWarning(
                "MainMenuController: продолжать нечего."
            );

            RefreshContinueButton();
            return;
        }

        GameSessionRequest.RequestContinue();

        SceneManager.LoadScene(gameSceneName);
    }

    public void LoadCheckpoint(
        int checkpointIndex)
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError(
                "Название игровой сцены не указано."
            );

            return;
        }

        if (!CampaignProgress.SelectCheckpoint(
                checkpointIndex))
        {
            return;
        }

        GameSessionRequest.RequestContinue();

        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
