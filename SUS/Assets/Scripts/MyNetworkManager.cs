using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyNetworkManager : NetworkManager
{
    [SerializeField] private Map Arena;
    private List<MyPlayerNetwork> players = new List<MyPlayerNetwork>();
    private bool started = false;
    //private int ArenaSize = 4;

    [Server]
    public override void Update() {
        for (int i = 0; i < players.Count; i++) {
            if (Arena.IsPlayerFalling(players[i].PositionX, players[i].PositionY))
                players[i].SetHealth(-100f);
            if (players[i].GetHealth() <= 0) {
                players[i].Explode();
                players[i].HandleGameDeath();
                players[i].DisconnectFromArena();
                //players[i].setRemainingPlayers
                players.Remove(players[i]);
                NetworkServer.maxConnections = NetworkManager.singleton.numPlayers - 1;
            }
            if ((players.Count == 1 || NetworkServer.connections.Count == 1) && started) {
                players[i].HandleGameWin();
                players[i].StopGame();
            }
        }
    }
    public override void OnStopServer() {
        Application.Quit();
    }

    [Server]
    public override void OnServerDisconnect(NetworkConnectionToClient conn) {
        base.OnServerDisconnect(conn);
        if (conn.identity.TryGetComponent<MyPlayerNetwork>(out var player)) {
            player.SetHealth(-100);
            players.Remove(players.Find(x => x.GetPseudo() == player.GetPseudo()));
        }
        Debug.Log("Disconnected from server");
    }

    [Server]
    public override void OnServerAddPlayer(NetworkConnectionToClient conn) {
        base.OnServerAddPlayer(conn);
        if (conn.identity.TryGetComponent<MyPlayerNetwork>(out var player))
        {
            for (int AtbPoints = 10; AtbPoints > 0; AtbPoints--) {
                switch (Random.Range(1,3)) {
                    case 1:
                        player.SetHealth(20f);
                        break;
                    case 2:
                        player.SetMines(1);
                        break;
                    case 3:
                        player.AddHaste();
                        break;
                    default:
                        break;
                }
            }

            player.ChangeColor();
            var playerPseudo = $"Player {NetworkServer.connections.Count}";
            player.SetPseudo(playerPseudo);
            players.Add(player);

            if (NetworkServer.connections.Count == 4 && !started) {
                started = true;
                for (int i = 0; i < players.Count; i++) {
                    players[i].StartGame();
                }
            }
        }
    }
}