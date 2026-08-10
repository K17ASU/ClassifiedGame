using System.Text;
using UnityEngine;

/// <summary>
/// Хранит имя игрока между сценами и запусками игры.
/// </summary>
public static class PlayerProfile
{
    private const string PlayerNameKey = "classified_player_name";
    private const int MaximumNameLength = 24;

    public static string PlayerName
    {
        get
        {
            return PlayerPrefs.GetString(PlayerNameKey, string.Empty);
        }
    }

    public static bool HasPlayerName
    {
        get
        {
            return !string.IsNullOrWhiteSpace(PlayerName);
        }
    }

    public static void SetPlayerName(string value)
    {
        string sanitizedName = SanitizePlayerName(value);

        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            PlayerPrefs.DeleteKey(PlayerNameKey);
            PlayerPrefs.Save();
            return;
        }

        PlayerPrefs.SetString(PlayerNameKey, sanitizedName);
        PlayerPrefs.Save();
    }

    public static string SanitizePlayerName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        StringBuilder result = new StringBuilder();
        bool previousCharacterWasSpace = false;

        foreach (char character in value.Trim())
        {
            bool isAllowed =
                char.IsLetter(character) ||
                character == ' ' ||
                character == '-' ||
                character == '\'';

            if (!isAllowed)
            {
                continue;
            }

            if (character == ' ')
            {
                if (previousCharacterWasSpace)
                {
                    continue;
                }

                previousCharacterWasSpace = true;
            }
            else
            {
                previousCharacterWasSpace = false;
            }

            result.Append(character);

            if (result.Length >= MaximumNameLength)
            {
                break;
            }
        }

        return result.ToString().Trim();
    }

    public static void ClearPlayerName()
    {
        PlayerPrefs.DeleteKey(PlayerNameKey);
        PlayerPrefs.Save();
    }
}
