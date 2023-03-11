using System.Collections;
using Mirror;
using UnityEngine;

public class Mine : NetworkBehaviour
{
    [SyncVar(hook = nameof(HandleColorChanged))]
    Color color = Color.red;


    private MyPlayerNetwork owner;

    private void Start()
    {
        // StartCoroutine(DestroyMineAfter5Sec());
    }

    // private IEnumerator DestroyMineAfter5Sec()
    // {
    //     yield return new WaitForSeconds(5);
    //     NetworkServer.Destroy(gameObject);
    // }

    #region Server

    [Server]
    public void ChangeMeshColor(Color color)
    {
        this.color = color;

        // Change color for the server
        if (TryGetComponent<MeshRenderer>(out var meshRenderer))
        {
            meshRenderer.material.color = color;
        }
    }

    [Server]
    public void setOwner(MyPlayerNetwork owner)
    {
        this.owner = owner;
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent<MyPlayerNetwork>(out var player))
        {
            if (player != owner)
            {
                player.TRPCCollideMine(player.connectionToClient, "You died");
                NetworkServer.Destroy(gameObject);
            }
        }
    }

    #endregion

    #region Client

    [Client]
    private void HandleColorChanged(Color oldColor, Color newColor)
    {
        if (TryGetComponent<MeshRenderer>(out var meshRenderer))
        {
            meshRenderer.material.color = newColor;
        }
    }

    #endregion
}