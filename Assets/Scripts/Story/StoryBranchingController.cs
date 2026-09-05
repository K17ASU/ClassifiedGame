using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Связывает StoryGraph с текущей кампанией.
///
/// Компонент должен находиться на том же GameObject,
/// что и DocumentRedactor.
///
/// V2 умеет маршрутизировать по состоянию сюжетных
/// фрагментов текущего документа.
/// Глобальное StoryState между разными документами
/// появится отдельным следующим этапом.
/// </summary>
public sealed class StoryBranchingController :
    MonoBehaviour
{
    [Header("Сюжет")]

    [SerializeField]
    private StoryGraph storyGraph;

    [SerializeField]
    private DocumentCatalog documentCatalog;

    private string pendingNextDocumentId =
        string.Empty;

    private bool hasResolvedRoute;

    private int completedPlayableDocumentCount;

    private int lastResolvedMaximumScore;

    public bool HasResolvedRoute =>
        hasResolvedRoute;

    public string PendingNextDocumentId =>
        pendingNextDocumentId;

    public int CampaignPlayableDocumentCount =>
        storyGraph != null
            ? storyGraph.CampaignPlayableDocumentCount
            : 0;

    public int CompletedPlayableDocumentCount =>
        completedPlayableDocumentCount;

    public int LastResolvedMaximumScore =>
        lastResolvedMaximumScore;

    private void Awake()
    {
        RestoreCompletedDocumentCount();
        ClearResolvedRoute();
    }

    public StoryRouteResult ResolveAfterDocument(
        DocumentData currentDocument,
        bool documentPassed,
        int documentScore,
        int totalScore,
        int maximumDocumentScore,
        string linearFallbackNextDocumentId,
        IReadOnlyList<DocumentWord> currentWords)
    {
        int completedIncludingCurrent =
            completedPlayableDocumentCount;

        if (currentDocument != null &&
            !currentDocument.IsTutorial)
        {
            completedIncludingCurrent++;
        }

        Dictionary<string, StoryFragmentState>
            fragmentStates =
                BuildFragmentStates(
                    currentWords
                );

        StoryRouteContext context =
            new StoryRouteContext(
                currentDocument != null
                    ? currentDocument.DocumentId
                    : string.Empty,
                documentPassed,
                documentScore,
                totalScore,
                completedIncludingCurrent,
                maximumDocumentScore,
                fragmentStates
            );

        StoryRouteResult result =
            StoryRouter.Resolve(
                storyGraph,
                context,
                linearFallbackNextDocumentId
            );

        if (!result.EndsCampaign &&
            documentCatalog != null &&
            documentCatalog.FindById(
                result.NextDocumentId) == null)
        {
            Debug.LogError(
                $"StoryBranchingController: StoryGraph ведёт к " +
                $"неизвестному Document Id: {result.NextDocumentId}. " +
                $"Использую линейный fallback."
            );

            result =
                new StoryRouteResult(
                    linearFallbackNextDocumentId
                );
        }

        completedPlayableDocumentCount =
            completedIncludingCurrent;

        lastResolvedMaximumScore =
            completedPlayableDocumentCount *
            Mathf.Max(
                1,
                maximumDocumentScore
            );

        pendingNextDocumentId =
            result.NextDocumentId;

        hasResolvedRoute =
            true;

        Debug.Log(
            $"Story route: " +
            $"{context.CurrentDocumentId} -> " +
            $"{(result.EndsCampaign ? "CAMPAIGN_END" : result.NextDocumentId)}. " +
            $"Score: {context.CampaignPercent:0.##}%."
        );

        LogFragmentStates(
            fragmentStates
        );

        return result;
    }

    public void ClearResolvedRoute()
    {
        pendingNextDocumentId =
            string.Empty;

        hasResolvedRoute =
            false;

        lastResolvedMaximumScore =
            0;
    }

    public void ResetForNewCampaign()
    {
        completedPlayableDocumentCount =
            0;

        ClearResolvedRoute();
    }

    public int GetCurrentPlayableDocumentNumber(
        bool currentDocumentFinished)
    {
        if (currentDocumentFinished)
        {
            return Mathf.Max(
                1,
                completedPlayableDocumentCount
            );
        }

        return Mathf.Max(
            1,
            completedPlayableDocumentCount + 1
        );
    }

    private Dictionary<string, StoryFragmentState>
        BuildFragmentStates(
            IReadOnlyList<DocumentWord> currentWords)
    {
        Dictionary<string, StoryFragmentState> states =
            new Dictionary<string, StoryFragmentState>();

        if (currentWords == null)
        {
            return states;
        }

        Dictionary<string, bool> allRedacted =
            new Dictionary<string, bool>();

        foreach (
            DocumentWord word
            in currentWords)
        {
            if (word == null ||
                string.IsNullOrWhiteSpace(
                    word.storyFragmentId))
            {
                continue;
            }

            string fragmentId =
                word.storyFragmentId.Trim();

            if (!allRedacted.ContainsKey(
                    fragmentId))
            {
                allRedacted[
                    fragmentId
                ] = true;
            }

            if (!word.isRedacted)
            {
                allRedacted[
                    fragmentId
                ] = false;
            }
        }

        foreach (
            KeyValuePair<string, bool> item
            in allRedacted)
        {
            states[item.Key] =
                item.Value
                    ? StoryFragmentState.Redacted
                    : StoryFragmentState.Exposed;
        }

        return states;
    }

    private void LogFragmentStates(
        IReadOnlyDictionary<
            string,
            StoryFragmentState> fragmentStates)
    {
        if (fragmentStates == null ||
            fragmentStates.Count == 0)
        {
            return;
        }

        foreach (
            KeyValuePair<
                string,
                StoryFragmentState> item
            in fragmentStates)
        {
            Debug.Log(
                $"Story fragment: " +
                $"{item.Key} = {item.Value}."
            );
        }
    }

    private void RestoreCompletedDocumentCount()
    {
        completedPlayableDocumentCount =
            0;

        if (documentCatalog == null)
        {
            return;
        }

        CampaignSaveData campaign =
            SaveManager.LoadCampaign();

        if (campaign == null ||
            campaign.checkpoints == null)
        {
            return;
        }

        CheckpointSaveData activeCheckpoint =
            FindActiveCheckpoint(
                campaign
            );

        if (activeCheckpoint == null ||
            activeCheckpoint.completedDocuments == null)
        {
            return;
        }

        foreach (
            DocumentResultSaveData result
            in activeCheckpoint.completedDocuments)
        {
            if (result == null ||
                string.IsNullOrWhiteSpace(
                    result.documentId))
            {
                continue;
            }

            DocumentData document =
                documentCatalog.FindById(
                    result.documentId
                );

            if (document != null &&
                !document.IsTutorial)
            {
                completedPlayableDocumentCount++;
            }
        }
    }

    private CheckpointSaveData FindActiveCheckpoint(
        CampaignSaveData campaign)
    {
        foreach (
            CheckpointSaveData checkpoint
            in campaign.checkpoints)
        {
            if (checkpoint != null &&
                checkpoint.checkpointId ==
                campaign.activeCheckpointId)
            {
                return checkpoint;
            }
        }

        return null;
    }
}
