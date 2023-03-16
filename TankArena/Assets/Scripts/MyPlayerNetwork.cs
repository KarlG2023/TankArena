using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class MyPlayerNetwork : NetworkBehaviour
{
    // Server variables
    [SyncVar(hook = nameof(HandlePseudoChanged))]
    string pseudo = "Player";

    [SyncVar(hook = nameof(HandleColorChanged))]
    Color color = Color.red;

    [SyncVar(hook = nameof(HandleTurretRotationChanged))]
    Quaternion turretRotation = Quaternion.identity;

    [SyncVar(hook = nameof(HandleAvailableMinesChanged))]
    int nbrMines = 1;

    [SyncVar(hook = nameof(HandleShootCouldownChanged))]
    float ShootCouldown = 2;

    [SyncVar(hook = nameof(HandleHealthChanged))]
    float Health = 100;

    // Only client variables
    [SerializeField] private UnityEvent<string> onPseudoChanged;
    [SerializeField] private UnityEvent<int> onAvailableMinesChanged;
    [SerializeField] private UnityEvent<float> onShootCouldownChanged;
    [SerializeField] private UnityEvent<float> onHealthChanged;
    [SerializeField] private GameObject minePrefab = null;
    [SerializeField] private GameObject missilePrefab = null;
    [SerializeField] private GameObject powerUpEffectPrefab = null;
    [SerializeField] private Camera tankCamera = null;

    private Vector3 movementDirection;
    private Vector3 pointOnTurret;
    private Quaternion targetRotation;
    private float lastMissileFiredTime = 0.0f;

    public override void OnStartLocalPlayer()
    {
        tankCamera = Camera.main;
    }

    private IEnumerator Mastodont(float duration, float newHealth, float mastodontScaleFactor)
    {
        this.Health += newHealth;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / 1f; // Increase t by 1 every second
            transform.localScale = Vector3.Lerp(new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f) * mastodontScaleFactor, t);
            yield return null;
        }

        yield return new WaitForSeconds(duration-2);

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / 1f;
            transform.localScale = Vector3.Lerp(new Vector3(1.0f, 1.0f, 1.0f) * mastodontScaleFactor, new Vector3(1.0f, 1.0f, 1.0f), t);
            yield return null;
        }

        if (this.Health - newHealth <= 0) {
            this.Health = 10;
        } else {
            this.Health -= newHealth;
        }
    }

    private IEnumerator FireRateBuff(float duration, float newFireRate)
    {
        this.ShootCouldown += newFireRate;
        yield return new WaitForSeconds(duration);
        this.ShootCouldown -= newFireRate;
    }

    #region Server

    [Server]
    [ContextMenu("Change Color")]
    public void ChangeColor()
    {
        this.color = Random.ColorHSV();

        // Change color for the server
        if (TryGetComponent<MeshRenderer>(out var meshRenderer))
        {
            meshRenderer.material.color = this.color;
        }
    }

    [Server]
    public void SetPseudo(string newPseudo)
    {
        this.RPCDisplayNewName(this.pseudo, newPseudo);
        this.pseudo = newPseudo;
    }

    [Server]
    public void SetTurretRotation(Quaternion newRotation)
    {
        this.turretRotation = newRotation;
    }

    [Server]
    public void SetHealth(float newHealth)
    {
        this.Health += newHealth;

        if (this.Health <= 0) {

        }
    }

    [Server]
    public void SetMines(int newValue)
    {
        this.nbrMines += newValue;
    }

    [Server]
    public void SetFireRate(float newFireRate, float duration)
    {
        StartCoroutine(FireRateBuff(duration, newFireRate));
    }

    [Server]
    public void ActivateMastodont(float health, float duration)
    {
        StartCoroutine(Mastodont(duration, health, 1.5f));
    }

    [Command]
    public void CmdMoveTo(float horizontalInput, float verticalInput)
    {
        if (this.Health <= 0) return;
        movementDirection = new Vector3(horizontalInput, 0.0f, verticalInput).normalized;
        transform.Translate(movementDirection * 5.0f * Time.deltaTime);

        // If there is no movement, don't rotate the mesh
        if ((Vector2) movementDirection == Vector2.zero)
        {
            return;
        }

        // Determine rotation direction based on movement direction
        int rotationDirection = Mathf.RoundToInt(Mathf.Sign(movementDirection.x));

        // If moving backwards, invert rotation direction
        if (verticalInput < 0)
        {
            rotationDirection *= -1;
        }

        // Rotate the player mesh
        transform.Rotate(Vector3.up, 100.0f * Time.deltaTime * rotationDirection);
    }

    [Command]
    public void CmdRotateTankTurret(Vector3 pointOnTurret)
    {
        if (this.Health <= 0) return;
        if (transform.Find("TankFree_Tower").gameObject.TryGetComponent<MeshRenderer>(out var meshRenderer))
        {
            meshRenderer.transform.LookAt(pointOnTurret);
            this.SetTurretRotation(meshRenderer.transform.rotation);
        }
    }

    [Command]
    public void CmdSpawnMine()
    {
        if (minePrefab == null) return;
        if (this.nbrMines <= 0) return;
        if (this.Health <= 0) return;

        var mine = Instantiate(minePrefab);

        // Calculate the position of the mine based on the rotation of the vehicle
        float offset = 1.0f;
        float height = 1.0f;
        
        Vector3 spawnPosition = transform.position - transform.forward * offset;
        spawnPosition.y -= height;

        mine.transform.position = spawnPosition;
        mine.transform.rotation = Quaternion.identity;

        if (mine.TryGetComponent<Mine>(out var mineComponent))
        {
            mineComponent.ChangeMeshColor(this.color);
            mineComponent.setOwner(this);
        }

        SetMines(-1);
        NetworkServer.Spawn(mine);
    }

    [Server]
    public void SpawnMissile()
    {
        if (missilePrefab == null) return;
        var missile = Instantiate(missilePrefab);

        if (transform.Find("TankFree_Tower").gameObject.TryGetComponent<MeshRenderer>(out var meshRenderer))
        {
            if (meshRenderer.transform.Find("TankFree_Canon").gameObject.TryGetComponent<MeshRenderer>(out var meshCanonRenderer))
            {
                Vector3 spawnPosition = meshCanonRenderer.transform.position;
                float height = 0.2f;

                spawnPosition.y -= height;
                missile.transform.rotation = turretRotation;
                missile.transform.position = spawnPosition;

                if (missile.TryGetComponent<Missile>(out var missileComponent))
                {
                    missileComponent.ChangeMeshColor(this.color);
                    missileComponent.setOwner(this);
                }

                NetworkServer.Spawn(missile);
            }
        }
    }

    [Command]
    public void CmdShoot()
    {
        if (missilePrefab == null) return;
        if (this.Health <= 0) return;

        if (Time.time - lastMissileFiredTime < this.ShootCouldown)
        {
            return;
        }

        lastMissileFiredTime = Time.time;
        this.SpawnMissile();
    }

    [Server]
    public void SpawnEffectStatus(string type, float duration)
    {
        if (powerUpEffectPrefab == null) return;
        var effect = Instantiate(powerUpEffectPrefab);

        if (effect.TryGetComponent<PowerUpEffect>(out var effectComponent))
        {
            effectComponent.setOwner(this);
            effectComponent.setDuration(type, duration);
            effectComponent.setType(type);
        }
        NetworkServer.Spawn(effect);
    }

    [Command]
    public void CmdShareEffectStatus(string type, float duration)
    {
        this.SpawnEffectStatus(type, duration);
    }

    #endregion

    #region Client

    [ClientRpc]
    private void RPCDisplayNewName(string oldName, string newName)
    {
        Debug.Log($"{newName} enters the game.");
    }


    [TargetRpc]
    public void TRPCCollidePowerUp(NetworkConnection target, string type, float duration)
    {
        this.CmdShareEffectStatus(type, duration);
    }

    [Client]
    private void HandleColorChanged(Color oldColor, Color newColor)
    {
        if (TryGetComponent<MeshRenderer>(out var meshRenderer))
        {
            meshRenderer.material.color = newColor;
        }
        
        // Try to get the MeshRenderer from the children of this game object
        MeshRenderer[] childMeshRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer childMeshRenderer in childMeshRenderers)
        {
            if (childMeshRenderer != meshRenderer) // To avoid modifying the same MeshRenderer twice
            {
                childMeshRenderer.material.color = newColor;
            }
        }
    }

    [Client]
    private void HandlePseudoChanged(string oldPseudo, string newPseudo)
    {
        this.onPseudoChanged?.Invoke(newPseudo);
    }

    [Client]
    private void HandleTurretRotationChanged(Quaternion oldRotation, Quaternion newRotation)
    {
        targetRotation = newRotation;
    }

    [Client]
    private void HandleAvailableMinesChanged(int oldQuantity, int newQuantity)
    {
        this.onAvailableMinesChanged?.Invoke(newQuantity);
    }

    [Client]
    private void HandleShootCouldownChanged(float oldValue, float newValue)
    {
        this.onShootCouldownChanged?.Invoke(newValue);
    }

    [Client]
    private void HandleHealthChanged(float oldValue, float newValue)
    {
        this.onHealthChanged?.Invoke(newValue);
        if (newValue <= 0) {
            GetComponent<Explosion>().enabled = true;
        }
    }

    [ClientCallback]
    private void Update()
    {
        if (transform.Find("TankFree_Tower").gameObject.TryGetComponent<Transform>(out var meshRenderer))
        {
        Quaternion newRotation = Quaternion.Lerp(meshRenderer.transform.rotation, targetRotation, Time.deltaTime * 10f);
            meshRenderer.rotation = newRotation;
        }

        if (!isOwned) return;

        Vector3 targetPosition = transform.position;
        Vector3 cameraPosition = new Vector3(targetPosition.x, targetPosition.y + 10f, targetPosition.z);
        tankCamera.transform.position = cameraPosition;

        // Look at the target
        tankCamera.transform.LookAt(targetPosition);

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (verticalInput != 0)
        {
            this.CmdMoveTo(horizontalInput, verticalInput);
        }

        Ray ray = tankCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayDistance;

        if (transform.Find("TankFree_Tower").gameObject.TryGetComponent<MeshRenderer>(out var childMeshRenderer))
        {
            // Rotate the child MeshRenderer
            if (groundPlane.Raycast(ray, out rayDistance))
            {
                Vector3 point = ray.GetPoint(rayDistance);
                if (pointOnTurret != new Vector3(point.x, childMeshRenderer.transform.position.y, point.z))
                {
                    pointOnTurret = new Vector3(point.x, childMeshRenderer.transform.position.y, point.z);
                    this.CmdRotateTankTurret(pointOnTurret);
                }
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            this.CmdShoot();
        }

        if (Input.GetMouseButtonDown(1))
        {
            this.CmdSpawnMine();
        }

    }

    #endregion
}