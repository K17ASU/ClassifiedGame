public enum GameSessionStartMode
{
    NewGame = 0,
    Continue = 1
}

/// <summary>
/// Передаёт из MainMenu в GameScene способ запуска сессии.
/// Значение живёт только до загрузки игровой сцены.
/// </summary>
public static class GameSessionRequest
{
    private static GameSessionStartMode nextStartMode =
        GameSessionStartMode.NewGame;

    public static void RequestNewGame()
    {
        nextStartMode =
            GameSessionStartMode.NewGame;
    }

    public static void RequestContinue()
    {
        nextStartMode =
            GameSessionStartMode.Continue;
    }

    public static GameSessionStartMode Consume()
    {
        GameSessionStartMode result =
            nextStartMode;

        nextStartMode =
            GameSessionStartMode.NewGame;

        return result;
    }
}
