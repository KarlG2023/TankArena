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

    [SyncVar(hook = nameof(HandleAvailableMissilesChanged))]
    int nbrMissiles = 10;

    [SyncVar(hook = nameof(HandleShootCouldownChanged))]
    float ShootCouldown = 2;

    [SyncVar(hook = nameof(HandleHealthChanged))]
    float Health = 100;

    // Only client variables
    [SerializeField] private UnityEvent<string> onPseudoChanged;
    [SerializeField] private UnityEvent<int> onAvailableMinesChanged;
    [SerializeField] private UnityEvent<int> onAvailableMissilesChanged;
    [SerializeField] private UnityEvent<float> onShootCouldownChanged;
    [SerializeField] private UnityEvent<float> onHealthChanged;
    [SerializeField] private GameObject minePrefab = null;
    [SerializeField] private GameObject missilePrefab = null;
    [SerializeField] private GameObject shootCouldownEffectPrefab = null;
    [SerializeField] private GameObject HealthEffectPrefab = null;
    [SerializeField] private Camera tankCamera = null;

    private float lastMissileFiredTime = 0.0f;
    private Vector3 movementDirection;
    private Vector3 pointOnTurret;
    private Quaternion targetRotation;

    public override void OnStartLocalPlayer()
    {
        tankCamera = Camera.main;
    }

    private IEnumerator FadeAndDestroy(GameObject obj, float delay)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        Color color = renderer.material.color;

        // fade the object over time
        for (float t = 1.0f; t > 0.0f; t -= Time.deltaTime / delay)
        {
            color.a = t;
            renderer.material.color = color;
            yield return null;
        }

        Destroy(obj); // destroy the object after fading
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
    public void SpawnMissile()
    {
        var missile = Instantiate(missilePrefab);

        if (transform.Find("TankFree_Tower").gameObject.TryGetComponent<MeshRenderer>(out var meshRenderer))
        {
            if (meshRenderer.transform.Find("TankFree_Canon").gameObject.TryGetComponent<MeshRenderer>(out var meshCanonRenderer))
            {
                Vector3 spawnPosition = meshCanonRenderer.transform.position;
                float height = 0.1f;

                spawnPosition.y -= height;
                missile.transform.rotation = turretRotation;
                missile.transform.position = spawnPosition;

                if (missile.TryGetComponent<Missile>(out var missileComponent))
                {
                    missileComponent.ChangeMeshColor(this.color);
                    missileComponent.setOwner(this);
                }

                this.nbrMissiles = this.nbrMissiles - 1;
                NetworkServer.Spawn(missile);
            }
        }
    }

    [Command]
    public void CmdMoveTo(float horizontalInput, float verticalInput)
    {
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

        this.nbrMines = this.nbrMines - 1;
        this.RPCDroppedAMine(this.pseudo, this.nbrMines);
        NetworkServer.Spawn(mine);
    }

    [Command]
    public void CmdShoot()
    {
        if (missilePrefab == null) return;
        if (this.nbrMissiles <= 0) return;

        if (Time.time - lastMissileFiredTime < ShootCouldown)
        {
            return;
        }

        lastMissileFiredTime = Time.time;
        this.SpawnMissile();
    }

    #endregion

    #region Client

    [ClientRpc]
    private void RPCDroppedAMine(string name, int quantity)
    {
        Debug.Log($"{name} dropped a mine ! He has {quantity} mines left !");
    }

    [ClientRpc]
    private void RPCDisplayNewName(string oldName, string newName)
    {
        Debug.Log($"{newName} enters the game.");
    }

    [TargetRpc]
    public void TRPCCollideMine(NetworkConnection target, string message)
    {
        Debug.Log(message);
    }

    [TargetRpc]
    public void TRPCCollideMissile(NetworkConnection target, string message)
    {
        Debug.Log(message);
    }

    [TargetRpc]
    public void TRPCCollidePowerUp(NetworkConnection target, string message, string type, float value)
    {
        Debug.Log(message);
        if (type == "ShootCouldown")
        {
            this.ShootCouldown += value;
            GameObject shootCooldownEffect = Instantiate(shootCouldownEffectPrefab, transform.position, Quaternion.identity);
            shootCooldownEffect.transform.parent = transform;
            NetworkServer.Spawn(shootCooldownEffect);
            StartCoroutine(FadeAndDestroy(shootCooldownEffect, 5f)); // start coroutine to fade and destroy the effect after 2 seconds
        }
        if (type == "Health")
        {
            if (this.Health + value > 100) {
                this.Health = 100;
            } else {
                this.Health += value;
            }
            GameObject HealthEffect = Instantiate(HealthEffectPrefab, transform.position, Quaternion.identity);
            HealthEffect.transform.parent = transform;
            NetworkServer.Spawn(HealthEffect);
            StartCoroutine(FadeAndDestroy(HealthEffect, 5f)); // start coroutine to fade and destroy the effect after 2 seconds
        }
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
    private void HandleAvailableMissilesChanged(int oldQuantity, int newQuantity)
    {
        this.onAvailableMissilesChanged?.Invoke(newQuantity);
    }

    [Client]
    private void HandleShootCouldownChanged(float oldValue, float newValue)
    {
        this.onShootCouldownChanged?.Invoke(newValue);
    }

    [Client]
    private void HandleHealthChanged(float oldValue, float newValue)
    {
        this.onShootCouldownChanged?.Invoke(newValue);
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