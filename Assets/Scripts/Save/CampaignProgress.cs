using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Хранит текущую историю checkpoint'ов в памяти
/// и передаёт её SaveManager для записи в JSON.
/// </summary>
public static class CampaignProgress
{
    private static CampaignSaveData currentCampaign;

    public static bool IsInitialized =>
        currentCampaign != null;

    public static bool CanContinue()
    {
        CampaignSaveData campaign =
            SaveManager.LoadCampaign();

        if (!IsValidCampaign(campaign))
        {
            return false;
        }

        int activeIndex =
            GetSafeActiveCheckpointIndex(campaign);

        CheckpointSaveData checkpoint =
            campaign.checkpoints[activeIndex];

        return checkpoint != null &&
               !string.IsNullOrWhiteSpace(
                   checkpoint.currentDocumentId
               );
    }

    /// <summary>
    /// Загружает активный checkpoint.
    /// Для обычного прохождения активным является последний.
    /// После выбора старого checkpoint'а — выбранный.
    /// </summary>
    public static bool TryLoadActiveCheckpoint(
        out CheckpointSaveData checkpoint)
    {
        checkpoint = null;

        CampaignSaveData loadedCampaign =
            SaveManager.LoadCampaign();

        if (!IsValidCampaign(loadedCampaign))
        {
            return false;
        }

        int activeIndex =
            GetSafeActiveCheckpointIndex(
                loadedCampaign
            );

        CheckpointSaveData activeCheckpoint =
            loadedCampaign.checkpoints[
                activeIndex
            ];

        if (activeCheckpoint == null)
        {
            Debug.LogError(
                "CampaignProgress: активный checkpoint повреждён."
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(
                activeCheckpoint.currentDocumentId))
        {
            Debug.Log(
                "CampaignProgress: кампания уже завершена."
            );

            return false;
        }

        loadedCampaign.hasActiveCheckpointSelection =
            true;

        loadedCampaign.activeCheckpointIndex =
            activeIndex;

        currentCampaign =
            loadedCampaign;

        checkpoint =
            activeCheckpoint;

        Debug.Log(
            $"CampaignProgress: загружен checkpoint " +
            $"#{checkpoint.checkpointIndex} перед документом " +
            $"{checkpoint.currentDocumentId}."
        );

        return true;
    }

    /// <summary>
    /// Оставлено для совместимости с уже добавленным
    /// DocumentRedactor. Теперь метод загружает активный,
    /// а не обязательно последний checkpoint.
    /// </summary>
    public static bool TryLoadLatestCheckpoint(
        out CheckpointSaveData checkpoint)
    {
        return TryLoadActiveCheckpoint(
            out checkpoint
        );
    }

    /// <summary>
    /// Выбирает любой существующий checkpoint как активный.
    /// Будущие checkpoint'ы НЕ удаляются в этот момент.
    /// Они будут удалены только после завершения
    /// переигрываемого документа.
    /// </summary>
    public static bool SelectCheckpoint(
        int checkpointIndex)
    {
        CampaignSaveData campaign =
            SaveManager.LoadCampaign();

        if (!IsValidCampaign(campaign))
        {
            return false;
        }

        if (checkpointIndex < 0 ||
            checkpointIndex >=
            campaign.checkpoints.Count)
        {
            Debug.LogError(
                $"CampaignProgress: checkpoint #{checkpointIndex} не существует."
            );

            return false;
        }

        CheckpointSaveData checkpoint =
            campaign.checkpoints[
                checkpointIndex
            ];

        if (checkpoint == null ||
            string.IsNullOrWhiteSpace(
                checkpoint.currentDocumentId))
        {
            Debug.LogError(
                $"CampaignProgress: checkpoint #{checkpointIndex} нельзя загрузить."
            );

            return false;
        }

        campaign.hasActiveCheckpointSelection =
            true;

        campaign.activeCheckpointIndex =
            checkpointIndex;

        currentCampaign =
            campaign;

        if (!SaveManager.SaveCampaign(
                currentCampaign))
        {
            return false;
        }

        Debug.Log(
            $"CampaignProgress: выбран checkpoint #{checkpointIndex} " +
            $"перед документом {checkpoint.currentDocumentId}. " +
            $"Будущие checkpoint'ы пока сохранены."
        );

        return true;
    }

    public static void StartNewCampaign(
        string firstDocumentId,
        int initialToolsMask,
        IReadOnlyList<string> unlockedCodexEntryIds = null,
        IReadOnlyList<string> readCodexEntryIds = null)
    {
        if (string.IsNullOrWhiteSpace(firstDocumentId))
        {
            Debug.LogError(
                "CampaignProgress: у первого документа отсутствует Document Id."
            );
            return;
        }

        currentCampaign =
            new CampaignSaveData
            {
                hasActiveCheckpointSelection = true,
                activeCheckpointIndex = 0
            };

        CheckpointSaveData initialCheckpoint =
            new CheckpointSaveData
            {
                checkpointIndex = 0,
                currentDocumentId = firstDocumentId,
                totalScore = 0,
                unlockedToolsMask = initialToolsMask,
                unlockedCodexEntryIds =
                    CloneStrings(unlockedCodexEntryIds),
                readCodexEntryIds =
                    CloneStrings(readCodexEntryIds)
            };

        currentCampaign.checkpoints.Add(
            initialCheckpoint
        );

        if (SaveManager.SaveCampaign(currentCampaign))
        {
            Debug.Log(
                $"CampaignProgress: создан checkpoint #0 перед документом {firstDocumentId}."
            );
        }
    }

    public static void RecordDocumentCompletion(
        string completedDocumentId,
        int documentScore,
        bool passed,
        int inspectionsUsed,
        string nextDocumentId,
        int totalScore,
        int unlockedToolsMask,
        IReadOnlyList<string> unlockedCodexEntryIds,
        IReadOnlyList<string> readCodexEntryIds)
    {
        if (currentCampaign == null)
        {
            currentCampaign =
                SaveManager.LoadCampaign();
        }

        if (!IsValidCampaign(currentCampaign))
        {
            Debug.LogError(
                "CampaignProgress: кампания не инициализирована."
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(completedDocumentId))
        {
            Debug.LogError(
                "CampaignProgress: у завершённого документа отсутствует Document Id."
            );
            return;
        }

        int activeIndex =
            GetSafeActiveCheckpointIndex(
                currentCampaign
            );

        CheckpointSaveData activeCheckpoint =
            currentCampaign.checkpoints[
                activeIndex
            ];

        if (activeCheckpoint == null)
        {
            Debug.LogError(
                "CampaignProgress: активный checkpoint повреждён."
            );
            return;
        }

        // Если игрок вернулся назад, старое будущее
        // удаляется только ПОСЛЕ завершения документа.
        int firstFutureIndex =
            activeIndex + 1;

        if (firstFutureIndex <
            currentCampaign.checkpoints.Count)
        {
            currentCampaign.checkpoints.RemoveRange(
                firstFutureIndex,
                currentCampaign.checkpoints.Count -
                firstFutureIndex
            );

            Debug.Log(
                $"CampaignProgress: старые будущие checkpoint'ы " +
                $"после #{activeIndex} удалены."
            );
        }

        CheckpointSaveData nextCheckpoint =
            new CheckpointSaveData
            {
                checkpointIndex =
                    currentCampaign.checkpoints.Count,

                currentDocumentId =
                    nextDocumentId ?? string.Empty,

                totalScore = totalScore,

                unlockedToolsMask =
                    unlockedToolsMask,

                completedDocuments =
                    CloneDocumentResults(
                        activeCheckpoint.completedDocuments
                    ),

                unlockedCodexEntryIds =
                    CloneStrings(
                        unlockedCodexEntryIds
                    ),

                readCodexEntryIds =
                    CloneStrings(
                        readCodexEntryIds
                    )
            };

        nextCheckpoint.completedDocuments.Add(
            new DocumentResultSaveData
            {
                documentId = completedDocumentId,
                score = documentScore,
                passed = passed,
                inspectionsUsed =
                    Mathf.Max(0, inspectionsUsed)
            }
        );

        currentCampaign.checkpoints.Add(
            nextCheckpoint
        );

        currentCampaign.hasActiveCheckpointSelection =
            true;

        currentCampaign.activeCheckpointIndex =
            nextCheckpoint.checkpointIndex;

        if (SaveManager.SaveCampaign(currentCampaign))
        {
            string nextLabel =
                string.IsNullOrWhiteSpace(nextDocumentId)
                    ? "CAMPAIGN_END"
                    : nextDocumentId;

            Debug.Log(
                $"CampaignProgress: создан checkpoint #{nextCheckpoint.checkpointIndex}. " +
                $"Завершён {completedDocumentId}, следующий: {nextLabel}, " +
                $"общий счёт: {totalScore}."
            );
        }
    }

    private static bool IsValidCampaign(
        CampaignSaveData campaign)
    {
        if (campaign == null)
        {
            return false;
        }

        if (campaign.saveVersion != 1)
        {
            Debug.LogError(
                $"CampaignProgress: неподдерживаемая версия сохранения: " +
                $"{campaign.saveVersion}."
            );

            return false;
        }

        if (campaign.checkpoints == null ||
            campaign.checkpoints.Count == 0)
        {
            return false;
        }

        return true;
    }

    private static int GetSafeActiveCheckpointIndex(
        CampaignSaveData campaign)
    {
        if (campaign == null ||
            campaign.checkpoints == null ||
            campaign.checkpoints.Count == 0)
        {
            return 0;
        }

        if (!campaign.hasActiveCheckpointSelection)
        {
            return campaign.checkpoints.Count - 1;
        }

        return Mathf.Clamp(
            campaign.activeCheckpointIndex,
            0,
            campaign.checkpoints.Count - 1
        );
    }

    private static List<DocumentResultSaveData>
        CloneDocumentResults(
            List<DocumentResultSaveData> source)
    {
        List<DocumentResultSaveData> result =
            new List<DocumentResultSaveData>();

        if (source == null)
        {
            return result;
        }

        foreach (DocumentResultSaveData item in source)
        {
            if (item == null)
            {
                continue;
            }

            result.Add(
                new DocumentResultSaveData
                {
                    documentId =
                        item.documentId,

                    score =
                        item.score,

                    passed =
                        item.passed,

                    inspectionsUsed =
                        item.inspectionsUsed
                }
            );
        }

        return result;
    }

    private static List<string> CloneStrings(
        IReadOnlyList<string> source)
    {
        List<string> result =
            new List<string>();

        if (source == null)
        {
            return result;
        }

        for (int i = 0; i < source.Count; i++)
        {
            string value = source[i];

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (!result.Contains(value))
            {
                result.Add(value);
            }
        }

        return result;
    }
}
