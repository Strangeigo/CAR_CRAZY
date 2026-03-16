using UnityEngine;

public class WheelHandler : MonoBehaviour
{
    [SerializeField] private WheelCollider _wheelCollider;
    private Vector3 _wheelPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    private void FixedUpdate() 
    {
        _wheelCollider.GetWorldPose(out Vector3 lPos, out Quaternion rot);
        _wheelPosition.Set(transform.position.x, lPos.y, transform.position.z);
        transform.position = _wheelPosition;
        transform.rotation = rot;
    }    
}
