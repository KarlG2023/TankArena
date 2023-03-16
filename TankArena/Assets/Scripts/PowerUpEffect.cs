using System.Collections;
using Mirror;
using UnityEngine;

public class PowerUpEffect : NetworkBehaviour
{
    [SerializeField] private GameObject healingPrefab = null;
    [SerializeField] private GameObject buffPrefab = null;
    [SerializeField] private GameObject debuffPrefab = null;
    [SerializeField] private GameObject mastodontPrefab = null;
    [SerializeField] private GameObject minePrefab = null;
    [SerializeField] private AudioSource heal_buff;
    [SerializeField] private AudioSource debuff;
    [SerializeField] private AudioSource mastodont;
    
    private GameObject effect = null;
    private MyPlayerNetwork owner;
    private Vector3 originalScale;
    private float powerUpDuration = 10f;

    private void Start()
    {
        originalScale = transform.localScale;
    }

    private IEnumerator ScaleOverTime(float powerUpDuration, float mastodontScaleFactor)
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / 1f; // Increase t by 1 every second
            transform.localScale = Vector3.Lerp(originalScale, originalScale * mastodontScaleFactor, t);
            yield return null;
        }

        yield return new WaitForSeconds(powerUpDuration-2);

        NetworkServer.Destroy(effect);
        effect = Instantiate(debuffPrefab, transform.position, Quaternion.identity);
        NetworkServer.Spawn(effect);
        debuff.Play();

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / powerUpDuration;
            transform.localScale = Vector3.Lerp(originalScale * mastodontScaleFactor, originalScale, t);
            yield return null;
        }
    }

    private IEnumerator DestroyEffect(float powerUpDuration, string type)
    {
        yield return new WaitForSeconds(powerUpDuration);

        if (type == "Buff") {
            NetworkServer.Destroy(effect);
            effect = Instantiate(debuffPrefab, transform.position, Quaternion.identity);
            NetworkServer.Spawn(effect);
            debuff.Play();
            yield return new WaitForSeconds(2f);
            NetworkServer.Destroy(effect);
        } else {
            yield return new WaitForSeconds(1f);
            NetworkServer.Destroy(effect);
        }
        NetworkServer.Destroy(gameObject);
    }

    #region Server

    [Server]
    public void setOwner(MyPlayerNetwork owner)
    {
        this.owner = owner;
    }

    [Server]
    public void setDuration(string type, float duration)
    {
        this.powerUpDuration = duration;
        if (type == "Mastodont") {
            StartCoroutine(ScaleOverTime(powerUpDuration, 2f));
        }
        StartCoroutine(DestroyEffect(powerUpDuration, type));
    }

    [Server]
    public void setType(string newType)
    {
        if (newType == "Health") {
            effect = Instantiate(healingPrefab, transform.position, Quaternion.identity);
            heal_buff.Play();
        }

        if (newType == "Buff") {
            effect = Instantiate(buffPrefab, transform.position, Quaternion.identity);
            heal_buff.Play();
        }

        if (newType == "Mastodont") {
            effect = Instantiate(mastodontPrefab, transform.position, Quaternion.identity);
            mastodont.Play();
        }

        if (newType == "Mine") {
            effect = Instantiate(minePrefab, transform.position, Quaternion.identity);
            heal_buff.Play();
        }

        NetworkServer.Spawn(effect);
    }

    #endregion

    #region Client

    [ClientCallback]
    private void Update()
    {
        if (effect == null) return;
        if (owner.TryGetComponent<MyPlayerNetwork>(out var player))
        {
            effect.transform.position = player.transform.position;
        }
    }

    #endregion
}
