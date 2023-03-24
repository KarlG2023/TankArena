using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class aiShortRange : NetworkBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsPlayer;
    [SerializeField] private LayerMask vision;
    private float health = 40;
    private bool _walkPointSet;
    private float lastMissileFiredTime = 0.0f;

    //Attacking
    private float timeBetweenAttacks = 2;
    public GameObject projectile;
   
    [ServerCallback]
    private void OnTriggerStay(Collider other)
    {

        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent<MyPlayerNetwork>(out var player))
        {
            if (!player.isPlayerReady()) return;
            transform.LookAt(player.transform);
            AttackPlayer(new Vector3(player.PositionX, 0 ,player.PositionY));
            agent.SetDestination(new Vector3(player.PositionX, 0 ,player.PositionY));
        }
    }

    [Server]
    private void AttackPlayer(Vector3 pos)
    {
        if (Time.time - lastMissileFiredTime < this.timeBetweenAttacks)
        {
            return;
        }
        GameObject missile;
        lastMissileFiredTime = Time.time;
        missile = Instantiate(projectile, transform.position, transform.rotation);
        if (missile.TryGetComponent<Missile>(out var missileComponent)) {
            missileComponent.ChangeMeshColor(Color.red);
            missileComponent.setMonster(this);
        }
        NetworkServer.Spawn(missile);

    }

    [Server]
    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0) Invoke(nameof(DestroyEnemy), 0.5f);
    }
    [Server]
    public void DestroyEnemy()
    {
        NetworkServer.Destroy(gameObject);
    }
}
