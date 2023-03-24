using System.Collections;
using Mirror;
using UnityEngine;

public class Missile : NetworkBehaviour
{
    [SyncVar(hook = nameof(HandleColorChanged))]
    Color color = Color.red;

    [SyncVar(hook = nameof(HandleIsAlive))]
    bool isAlive = true;

    [SerializeField] private GameObject explosionPrefab = null;
    [SerializeField] private float speed = 10f;

    private MyPlayerNetwork owner;
    private aiShortRange monster;
    private bool hasCollided = false;

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

        ParticleSystem blastPS = explosion.transform.Find("Flash").GetComponent<ParticleSystem>();
        blastPS.Play();

        ParticleSystem smokePS = explosion.transform.Find("Smoke").GetComponent<ParticleSystem>();
        smokePS.Play();

        ParticleSystem sparklePS = explosion.transform.Find("ShockWave").GetComponent<ParticleSystem>();
        sparklePS.Play();
    

        yield return new WaitForSeconds(Mathf.Max(blastPS.main.duration, smokePS.main.duration, sparklePS.main.duration));
        NetworkServer.Destroy(explosion);
        yield return new WaitForSeconds(2);
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
        StartCoroutine(DestroyMissile(10));
    }

    [Server]
    public void setMonster(aiShortRange owner)
    {
        this.monster = owner;
        StartCoroutine(DestroyMissile(10));
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (hasCollided)
            return;
        if (other.CompareTag("Rock")) {
            hasCollided = true;
            this.isAlive = false;
            StartCoroutine(DestroyMissile(0));
            return;
        }
        if (other.TryGetComponent<aiShortRange>(out var mob) && other.GetType() == typeof(BoxCollider)) {
            if (monster != mob) {
            hasCollided = true;
            this.isAlive = false;
            StartCoroutine(DestroyMissile(0));
            mob.DestroyEnemy();
            return;
            }
        }
        if (other.TryGetComponent<MyPlayerNetwork>(out var player)) {
            if (player != owner) {
                hasCollided = true;
                this.isAlive = false;
                StartCoroutine(DestroyMissile(0));
                player.SetHealth(-20f);
            }
        }
    }

    #endregion

    private void HandleColorChanged(Color oldColor, Color newColor) {
        if (TryGetComponent<MeshRenderer>(out var meshRenderer)) {
            meshRenderer.material.color = newColor;
        }
    }

    private void HandleIsAlive(bool oldValue, bool newValue) {
        if (newValue == false) {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null) {
                meshRenderer.enabled = false;
            }
        }
    }

    private void Update()
    {
        if (this.isAlive == true) {
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }
}