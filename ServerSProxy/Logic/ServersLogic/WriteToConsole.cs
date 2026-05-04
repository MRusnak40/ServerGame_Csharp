using ServerSProxy.Logic.PlayerCode;

internal static class WriteToConsole
{
    // Pokud chcete per-player lock, použijte writer instance jako zámek.
    public static async Task BroadcastAll(string message, List<Player> activePlayers)
    {
        Player[] snapshot;
        lock (activePlayers) snapshot = activePlayers.ToArray();

        foreach (var player in snapshot)
        {
            await SafeWrite(player, message);
        }
    }

    public static Task TextToPlayer(Player player, string message) => SafeWrite(player, message);

    public static Task TextToPlayerOneLine(Player player, string message) => SafeWrite(player, message);

    public static async Task<string> TakeInput(Player player)
    {
        try
        {
            if (player?.Reader == null) return "";
            return await player.Reader.ReadLineAsync() ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static async Task SafeWrite(Player player, string message)
    {
        if (player == null) return;
        var writer = player.Writer;
        if (writer == null) return;

        try
        {
            // lock na writer instanci, aby se nepřepisovaly zprávy z více vláken
            lock (writer)
            {
                writer.WriteLine(message);
            }
            await writer.FlushAsync();
        }
        catch
        {
            // ticho: klient je pravděpodobně odpojený; volající by měl řešit cleanup
        }
    }
}
