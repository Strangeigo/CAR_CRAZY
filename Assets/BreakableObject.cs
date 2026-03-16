using Unity.VisualScripting;
using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    [SerializeField] private Rigidbody[] _ExplodingParts;
    [SerializeField] private float _ExplosionForce = 500f;
    [SerializeField] private Rigidbody _basePart;
    [SerializeField] private Collider _WholeCollider;
    [SerializeField] private float _collisionForceNeeded = 18f; // Adjust this value based on your needs

    private void Update()
    {

    }
    private void BreakParts()
    {
        _WholeCollider.enabled = false;
        foreach (Rigidbody part in _ExplodingParts)
        {
            part.isKinematic = false;
            part.AddExplosionForce(_ExplosionForce, transform.position, 5f);
        }
        _basePart.isKinematic = false;
    }
    private void OnTriggerEnter(Collider other) 
    {
        if(other.CompareTag("Player"))
         BreakParts();
    }
    private void OnCollisionEnter(Collision other) 
    {
        Debug.Log("Collided with: " + other.gameObject.name);
        if(other.relativeVelocity.magnitude > _collisionForceNeeded) // Adjust the threshold as needed
        {
            Debug.Log("Collision force: " + other.relativeVelocity.magnitude);
            BreakParts();
        }
    }
}
