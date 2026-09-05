using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Связывает StoryGraph с текущей кампанией.
///
/// V3:
/// - FragmentRedacted / FragmentExposed проверяют текущий документ.
/// - StoryStateRedacted / StoryStateExposed проверяют постоянное
///   состояние текущей ветки.
/// - StoryState сохраняется в каждом checkpoint.
/// </summary>
public sealed class StoryBranchingController :
    MonoBehaviour
{
    [Header("Сюжет")]

    [SerializeField]
    private StoryGraph storyGraph;

    [SerializeField]
    private DocumentCatalog documentCatalog;

    private readonly Dictionary<
        string,
        StoryFragmentState> storyState =
            new Dictionary<
                string,
                StoryFragmentState>();

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
        storyState.Clear();
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
            currentFragmentStates =
                BuildFragmentStates(
                    currentWords
                );

        // Новое решение по тому же id перезаписывает старое.
        ApplyCurrentFragmentsToStoryState(
            currentFragmentStates
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
                currentFragmentStates,
                storyState
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

        LogCurrentFragmentStates(
            currentFragmentStates
        );

        return result;
    }

    public void RestoreFromCheckpoint(
        CheckpointSaveData checkpoint)
    {
        storyState.Clear();

        completedPlayableDocumentCount =
            0;

        if (checkpoint == null)
        {
            ClearResolvedRoute();
            return;
        }

        RestoreCompletedDocumentCount(
            checkpoint
        );

        if (checkpoint.storyState != null)
        {
            foreach (
                StoryStateSaveData item
                in checkpoint.storyState)
            {
                if (item == null ||
                    string.IsNullOrWhiteSpace(
                        item.fragmentId))
                {
                    continue;
                }

                StoryFragmentState state =
                    ToStoryFragmentState(
                        item.state
                    );

                if (state ==
                    StoryFragmentState.NotFound)
                {
                    continue;
                }

                storyState[
                    item.fragmentId.Trim()
                ] = state;
            }
        }

        ClearResolvedRoute();

        Debug.Log(
            $"StoryState восстановлен из checkpoint " +
            $"#{checkpoint.checkpointId}. " +
            $"Записей: {storyState.Count}."
        );
    }

    public List<StoryStateSaveData>
        GetStoryStateForSave()
    {
        List<string> ids =
            new List<string>(
                storyState.Keys
            );

        ids.Sort(
            System.StringComparer.Ordinal
        );

        List<StoryStateSaveData> result =
            new List<StoryStateSaveData>();

        foreach (string id in ids)
        {
            StoryFragmentState state =
                storyState[id];

            if (state ==
                StoryFragmentState.NotFound)
            {
                continue;
            }

            result.Add(
                new StoryStateSaveData
                {
                    fragmentId = id,
                    state = (int)state
                }
            );
        }

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

        storyState.Clear();

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
            new Dictionary<
                string,
                StoryFragmentState>();

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

    private void ApplyCurrentFragmentsToStoryState(
        IReadOnlyDictionary<
            string,
            StoryFragmentState> currentFragmentStates)
    {
        if (currentFragmentStates == null)
        {
            return;
        }

        foreach (
            KeyValuePair<
                string,
                StoryFragmentState> item
            in currentFragmentStates)
        {
            if (item.Value ==
                StoryFragmentState.NotFound)
            {
                continue;
            }

            storyState[
                item.Key
            ] = item.Value;
        }
    }

    private void RestoreCompletedDocumentCount(
        CheckpointSaveData checkpoint)
    {
        if (checkpoint == null ||
            checkpoint.completedDocuments == null ||
            documentCatalog == null)
        {
            return;
        }

        foreach (
            DocumentResultSaveData result
            in checkpoint.completedDocuments)
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

    private StoryFragmentState
        ToStoryFragmentState(
            int savedState)
    {
        if (savedState ==
            (int)StoryFragmentState.Redacted)
        {
            return StoryFragmentState.Redacted;
        }

        if (savedState ==
            (int)StoryFragmentState.Exposed)
        {
            return StoryFragmentState.Exposed;
        }

        return StoryFragmentState.NotFound;
    }

    private void LogCurrentFragmentStates(
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
                $"{item.Key} = {item.Value}. " +
                $"Состояние сохранено в StoryState."
            );
        }
    }
}
