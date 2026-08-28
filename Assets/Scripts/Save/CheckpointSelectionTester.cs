using UnityEngine;

/// <summary>
/// Временный компонент для проверки выбора старых checkpoint'ов
/// до создания полноценного UI истории сохранений.
/// После проверки его можно удалить.
/// </summary>
public sealed class CheckpointSelectionTester : MonoBehaviour
{
    [SerializeField]
    [Min(0)]
    private int checkpointIndex;

    [ContextMenu("TEST Print Checkpoints")]
    private void PrintCheckpoints()
    {
        CampaignSaveData campaign =
            SaveManager.LoadCampaign();

        if (campaign == null ||
            campaign.checkpoints == null ||
            campaign.checkpoints.Count == 0)
        {
            Debug.Log(
                "CheckpointSelectionTester: checkpoint'ов нет."
            );
            return;
        }

        Debug.Log(
            $"CheckpointSelectionTester: всего checkpoint'ов: " +
            $"{campaign.checkpoints.Count}, " +
            $"activeCheckpointIndex: " +
            $"{campaign.activeCheckpointIndex}."
        );

        for (int i = 0;
             i < campaign.checkpoints.Count;
             i++)
        {
            CheckpointSaveData checkpoint =
                campaign.checkpoints[i];

            if (checkpoint == null)
            {
                Debug.Log(
                    $"Checkpoint #{i}: NULL"
                );
                continue;
            }

            Debug.Log(
                $"Checkpoint #{i}: " +
                $"document={checkpoint.currentDocumentId}, " +
                $"score={checkpoint.totalScore}, " +
                $"completed=" +
                $"{checkpoint.completedDocuments?.Count ?? 0}"
            );
        }
    }

    [ContextMenu("TEST Select Checkpoint")]
    private void SelectCheckpoint()
    {
        bool success =
            CampaignProgress.SelectCheckpoint(
                checkpointIndex
            );

        if (success)
        {
            Debug.Log(
                $"CheckpointSelectionTester: выбран " +
                $"checkpoint #{checkpointIndex}. " +
                $"Теперь нажми Continue."
            );
        }
        else
        {
            Debug.LogError(
                $"CheckpointSelectionTester: не удалось выбрать " +
                $"checkpoint #{checkpointIndex}."
            );
        }
    }
}
