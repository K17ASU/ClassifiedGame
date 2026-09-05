using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет историей checkpoint'ов кампании.
///
/// saveVersion 2:
/// checkpoint'ы образуют дерево.
///
/// saveVersion 3:
/// каждый checkpoint хранит StoryState своей ветки.
/// </summary>
public static class CampaignProgress
{
    private const int CurrentSaveVersion = 3;

    private static CampaignSaveData currentCampaign;

    public static bool IsInitialized =>
        currentCampaign != null;

    public static bool CanContinue()
    {
        CampaignSaveData campaign =
            LoadAndUpgradeCampaign();

        if (!IsValidCampaign(campaign))
        {
            return false;
        }

        CheckpointSaveData checkpoint =
            FindCheckpointById(
                campaign,
                campaign.activeCheckpointId
            );

        return checkpoint != null &&
               !string.IsNullOrWhiteSpace(
                   checkpoint.currentDocumentId
               );
    }

    public static bool TryLoadActiveCheckpoint(
        out CheckpointSaveData checkpoint)
    {
        checkpoint = null;

        CampaignSaveData loadedCampaign =
            LoadAndUpgradeCampaign();

        if (!IsValidCampaign(loadedCampaign))
        {
            return false;
        }

        CheckpointSaveData activeCheckpoint =
            FindCheckpointById(
                loadedCampaign,
                loadedCampaign.activeCheckpointId
            );

        if (activeCheckpoint == null)
        {
            Debug.LogError(
                "CampaignProgress: активный checkpoint не найден."
            );
            return false;
        }

        if (string.IsNullOrWhiteSpace(
                activeCheckpoint.currentDocumentId))
        {
            Debug.Log(
                "CampaignProgress: выбранная ветка кампании завершена."
            );
            return false;
        }

        currentCampaign =
            loadedCampaign;

        checkpoint =
            activeCheckpoint;

        Debug.Log(
            $"CampaignProgress: загружен checkpoint " +
            $"#{checkpoint.checkpointId} перед документом " +
            $"{checkpoint.currentDocumentId}."
        );

        return true;
    }

    public static bool TryLoadLatestCheckpoint(
        out CheckpointSaveData checkpoint)
    {
        return TryLoadActiveCheckpoint(
            out checkpoint
        );
    }

    public static bool SelectCheckpoint(
        int checkpointId)
    {
        CampaignSaveData campaign =
            LoadAndUpgradeCampaign();

        if (!IsValidCampaign(campaign))
        {
            return false;
        }

        CheckpointSaveData checkpoint =
            FindCheckpointById(
                campaign,
                checkpointId
            );

        if (checkpoint == null)
        {
            Debug.LogError(
                $"CampaignProgress: checkpoint #{checkpointId} не существует."
            );
            return false;
        }

        if (string.IsNullOrWhiteSpace(
                checkpoint.currentDocumentId))
        {
            Debug.LogError(
                $"CampaignProgress: checkpoint #{checkpointId} " +
                $"является концом ветки."
            );
            return false;
        }

        campaign.activeCheckpointId =
            checkpointId;

        currentCampaign =
            campaign;

        if (!SaveManager.SaveCampaign(
                currentCampaign))
        {
            return false;
        }

        Debug.Log(
            $"CampaignProgress: выбран checkpoint #{checkpointId}. " +
            $"Существующие ветки сохранены."
        );

        return true;
    }

