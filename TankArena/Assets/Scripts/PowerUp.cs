using System.Collections;
using Mirror;
using UnityEngine;

public class PowerUp : NetworkBehaviour
{
    private MyPlayerNetwork owner;
    [SerializeField] private string type = "Health";

    private void Start()
    {

    }

    #region Server

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    { 
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (type == "Health") {
            if (other.TryGetComponent<MyPlayerNetwork>(out var player))
            {
                player.TRPCCollidePowerUp(player.connectionToClient, "You have recoverd a Health power up", "Health", +25);
                NetworkServer.Destroy(gameObject);
            }
        }

        if (type == "RapidFire") {
            if (other.TryGetComponent<MyPlayerNetwork>(out var player))
            {
                player.TRPCCollidePowerUp(player.connectionToClient, "You have recoverd a rapidfire power up", "ShootCouldown", -1.75f);
                NetworkServer.Destroy(gameObject);
            }
        }
    }

    #endregion
}
