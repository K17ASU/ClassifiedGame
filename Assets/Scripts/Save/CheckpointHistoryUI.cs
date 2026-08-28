using System;
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
    private RectTransform checkpointContent;

    [SerializeField]
    private Button checkpointButtonTemplate;

    [Header("Delete Confirmation")]

    [SerializeField]
    private GameObject deleteConfirmPanel;

    [SerializeField]
    private Button confirmDeleteButton;

    [SerializeField]
    private Button cancelDeleteButton;

    [Header("Data")]

    [SerializeField]
    private DocumentCatalog documentCatalog;

    [SerializeField]
    private MainMenuController mainMenuController;

    [Header("Tree Layout")]

    [SerializeField]
    private float nodeWidth = 220f;

    [SerializeField]
    private float nodeHeight = 72f;

    [SerializeField]
    private float horizontalSpacing = 260f;

    [SerializeField]
    private float verticalSpacing = 110f;

    [SerializeField]
    private float leftPadding = 140f;

    [SerializeField]
    private float rightPadding = 140f;

    [SerializeField]
    private float topPadding = 80f;

    [SerializeField]
    private float bottomPadding = 80f;

    [Header("Normal Branch")]

    [SerializeField]
    private Color normalNodeColor =
        new Color32(255, 255, 255, 255);

    [SerializeField]
    private Color normalConnectorColor =
        new Color32(95, 115, 95, 255);

    [Header("Active Branch")]

    [SerializeField]
    private Color activeNodeColor =
        new Color32(175, 205, 175, 255);

    [SerializeField]
    private Color activeConnectorColor =
        new Color32(150, 190, 150, 255);

    [SerializeField]
    private float connectorThickness = 2f;

    [SerializeField]
    private float activeConnectorThickness = 4f;

    private readonly List<GameObject> spawnedObjects =
        new List<GameObject>();

    private readonly Dictionary<int, CheckpointSaveData>
        checkpointsById =
            new Dictionary<int, CheckpointSaveData>();

    private readonly Dictionary<int, CheckpointSaveData>
        allCheckpointsById =
            new Dictionary<int, CheckpointSaveData>();

    private readonly Dictionary<int, List<CheckpointSaveData>>
        childrenByParentId =
            new Dictionary<int, List<CheckpointSaveData>>();

    private readonly Dictionary<int, Vector2>
        nodePositions =
            new Dictionary<int, Vector2>();

    private readonly HashSet<int> layoutVisited =
        new HashSet<int>();

    private readonly HashSet<int> activePathIds =
        new HashSet<int>();

    private int nextLeafRow;
    private int maxDepth;

    private int pendingDeleteCheckpointId =
        -1;

    private void Start()
    {
        if (historyPanel != null)
        {
            historyPanel.SetActive(false);
        }

        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(false);
        }

        if (confirmDeleteButton != null)
        {
            confirmDeleteButton.onClick.AddListener(
                ConfirmDeleteBranch
            );
        }

        if (cancelDeleteButton != null)
        {
            cancelDeleteButton.onClick.AddListener(
                CancelDeleteBranch
            );
        }

        RefreshOpenButton();

        LocalizationSettings.SelectedLocaleChanged +=
            OnSelectedLocaleChanged;
    }

    private void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -=
            OnSelectedLocaleChanged;

        if (confirmDeleteButton != null)
        {
            confirmDeleteButton.onClick.RemoveListener(
                ConfirmDeleteBranch
            );
        }

        if (cancelDeleteButton != null)
        {
            cancelDeleteButton.onClick.RemoveListener(
                CancelDeleteBranch
            );
        }
    }

    public void OpenHistory()
    {
        if (historyPanel == null)
        {
            return;
        }

        historyPanel.SetActive(true);
        RebuildCheckpointTree();
    }

    public void CloseHistory()
    {
        CancelDeleteBranch();

        if (historyPanel != null)
        {
            historyPanel.SetActive(false);
        }
    }

    private void RequestDeleteBranch(
        int checkpointId)
    {
        if (checkpointId <= 0)
        {
            return;
        }

        pendingDeleteCheckpointId =
            checkpointId;

        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "CheckpointHistoryUI: Delete Confirm Panel не назначен."
            );
        }
    }

    private void ConfirmDeleteBranch()
    {
        if (pendingDeleteCheckpointId < 0)
        {
            return;
        }

        int checkpointId =
            pendingDeleteCheckpointId;

        pendingDeleteCheckpointId =
            -1;

        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(false);
        }

        if (CampaignProgress.DeleteBranch(
                checkpointId,
                out int deletedCount))
        {
            Debug.Log(
                $"CheckpointHistoryUI: удалена ветка. " +
                $"Checkpoint'ов удалено: {deletedCount}."
            );

            RefreshOpenButton();
            RebuildCheckpointTree();
        }
    }

    private void CancelDeleteBranch()
    {
        pendingDeleteCheckpointId =
            -1;

        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(false);
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
                if (IsVisibleCheckpoint(checkpoint))
                {
                    loadableCheckpointCount++;
                }
            }
        }

        openHistoryButton.interactable =
            loadableCheckpointCount > 1;
    }

    private void RebuildCheckpointTree()
    {
        ClearSpawnedObjects();

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
            campaign.checkpoints == null ||
            campaign.checkpoints.Count == 0)
        {
            return;
        }

        BuildCheckpointGraph(campaign);
        BuildActivePath(campaign);

        List<CheckpointSaveData> roots =
            FindRoots();

        if (roots.Count == 0)
        {
            Debug.LogError(
                "CheckpointHistoryUI: не найден корневой checkpoint."
            );
            return;
        }

        roots.Sort(
            (a, b) =>
                a.checkpointId.CompareTo(
                    b.checkpointId
                )
        );

        nodePositions.Clear();
        layoutVisited.Clear();
        nextLeafRow = 0;
        maxDepth = 0;

        foreach (
            CheckpointSaveData root
            in roots)
        {
            LayoutNode(
                root,
                0
            );

            nextLeafRow++;
        }

        CreateConnectors();
        CreateNodes();
        ResizeContent();
    }

    private void BuildCheckpointGraph(
        CampaignSaveData campaign)
    {
        checkpointsById.Clear();
        allCheckpointsById.Clear();
        childrenByParentId.Clear();

        foreach (
            CheckpointSaveData checkpoint
            in campaign.checkpoints)
        {
            if (checkpoint == null)
            {
                continue;
            }

            allCheckpointsById[
                checkpoint.checkpointId
            ] = checkpoint;

            if (IsVisibleCheckpoint(checkpoint))
            {
                checkpointsById[
                    checkpoint.checkpointId
                ] = checkpoint;
            }
        }

        foreach (
            CheckpointSaveData checkpoint
            in checkpointsById.Values)
        {
            if (!childrenByParentId.TryGetValue(
                    checkpoint.parentCheckpointId,
                    out List<CheckpointSaveData> children))
            {
                children =
                    new List<CheckpointSaveData>();

                childrenByParentId[
                    checkpoint.parentCheckpointId
                ] = children;
            }

            children.Add(checkpoint);
        }

        foreach (
            List<CheckpointSaveData> children
            in childrenByParentId.Values)
        {
            children.Sort(
                (a, b) =>
                    a.checkpointId.CompareTo(
                        b.checkpointId
                    )
            );
        }
    }

    private void BuildActivePath(
        CampaignSaveData campaign)
    {
        activePathIds.Clear();

        int currentId =
            campaign.activeCheckpointId;

        int safetyCounter = 0;

        while (
            allCheckpointsById.TryGetValue(
                currentId,
                out CheckpointSaveData checkpoint
            ))
        {
            activePathIds.Add(
                checkpoint.checkpointId
            );

            if (checkpoint.parentCheckpointId < 0)
            {
                break;
            }

            currentId =
                checkpoint.parentCheckpointId;

            safetyCounter++;

            if (safetyCounter >
                allCheckpointsById.Count)
            {
                Debug.LogError(
                    "CheckpointHistoryUI: обнаружен цикл в дереве checkpoint'ов."
                );
                break;
            }
        }
    }

    private List<CheckpointSaveData> FindRoots()
    {
        List<CheckpointSaveData> roots =
            new List<CheckpointSaveData>();

        foreach (
            CheckpointSaveData checkpoint
            in checkpointsById.Values)
        {
            if (checkpoint.parentCheckpointId < 0 ||
                !checkpointsById.ContainsKey(
                    checkpoint.parentCheckpointId
                ))
            {
                roots.Add(checkpoint);
            }
        }

        return roots;
    }

    private float LayoutNode(
        CheckpointSaveData checkpoint,
        int depth)
    {
        if (checkpoint == null)
        {
            return nextLeafRow;
        }

        if (!layoutVisited.Add(
                checkpoint.checkpointId))
        {
            return GetExistingRow(
                checkpoint.checkpointId
            );
        }

        maxDepth =
            Mathf.Max(
                maxDepth,
                depth
            );

        List<CheckpointSaveData> children =
            GetVisibleChildren(
                checkpoint.checkpointId
            );

        float row;

        if (children.Count == 0)
        {
            row =
                nextLeafRow;

            nextLeafRow++;
        }
        else
        {
            float totalChildRows = 0f;

            foreach (
                CheckpointSaveData child
                in children)
            {
                totalChildRows +=
                    LayoutNode(
                        child,
                        depth + 1
                    );
            }

            row =
                totalChildRows /
                children.Count;
        }

        nodePositions[
            checkpoint.checkpointId
        ] =
            new Vector2(
                leftPadding +
                depth *
                horizontalSpacing,

                -(
                    topPadding +
                    row *
                    verticalSpacing
                )
            );

        return row;
    }

    private float GetExistingRow(
        int checkpointId)
    {
        if (!nodePositions.TryGetValue(
                checkpointId,
                out Vector2 position))
        {
            return nextLeafRow;
        }

        return
            (
                -position.y -
                topPadding
            ) /
            verticalSpacing;
    }

    private List<CheckpointSaveData>
        GetVisibleChildren(
            int parentCheckpointId)
    {
        if (!childrenByParentId.TryGetValue(
                parentCheckpointId,
                out List<CheckpointSaveData> children))
        {
            return new List<CheckpointSaveData>();
        }

        return children;
    }

    private void CreateNodes()
    {
        List<int> ids =
            new List<int>(
                nodePositions.Keys
            );

        ids.Sort();

        foreach (int checkpointId in ids)
        {
            if (!checkpointsById.TryGetValue(
                    checkpointId,
                    out CheckpointSaveData checkpoint))
            {
                continue;
            }

            Button button =
                Instantiate(
                    checkpointButtonTemplate,
                    checkpointContent
                );

            button.gameObject.SetActive(true);

            RectTransform rect =
                button.transform as RectTransform;

            if (rect != null)
            {
                rect.anchorMin =
                    new Vector2(0f, 1f);

                rect.anchorMax =
                    new Vector2(0f, 1f);

                rect.pivot =
                    new Vector2(0.5f, 0.5f);

                rect.sizeDelta =
                    new Vector2(
                        nodeWidth,
                        nodeHeight
                    );

                rect.anchoredPosition =
                    nodePositions[
                        checkpointId
                    ];
            }

            ApplyNodeColors(
                button,
                activePathIds.Contains(
                    checkpointId
                )
            );

            CheckpointNodeView nodeView =
                button.GetComponent<
                    CheckpointNodeView
                >();

            TMP_Text label =
                nodeView != null
                    ? nodeView.Label
                    : button.GetComponentInChildren<
                        TMP_Text
                    >(true);

            if (label != null)
            {
                label.text =
                    GetCheckpointLabel(
                        checkpoint
                    );
            }

            button.onClick.RemoveAllListeners();

            int capturedCheckpointId =
                checkpointId;

            button.onClick.AddListener(
                () =>
                    mainMenuController.LoadCheckpoint(
                        capturedCheckpointId
                    )
            );

            bool campaignEnd =
    string.IsNullOrWhiteSpace(
        checkpoint.currentDocumentId
    );

            button.interactable =
                !campaignEnd;

            if (!campaignEnd)
            {
                button.onClick.AddListener(
                    () =>
                        mainMenuController.LoadCheckpoint(
                            capturedCheckpointId
                        )
                );
            }

            if (nodeView != null &&
                nodeView.DeleteButton != null)
            {
                Button deleteButton =
                    nodeView.DeleteButton;

                deleteButton.gameObject.SetActive(
                    checkpoint.parentCheckpointId >= 0
                );

                deleteButton.onClick.RemoveAllListeners();

                if (checkpoint.parentCheckpointId >= 0)
                {
                    deleteButton.onClick.AddListener(
                        () =>
                            RequestDeleteBranch(
                                capturedCheckpointId
                            )
                    );
                }
            }

            spawnedObjects.Add(
                button.gameObject
            );
        }
    }

    private void ApplyNodeColors(
        Button button,
        bool isActivePath)
    {
        if (button == null)
        {
            return;
        }

        Color baseColor =
            isActivePath
                ? activeNodeColor
                : normalNodeColor;

        ColorBlock colors =
            button.colors;

        colors.normalColor =
            baseColor;

        colors.selectedColor =
            baseColor;

        colors.highlightedColor =
            Color.Lerp(
                baseColor,
                Color.white,
                0.15f
            );

        colors.pressedColor =
            Color.Lerp(
                baseColor,
                Color.black,
                0.15f
            );

        button.colors =
            colors;
    }

    private void CreateConnectors()
    {
        foreach (
            CheckpointSaveData checkpoint
            in checkpointsById.Values)
        {
            if (checkpoint.parentCheckpointId < 0)
            {
                continue;
            }

            if (!nodePositions.TryGetValue(
                    checkpoint.parentCheckpointId,
                    out Vector2 parentPosition))
            {
                continue;
            }

            if (!nodePositions.TryGetValue(
                    checkpoint.checkpointId,
                    out Vector2 childPosition))
            {
                continue;
            }

            bool isActiveConnector =
                activePathIds.Contains(
                    checkpoint.parentCheckpointId
                ) &&
                activePathIds.Contains(
                    checkpoint.checkpointId
                );

            CreateConnector(
                parentPosition,
                childPosition,
                isActiveConnector
            );
        }
    }

    private void CreateConnector(
        Vector2 from,
        Vector2 to,
        bool isActive)
    {
        GameObject connector =
            new GameObject(
                "CheckpointConnector",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

        connector.transform.SetParent(
            checkpointContent,
            false
        );

        RectTransform rect =
            connector.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(0f, 1f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        Vector2 delta =
            to - from;

        rect.anchoredPosition =
            (from + to) * 0.5f;

        rect.sizeDelta =
            new Vector2(
                delta.magnitude,
                isActive
                    ? activeConnectorThickness
                    : connectorThickness
            );

        rect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(
                    delta.y,
                    delta.x
                ) *
                Mathf.Rad2Deg
            );

        Image image =
            connector.GetComponent<Image>();

        image.color =
            isActive
                ? activeConnectorColor
                : normalConnectorColor;

        image.raycastTarget =
            false;

        connector.transform.SetAsFirstSibling();

        spawnedObjects.Add(
            connector
        );
    }

    private void ResizeContent()
    {
        int rowCount =
            Mathf.Max(
                1,
                nextLeafRow
            );

        float width =
            leftPadding +
            maxDepth *
            horizontalSpacing +
            nodeWidth * 0.5f +
            rightPadding;

        float height =
            topPadding +
            Mathf.Max(
                0,
                rowCount - 1
            ) *
            verticalSpacing +
            nodeHeight * 0.5f +
            bottomPadding;

        checkpointContent.anchorMin =
            new Vector2(0f, 1f);

        checkpointContent.anchorMax =
            new Vector2(0f, 1f);

        checkpointContent.pivot =
            new Vector2(0f, 1f);

        checkpointContent.sizeDelta =
            new Vector2(
                width,
                height
            );

        checkpointContent.anchoredPosition =
            Vector2.zero;
    }

    private bool IsVisibleCheckpoint(
        CheckpointSaveData checkpoint)
    {
        return checkpoint != null;
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
            $"{documentLabel}\n{checkpoint.totalScore}";
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

    private void ClearSpawnedObjects()
    {
        foreach (
            GameObject spawnedObject
            in spawnedObjects)
        {
            if (spawnedObject != null)
            {
                Destroy(
                    spawnedObject
                );
            }
        }

        spawnedObjects.Clear();
    }

    private void OnSelectedLocaleChanged(
        Locale locale)
    {
        if (historyPanel != null &&
            historyPanel.activeSelf)
        {
            RebuildCheckpointTree();
        }

        RefreshOpenButton();
    }
}
