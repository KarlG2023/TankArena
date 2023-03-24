using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Explosion : NetworkBehaviour
{
    [SerializeField] private float explosionForce = 1000f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float destroyDelay = 1f;
    [SerializeField] private GameObject[] explosionParts;
    [SerializeField] private GameObject explosionPrefab = null;

    private void Start()
    {
        Explode();
    }

    private void Explode()
    {
        foreach (GameObject part in explosionParts)
        {
            StartCoroutine(ExplosionEffect(part));
            Rigidbody rb = part.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }

        StartCoroutine(DestroyParts());
    }

    private IEnumerator ExplosionEffect(GameObject tankPart)
    {
        yield return new WaitForSeconds(2);
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        var explosion = Instantiate(explosionPrefab, tankPart.transform.position, Quaternion.identity);

        ParticleSystem blastPS = explosion.transform.Find("Blast").GetComponent<ParticleSystem>();
        blastPS.Play();

        ParticleSystem smokePS = explosion.transform.Find("Smoke").GetComponent<ParticleSystem>();
        smokePS.Play();

        ParticleSystem sparklePS = explosion.transform.Find("Sparkle").GetComponent<ParticleSystem>();
        sparklePS.Play();
        yield return new WaitForSeconds(2);
    }

    private IEnumerator DestroyParts()
    {
        yield return new WaitForSeconds(destroyDelay);
        foreach (GameObject part in explosionParts)
        {
            Destroy(part);
        }
    }
}