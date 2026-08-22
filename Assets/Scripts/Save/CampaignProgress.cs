using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Хранит текущую историю checkpoint'ов в памяти
/// и передаёт её SaveManager для записи в JSON.
/// Загрузка checkpoint'ов будет подключена отдельным этапом.
/// </summary>
public static class CampaignProgress
{
    private static CampaignSaveData currentCampaign;

    public static bool IsInitialized =>
        currentCampaign != null;

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
            new CampaignSaveData();

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
        if (currentCampaign == null ||
            currentCampaign.checkpoints == null ||
            currentCampaign.checkpoints.Count == 0)
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

        CheckpointSaveData previousCheckpoint =
            currentCampaign.checkpoints[
                currentCampaign.checkpoints.Count - 1
            ];

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
                        previousCheckpoint.completedDocuments
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
