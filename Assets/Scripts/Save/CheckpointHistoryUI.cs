using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public sealed class CheckpointHistoryUI : MonoBehaviour
{
    [Header("UI")]

    [SerializeField]
    private GameObject historyPanel;

    [SerializeField]
    private Button openHistoryButton;

    [SerializeField]
    private Transform checkpointContent;

    [SerializeField]
    private Button checkpointButtonTemplate;

    [Header("Data")]

    [SerializeField]
    private DocumentCatalog documentCatalog;

    [SerializeField]
    private MainMenuController mainMenuController;

    private readonly List<Button> spawnedButtons =
        new List<Button>();

    private void Start()
    {
        if (historyPanel != null)
        {
            historyPanel.SetActive(false);
        }

        RefreshOpenButton();

        LocalizationSettings.SelectedLocaleChanged +=
            OnSelectedLocaleChanged;
    }

    private void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -=
            OnSelectedLocaleChanged;
    }

    public void OpenHistory()
    {
        if (historyPanel == null)
        {
            return;
        }

        historyPanel.SetActive(true);
        RebuildCheckpointList();
    }

    public void CloseHistory()
    {
        if (historyPanel != null)
        {
            historyPanel.SetActive(false);
        }
    }

    private void RefreshOpenButton()
    {
        if (openHistoryButton == null)
        {
            return;
        }

        CampaignSaveData campaign =
            SaveManager.LoadCampaign();

        int loadableCheckpointCount = 0;

        if (campaign != null &&
            campaign.checkpoints != null)
        {
            foreach (
                CheckpointSaveData checkpoint
                in campaign.checkpoints)
            {
                if (checkpoint != null &&
                    !string.IsNullOrWhiteSpace(
                        checkpoint.currentDocumentId))
                {
                    loadableCheckpointCount++;
                }
            }
        }

        openHistoryButton.interactable =
            loadableCheckpointCount > 1;
    }

    private void RebuildCheckpointList()
    {
        ClearSpawnedButtons();

        if (checkpointContent == null ||
            checkpointButtonTemplate == null ||
            documentCatalog == null ||
            mainMenuController == null)
        {
            Debug.LogError(
                "CheckpointHistoryUI: не назначены ссылки в Inspector."
            );
            return;
        }

        CampaignSaveData campaign =
            SaveManager.LoadCampaign();

        if (campaign == null ||
            campaign.checkpoints == null)
        {
            return;
        }

        foreach (
            CheckpointSaveData checkpoint
            in campaign.checkpoints)
        {
            if (checkpoint == null ||
                string.IsNullOrWhiteSpace(
                    checkpoint.currentDocumentId))
            {
                continue;
            }

            CreateCheckpointButton(
                checkpoint
            );
        }
    }

    private void CreateCheckpointButton(
        CheckpointSaveData checkpoint)
    {
        Button button =
            Instantiate(
                checkpointButtonTemplate,
                checkpointContent
            );

        button.gameObject.SetActive(true);

        TMP_Text label =
            button.GetComponentInChildren<TMP_Text>(
                true
            );

        if (label != null)
        {
            label.text =
                GetCheckpointLabel(checkpoint);
        }

        button.onClick.RemoveAllListeners();

        int capturedCheckpointId =
            checkpoint.checkpointId;

        button.onClick.AddListener(
            () =>
                mainMenuController.LoadCheckpoint(
                    capturedCheckpointId
                )
        );

        spawnedButtons.Add(button);
    }

    private string GetCheckpointLabel(
        CheckpointSaveData checkpoint)
    {
        DocumentData document =
            documentCatalog.FindById(
                checkpoint.currentDocumentId
            );

        string documentLabel =
            checkpoint.currentDocumentId;

        if (document != null)
        {
            string number =
                GetLocalizedText(
                    document.LocalizedDocumentNumber,
                    document.DocumentNumber
                );

            string title =
                GetLocalizedText(
                    document.LocalizedDocumentTitle,
                    document.DocumentTitle
                );

            if (!string.IsNullOrWhiteSpace(number) &&
                !string.IsNullOrWhiteSpace(title))
            {
                documentLabel =
                    $"{number} — {title}";
            }
            else if (!string.IsNullOrWhiteSpace(title))
            {
                documentLabel =
                    title;
            }
            else if (!string.IsNullOrWhiteSpace(number))
            {
                documentLabel =
                    number;
            }
        }

        return
            $"{documentLabel}   |   {checkpoint.totalScore}";
    }

    private string GetLocalizedText(
        LocalizedString localizedString,
        string fallback)
    {
        if (localizedString == null ||
            localizedString.IsEmpty)
        {
            return fallback;
        }

        string result =
            localizedString.GetLocalizedString();

        return string.IsNullOrWhiteSpace(result)
            ? fallback
            : result;
    }

    private void ClearSpawnedButtons()
    {
        foreach (
            Button button
            in spawnedButtons)
        {
            if (button != null)
            {
                Destroy(
                    button.gameObject
                );
            }
        }

        spawnedButtons.Clear();
    }

    private void OnSelectedLocaleChanged(
        Locale locale)
    {
        if (historyPanel != null &&
            historyPanel.activeSelf)
        {
            RebuildCheckpointList();
        }

        RefreshOpenButton();
    }
}
