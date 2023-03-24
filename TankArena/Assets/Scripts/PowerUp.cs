using System.Collections;
using Mirror;
using UnityEngine;

public class PowerUp : NetworkBehaviour
{
    [SerializeField] private string type = "Health";
    [SerializeField] private float powerUpDuration = 10f;
    [SyncVar(hook = nameof(HandleIsAlive))]
    bool isAlive = true;

    private void Start()
    {
        
        if (type == "Health")
        {
            Transform healthPrefab = transform.Find("Health");
            healthPrefab.gameObject.SetActive(true);
            StartCoroutine(RotatePrefab(healthPrefab));
        } else if (type == "Buff")
        {
            Transform buffPrefab = transform.Find("Buff");
            buffPrefab.gameObject.SetActive(true);
            StartCoroutine(RotatePrefab(buffPrefab));
        } else if (type == "Mastodont")
        {
            Transform mastodontPrefab = transform.Find("Mastodont");
            mastodontPrefab.gameObject.SetActive(true);
            StartCoroutine(RotatePrefab(mastodontPrefab));
        } else if (type == "Mine")
        {
            Transform minePrefab = transform.Find("Mine");
            minePrefab.gameObject.SetActive(true);
            StartCoroutine(RotatePrefab(minePrefab));
        }
    }

    private IEnumerator RotatePrefab(Transform prefab)
    {
        while (true)
        {
            prefab.Rotate(Vector3.up, 30f * Time.deltaTime);
            yield return null;
        }
    }

    [Server]
    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Player") || !isAlive)
        {
            return;
        }
        if (type == "Health") {
            if (other.TryGetComponent<MyPlayerNetwork>(out var player))
            {
                player.SetHealth(20f);
                player.TRPCCollidePowerUp(player.connectionToClient, "Health", powerUpDuration);
            }
        }

        if (type == "Buff") {
            if (other.TryGetComponent<MyPlayerNetwork>(out var player))
            {
                player.SetFireRate(-1.75f, powerUpDuration);
                player.TRPCCollidePowerUp(player.connectionToClient, "Buff", powerUpDuration);
            }
        }

        if (type == "Mastodont") {
            if (other.TryGetComponent<MyPlayerNetwork>(out var player))
            {
                player.ActivateMastodont(100f, powerUpDuration);
                player.TRPCCollidePowerUp(player.connectionToClient, "Mastodont", powerUpDuration);
            }
        }

        if (type == "Mine") {
            if (other.TryGetComponent<MyPlayerNetwork>(out var player))
            {
                player.SetMines(3);
                player.TRPCCollidePowerUp(player.connectionToClient, "Mine", powerUpDuration);
            }
        }
        NetworkServer.Destroy(gameObject);
        StartCoroutine(DestroyPower());
    }

    [Client]
    private void HandleIsAlive(bool oldValue, bool newValue)
    {
        if (newValue == false) {
            MeshRenderer meshRenderer = this.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }
    }
    private IEnumerator DestroyPower()
    {
        this.isAlive = false;
        NetworkServer.Destroy(gameObject);
        yield return new WaitForSeconds(0.1f);
        NetworkServer.Destroy(gameObject);
    }
}
