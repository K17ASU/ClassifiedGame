using System;

/// <summary>
/// Подставляет динамические значения в тексты документов.
/// Используйте маркер {PLAYER_NAME}.
/// </summary>
public static class PlayerTextResolver
{
    public const string PlayerNameToken = "{PLAYER_NAME}";

    public static string Resolve(string sourceText)
    {
        if (string.IsNullOrEmpty(sourceText))
        {
            return sourceText;
        }

        string playerName = PlayerProfile.PlayerName;

        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = "Агент";
        }

        return sourceText.Replace(
            PlayerNameToken,
            playerName,
            StringComparison.Ordinal
        );
    }
}