    public static bool DeleteBranch(
        int branchRootCheckpointId,
        out int deletedCheckpointCount)
    {
        deletedCheckpointCount = 0;

        CampaignSaveData campaign =
            LoadAndUpgradeCampaign();

        if (!IsValidCampaign(campaign))
        {
            return false;
        }

        CheckpointSaveData branchRoot =
            FindCheckpointById(
                campaign,
                branchRootCheckpointId
            );

        if (branchRoot == null)
        {
            Debug.LogError(
                $"CampaignProgress: checkpoint #{branchRootCheckpointId} не существует."
            );
            return false;
        }

        if (branchRoot.parentCheckpointId < 0)
        {
            Debug.LogWarning(
                "CampaignProgress: корневой checkpoint удалить нельзя."
            );
            return false;
        }

        HashSet<int> idsToDelete =
            CollectBranchCheckpointIds(
                campaign,
                branchRootCheckpointId
            );

        if (idsToDelete.Count == 0)
        {
            return false;
        }

        bool activeCheckpointWillBeDeleted =
            idsToDelete.Contains(
                campaign.activeCheckpointId
            );

        if (activeCheckpointWillBeDeleted)
        {
            campaign.activeCheckpointId =
                branchRoot.parentCheckpointId;
        }

        for (int i =
                 campaign.checkpoints.Count - 1;
             i >= 0;
             i--)
        {
            CheckpointSaveData checkpoint =
                campaign.checkpoints[i];

            if (checkpoint != null &&
                idsToDelete.Contains(
                    checkpoint.checkpointId
                ))
            {
                campaign.checkpoints.RemoveAt(i);
                deletedCheckpointCount++;
            }
        }

        currentCampaign =
            campaign;

        if (!SaveManager.SaveCampaign(
                currentCampaign))
        {
            return false;
        }

        Debug.Log(
            $"CampaignProgress: удалена ветка от checkpoint " +
            $"#{branchRootCheckpointId}. " +
            $"Удалено checkpoint'ов: {deletedCheckpointCount}. " +
            $"Активный checkpoint: #{campaign.activeCheckpointId}."
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
                saveVersion =
                    CurrentSaveVersion,

                activeCheckpointId = 0,
                nextCheckpointId = 1,

                hasActiveCheckpointSelection = true,
                activeCheckpointIndex = 0
            };

        CheckpointSaveData initialCheckpoint =
            new CheckpointSaveData
            {
                checkpointId = 0,
                parentCheckpointId = -1,
                checkpointIndex = 0,
                currentDocumentId = firstDocumentId,
                totalScore = 0,
                unlockedToolsMask = initialToolsMask,

                unlockedCodexEntryIds =
                    CloneStrings(
                        unlockedCodexEntryIds
                    ),

                readCodexEntryIds =
                    CloneStrings(
                        readCodexEntryIds
                    ),

                storyState =
                    new List<StoryStateSaveData>()
            };

        currentCampaign.checkpoints.Add(
            initialCheckpoint
        );

        if (SaveManager.SaveCampaign(
                currentCampaign))
        {
            Debug.Log(
                $"CampaignProgress: создан корневой checkpoint #0 " +
                $"перед документом {firstDocumentId}."
            );
        }
    }

