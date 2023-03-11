using System.Collections;
using Mirror;
using UnityEngine;

public class Missile : NetworkBehaviour
{
    [SyncVar(hook = nameof(HandleColorChanged))]
    Color color = Color.red;

    [SerializeField] private GameObject explosionPrefab = null;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float growthTime = 1.0f;
    private MyPlayerNetwork owner;
    private bool hasCollided = false;

    private void Start()
    {
        StartCoroutine(GrowMissile());
        StartCoroutine(DestroyMissile(10));
    }

    private IEnumerator GrowMissile()
    {
        float elapsedTime = 0;
        Vector3 startingScale = new Vector3(0.0f, 0.0f, 0f);
        Vector3 targetScale = new Vector3(2.5f, 2.5f, 2f);
        while (elapsedTime < growthTime)
        {
            transform.localScale = Vector3.Lerp(startingScale, targetScale, (elapsedTime / growthTime));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localScale = targetScale;
    }

    private IEnumerator DestroyMissile(int delay)
    {
        yield return new WaitForSeconds(delay);

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        var explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        NetworkServer.Spawn(explosion);

        ParticleSystem blastPS = explosion.transform.Find("Blast").GetComponent<ParticleSystem>();
        blastPS.Play();

        ParticleSystem smokePS = explosion.transform.Find("Smoke").GetComponent<ParticleSystem>();
        smokePS.Play();

        ParticleSystem sparklePS = explosion.transform.Find("Sparkle").GetComponent<ParticleSystem>();
        sparklePS.Play();
    

        yield return new WaitForSeconds(Mathf.Max(blastPS.main.duration, smokePS.main.duration, sparklePS.main.duration));
        NetworkServer.Destroy(explosion);
        NetworkServer.Destroy(gameObject);
    }

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
        if (hasCollided)
            return;
        
        if (!other.CompareTag("Player"))
        {
            hasCollided = true;
            StartCoroutine(DestroyMissile(0));
            // NetworkServer.Destroy(gameObject);
            return;
        }

        if (other.TryGetComponent<MyPlayerNetwork>(out var player))
        {
            if (player != owner)
            {
            hasCollided = true;
            StartCoroutine(DestroyMissile(0));
            player.TRPCCollideMissile(player.connectionToClient, "You died");
            // NetworkServer.Destroy(gameObject);
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

    [ClientCallback]
    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    #endregion
}
