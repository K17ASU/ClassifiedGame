using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Читает и записывает файл кампании.
/// Пока не связан с игровым прогрессом.
/// </summary>
public static class SaveManager
{
    private const string SaveFileName =
        "classified_campaign.json";

    public static string SavePath =>
        Path.Combine(
            Application.persistentDataPath,
            SaveFileName
        );

    public static bool HasSave =>
        File.Exists(SavePath);

    public static bool SaveCampaign(
        CampaignSaveData campaign)
    {
        if (campaign == null)
        {
            Debug.LogError(
                "SaveManager: нельзя сохранить null CampaignSaveData."
            );
            return false;
        }

        try
        {
            string directory =
                Path.GetDirectoryName(SavePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json =
                JsonUtility.ToJson(
                    campaign,
                    true
                );

            File.WriteAllText(
                SavePath,
                json
            );

            Debug.Log(
                $"SaveManager: сохранение записано:\n{SavePath}"
            );

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"SaveManager: ошибка записи сохранения.\n{exception}"
            );

            return false;
        }
    }

    public static CampaignSaveData LoadCampaign()
    {
        if (!HasSave)
        {
            Debug.Log(
                "SaveManager: файла сохранения пока нет."
            );

            return null;
        }

        try
        {
            string json =
                File.ReadAllText(SavePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError(
                    "SaveManager: файл сохранения пуст."
                );

                return null;
            }

            CampaignSaveData campaign =
                JsonUtility.FromJson<CampaignSaveData>(
                    json
                );

            if (campaign == null)
            {
                Debug.LogError(
                    "SaveManager: не удалось прочитать CampaignSaveData."
                );

                return null;
            }

            if (campaign.checkpoints == null)
            {
                campaign.checkpoints =
                    new List<CheckpointSaveData>();
            }

            Debug.Log(
                $"SaveManager: сохранение загружено:\n{SavePath}"
            );

            return campaign;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"SaveManager: ошибка чтения сохранения.\n{exception}"
            );

            return null;
        }
    }

    public static bool DeleteSave()
    {
        if (!HasSave)
        {
            return true;
        }

        try
        {
            File.Delete(SavePath);

            Debug.Log(
                "SaveManager: тестовое сохранение удалено."
            );

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"SaveManager: ошибка удаления сохранения.\n{exception}"
            );

            return false;
        }
    }
}