    // Старый overload оставлен для совместимости.
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
        RecordDocumentCompletion(
            completedDocumentId,
            documentScore,
            passed,
            inspectionsUsed,
            nextDocumentId,
            totalScore,
            unlockedToolsMask,
            unlockedCodexEntryIds,
            readCodexEntryIds,
            null
        );
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
        IReadOnlyList<string> readCodexEntryIds,
        IReadOnlyList<StoryStateSaveData> storyState)
    {
        if (currentCampaign == null)
        {
            currentCampaign =
                LoadAndUpgradeCampaign();
        }

        if (!IsValidCampaign(currentCampaign))
        {
            Debug.LogError(
                "CampaignProgress: кампания не инициализирована."
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(
                completedDocumentId))
        {
            Debug.LogError(
                "CampaignProgress: у завершённого документа отсутствует Document Id."
            );
            return;
        }

        CheckpointSaveData activeCheckpoint =
            FindCheckpointById(
                currentCampaign,
                currentCampaign.activeCheckpointId
            );

        if (activeCheckpoint == null)
        {
            Debug.LogError(
                "CampaignProgress: активный checkpoint не найден."
            );
            return;
        }

        bool createsBranch =
            HasChildCheckpoint(
                currentCampaign,
                activeCheckpoint.checkpointId
            );

        int newCheckpointId =
            currentCampaign.nextCheckpointId;

        currentCampaign.nextCheckpointId++;

        List<StoryStateSaveData> nextStoryState =
            storyState != null
                ? CloneStoryState(storyState)
                : CloneStoryState(
                    activeCheckpoint.storyState
                );

        CheckpointSaveData nextCheckpoint =
            new CheckpointSaveData
            {
                checkpointId =
                    newCheckpointId,

                parentCheckpointId =
                    activeCheckpoint.checkpointId,

                checkpointIndex =
                    newCheckpointId,

                currentDocumentId =
                    nextDocumentId ?? string.Empty,

                totalScore =
                    totalScore,

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
                    ),

                storyState =
                    nextStoryState
            };

        nextCheckpoint.completedDocuments.Add(
            new DocumentResultSaveData
            {
                documentId =
                    completedDocumentId,

                score =
                    documentScore,

                passed =
                    passed,

                inspectionsUsed =
                    Mathf.Max(
                        0,
                        inspectionsUsed
                    )
            }
        );

        currentCampaign.checkpoints.Add(
            nextCheckpoint
        );

        currentCampaign.activeCheckpointId =
            newCheckpointId;

        if (SaveManager.SaveCampaign(
                currentCampaign))
        {
            string nextLabel =
                string.IsNullOrWhiteSpace(
                    nextDocumentId)
                    ? "CAMPAIGN_END"
                    : nextDocumentId;

            string branchLabel =
                createsBranch
                    ? " Создана новая ветка."
                    : string.Empty;

            Debug.Log(
                $"CampaignProgress: создан checkpoint " +
                $"#{newCheckpointId} от #{activeCheckpoint.checkpointId}. " +
                $"Завершён {completedDocumentId}, следующий: {nextLabel}, " +
                $"общий счёт: {totalScore}, " +
                $"StoryState: {nextStoryState.Count}.{branchLabel}"
            );
        }
    }

    private static HashSet<int>
        CollectBranchCheckpointIds(
            CampaignSaveData campaign,
            int branchRootCheckpointId)
    {
        HashSet<int> result =
            new HashSet<int>();

        Queue<int> queue =
            new Queue<int>();

        queue.Enqueue(
            branchRootCheckpointId
        );

        while (queue.Count > 0)
        {
            int currentId =
                queue.Dequeue();

            if (!result.Add(currentId))
            {
                continue;
            }

            foreach (
                CheckpointSaveData checkpoint
                in campaign.checkpoints)
            {
                if (checkpoint != null &&
                    checkpoint.parentCheckpointId ==
                    currentId)
                {
                    queue.Enqueue(
                        checkpoint.checkpointId
                    );
                }
            }
        }

        return result;
    }

    private static CampaignSaveData
        LoadAndUpgradeCampaign()
    {
        CampaignSaveData campaign =
            SaveManager.LoadCampaign();

        if (campaign == null)
        {
            return null;
        }

        bool saveWasUpgraded =
            false;

        if (campaign.saveVersion == 1)
        {
            UpgradeVersion1ToVersion2(
                campaign
            );

            saveWasUpgraded =
                true;
        }

        if (campaign.saveVersion == 2)
        {
            UpgradeVersion2ToVersion3(
                campaign
            );

            saveWasUpgraded =
                true;
        }

        if (campaign.saveVersion ==
            CurrentSaveVersion)
        {
            RepairVersion3Metadata(
                campaign
            );
        }

        if (saveWasUpgraded)
        {
            SaveManager.SaveCampaign(
                campaign
            );

            Debug.Log(
                $"CampaignProgress: сохранение обновлено " +
                $"до версии {CurrentSaveVersion}."
            );
        }

        return campaign;
    }

    private static void UpgradeVersion1ToVersion2(
        CampaignSaveData campaign)
    {
        if (campaign.checkpoints == null)
        {
            campaign.checkpoints =
                new List<CheckpointSaveData>();
        }

        for (int i = 0;
             i < campaign.checkpoints.Count;
             i++)
        {
            CheckpointSaveData checkpoint =
                campaign.checkpoints[i];

            if (checkpoint == null)
            {
                continue;
            }

            checkpoint.checkpointId =
                i;

            checkpoint.parentCheckpointId =
                i == 0
                    ? -1
                    : i - 1;

            checkpoint.checkpointIndex =
                i;
        }

        int activeListIndex;

        if (campaign.hasActiveCheckpointSelection)
        {
            activeListIndex =
                Mathf.Clamp(
                    campaign.activeCheckpointIndex,
                    0,
                    Mathf.Max(
                        0,
                        campaign.checkpoints.Count - 1
                    )
                );
        }
        else
        {
            activeListIndex =
                Mathf.Max(
                    0,
                    campaign.checkpoints.Count - 1
                );
        }

        campaign.activeCheckpointId =
            activeListIndex;

        campaign.nextCheckpointId =
            campaign.checkpoints.Count;

        campaign.saveVersion =
            2;

        campaign.hasActiveCheckpointSelection =
            true;

        campaign.activeCheckpointIndex =
            activeListIndex;
    }

    private static void UpgradeVersion2ToVersion3(
        CampaignSaveData campaign)
    {
        if (campaign.checkpoints == null)
        {
            campaign.checkpoints =
                new List<CheckpointSaveData>();
        }

        foreach (
            CheckpointSaveData checkpoint
            in campaign.checkpoints)
        {
            if (checkpoint == null)
            {
                continue;
            }

            if (checkpoint.storyState == null)
            {
                checkpoint.storyState =
                    new List<StoryStateSaveData>();
            }
        }

        campaign.saveVersion =
            3;
    }

    private static void RepairVersion3Metadata(
        CampaignSaveData campaign)
    {
        if (campaign.checkpoints == null)
        {
            campaign.checkpoints =
                new List<CheckpointSaveData>();

            return;
        }

        int maxId =
            -1;

        foreach (
            CheckpointSaveData checkpoint
            in campaign.checkpoints)
        {
            if (checkpoint == null)
            {
                continue;
            }

            if (checkpoint.checkpointId >
                maxId)
            {
                maxId =
                    checkpoint.checkpointId;
            }

            if (checkpoint.completedDocuments == null)
            {
                checkpoint.completedDocuments =
                    new List<DocumentResultSaveData>();
            }

            if (checkpoint.unlockedCodexEntryIds == null)
            {
                checkpoint.unlockedCodexEntryIds =
                    new List<string>();
            }

            if (checkpoint.readCodexEntryIds == null)
            {
                checkpoint.readCodexEntryIds =
                    new List<string>();
            }

            if (checkpoint.storyState == null)
            {
                checkpoint.storyState =
                    new List<StoryStateSaveData>();
            }
        }

        if (campaign.nextCheckpointId <=
            maxId)
        {
            campaign.nextCheckpointId =
                maxId + 1;
        }

        if (FindCheckpointById(
                campaign,
                campaign.activeCheckpointId) ==
            null)
        {
            campaign.activeCheckpointId =
                maxId;
        }
    }

    private static bool IsValidCampaign(
        CampaignSaveData campaign)
    {
        if (campaign == null)
        {
            return false;
        }

        if (campaign.saveVersion !=
            CurrentSaveVersion)
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

    private static CheckpointSaveData
        FindCheckpointById(
            CampaignSaveData campaign,
            int checkpointId)
    {
        if (campaign == null ||
            campaign.checkpoints == null)
        {
            return null;
        }

        foreach (
            CheckpointSaveData checkpoint
            in campaign.checkpoints)
        {
            if (checkpoint != null &&
                checkpoint.checkpointId ==
                checkpointId)
            {
                return checkpoint;
            }
        }

        return null;
    }

    private static bool HasChildCheckpoint(
        CampaignSaveData campaign,
        int parentCheckpointId)
    {
        if (campaign == null ||
            campaign.checkpoints == null)
        {
            return false;
        }

        foreach (
            CheckpointSaveData checkpoint
            in campaign.checkpoints)
        {
            if (checkpoint != null &&
                checkpoint.parentCheckpointId ==
                parentCheckpointId)
            {
                return true;
            }
        }

        return false;
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

        foreach (
            DocumentResultSaveData item
            in source)
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

    private static List<StoryStateSaveData>
        CloneStoryState(
            IReadOnlyList<StoryStateSaveData> source)
    {
        List<StoryStateSaveData> result =
            new List<StoryStateSaveData>();

        if (source == null)
        {
            return result;
        }

        foreach (
            StoryStateSaveData item
            in source)
        {
            if (item == null ||
                string.IsNullOrWhiteSpace(
                    item.fragmentId))
            {
                continue;
            }

            result.Add(
                new StoryStateSaveData
                {
                    fragmentId =
                        item.fragmentId,

                    state =
                        item.state
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

        for (int i = 0;
             i < source.Count;
             i++)
        {
            string value =
                source[i];

            if (string.IsNullOrWhiteSpace(
                    value))
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
