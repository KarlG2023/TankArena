using System.Collections;
using Mirror;
using UnityEngine;

public class Tree : NetworkBehaviour
{
    [ClientCallback]
    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Player") && !other.CompareTag("Missile")) {
            return;
        }
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        Destroy(gameObject, 1f);
    }
}
