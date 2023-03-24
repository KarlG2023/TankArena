using System.Collections;
using Mirror;
using UnityEngine;

public class Mine : NetworkBehaviour
{
    [SyncVar(hook = nameof(HandleColorChanged))]
    Color color = Color.red;

    [SyncVar(hook = nameof(HandleIsAlive))]
    bool isAlive = true;

    [SerializeField] private GameObject explosionPrefab = null;
    [SerializeField] private AudioSource explosion;

    private MyPlayerNetwork owner;
    private GameObject effect = null;

    private IEnumerator DestroyMine()
    {
        this.isAlive = false;

        yield return new WaitForSeconds(1f);

        NetworkServer.Destroy(effect);
        NetworkServer.Destroy(gameObject);
    }

    #region Server

    [Server]
    public void ChangeMeshColor(Color color)
    {
        this.color = color;
        if (TryGetComponent<MeshRenderer>(out var meshRenderer)) {
            meshRenderer.material.color = color;
        }
    }

    [Server]
    public void setOwner(MyPlayerNetwork owner) {
        this.owner = owner;
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent<MyPlayerNetwork>(out var player))
        {
            if (player != owner && isAlive == true)
            {
                player.SetHealth(-80f);
                effect = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                explosion.Play();
                NetworkServer.Spawn(effect);
                StartCoroutine(DestroyMine());
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

    [Client]
    private void HandleIsAlive(bool oldValue, bool newValue)
    {
        if (newValue == false) {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }
    }

    #endregion
}