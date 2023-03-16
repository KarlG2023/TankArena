using System.Collections;
using Mirror;
using UnityEngine;

public class PowerUp : NetworkBehaviour
{
    [SerializeField] private string type = "Health";
    [SerializeField] private float powerUpDuration = 10f;

    private void Start()
    {
        
        if (type == "Health")
        {
            Transform healthPrefab = transform.Find("gem_h");
            healthPrefab.gameObject.SetActive(true);
            SetParticleColors(transform, Color.green);
            StartCoroutine(RotatePrefab(healthPrefab));
        }
        else if (type == "Buff")
        {
            Transform buffPrefab = transform.Find("gem_b");
            buffPrefab.gameObject.SetActive(true);
            SetParticleColors(transform, Color.yellow);
            StartCoroutine(RotatePrefab(buffPrefab));
        }
        else if (type == "Mastodont")
        {
            Transform mastodontPrefab = transform.Find("gem_m");
            mastodontPrefab.gameObject.SetActive(true);
            SetParticleColors(transform, Color.blue);
            StartCoroutine(RotatePrefab(mastodontPrefab));
        }

        else if (type == "Mine")
        {
            Transform minePrefab = transform.Find("gem_m2");
            minePrefab.gameObject.SetActive(true);
            SetParticleColors(transform, new Color(0.5f, 0.0f, 0.5f));
            StartCoroutine(RotatePrefab(minePrefab));
        }
    }

    private void SetParticleColors(Transform prefab, Color color)
    {
        foreach (Transform child in prefab)
        {
            ParticleSystem ps = child.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = color;
                foreach (Transform child2 in child)
                {
                    ParticleSystem ps2 = child2.GetComponent<ParticleSystem>();
                    var main2 = ps2.main;
                    main2.startColor = color;
                }
            }
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
                player.SetHealth(20f);
                player.TRPCCollidePowerUp(player.connectionToClient, "Health", powerUpDuration);
                NetworkServer.Destroy(gameObject);
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
    }

    #endregion
}
