using Mirror;

public class MyNetworkManager : NetworkManager
{
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
        if (conn.identity.TryGetComponent<MyPlayerNetwork>(out var player))
        {
            player.ChangeColor();
            var playerPseudo = $"Player {NetworkServer.connections.Count}";
            player.SetPseudo(playerPseudo);
        }
    }
}