using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    // ─── Wheels ────────────────────────────────────────────────────────────
    // Your array order: [0]=RL, [1]=RR, [2]=FL, [3]=FR
    // Assign them in that order in the Inspector.
    [Header("Wheels — RL, RR, FL, FR")]
    [SerializeField] private WheelCollider[] _wheelColliders;   // size 4

    [Header("Wheel Meshes (optional)")]
    [SerializeField] private Transform[] _wheelMeshes;          // same order

    // ─── Motor ─────────────────────────────────────────────────────────────
    [Header("Motor")]
    [SerializeField] private float _torque      = 1000f;
    [SerializeField] private float _maxSpeedKmh = 110f;

    // ─── Steering ──────────────────────────────────────────────────────────
    [Header("Steering")]
    [SerializeField] private float _maxSteerAngle = 32f;
    // At max speed, steer angle is reduced by this fraction (0.5 = half angle)
    [SerializeField] private float _steerDamping  = 0.55f;

    // ─── Brakes ────────────────────────────────────────────────────────────
    [Header("Brakes")]
    [SerializeField] private float _brakeTorque = 2000f;

    // ─── Anti-rollover ─────────────────────────────────────────────────────
    [Header("Anti-rollover")]
    // Sway bar: resists axle roll without killing the bouncy suspension
    [SerializeField] private float   _antiRollForce = 2500f;

    // ─── Private ───────────────────────────────────────────────────────────
    private Rigidbody _rb;
    private bool      _jumpQueued;

    // Front wheels: indices 0 & 1 
    private const int FL = 0, FR = 1;
    // Rear wheels: indices 2 & 3
    private const int RL = 2, RR = 3;

    // ───────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Input must be read in Update to never miss a frame
        if (Input.GetButtonDown("Jump"))
            _jumpQueued = true;
    }

    void FixedUpdate()
    {
        float throttle = Input.GetAxis("Vertical");
        float steer    = Input.GetAxis("Horizontal");
        bool  braking  = Input.GetKey(KeyCode.Space);

        HandleMotor(throttle, braking);
        HandleSteering(steer);
        HandleJump();

        // Anti-roll applied per axle
        ApplyAntiRoll(FL, FR);
        ApplyAntiRoll(RL, RR);

        SyncMeshes();
    }

    // ─── Motor ─────────────────────────────────────────────────────────────
    void HandleMotor(float throttle, bool braking)
    {
        float speedKmh   = _rb.linearVelocity.magnitude * 3.6f;
        float speedRatio = Mathf.Clamp01(speedKmh / _maxSpeedKmh);
        // Torque fades as you approach max speed (soft limiter)
        float torque     = braking ? 0f
                                   : _torque * throttle * (1f - speedRatio * speedRatio);
        float brake      = braking ? _brakeTorque : 0f;

        for (int i = 0; i < _wheelColliders.Length; i++)
        {
            _wheelColliders[i].motorTorque = torque;
            _wheelColliders[i].brakeTorque = brake;
        }
    }

    // ─── Steering ──────────────────────────────────────────────────────────
    void HandleSteering(float steer)
    {
        float speedKmh = _rb.linearVelocity.magnitude * 3.6f;
        float t        = Mathf.Clamp01(speedKmh / _maxSpeedKmh);
        float angle    = _maxSteerAngle * Mathf.Lerp(1f, 1f - _steerDamping, t);

        // Only front wheels steer (indices 2 & 3 = FL & FR in your setup)
        _wheelColliders[FL].steerAngle = steer * angle;
        _wheelColliders[FR].steerAngle = steer * angle;
    }

    // ─── Jump ──────────────────────────────────────────────────────────────
    void HandleJump()
    {
        if (!_jumpQueued) return;
        _jumpQueued = false;

        bool grounded = false;
        foreach (var w in _wheelColliders)
            grounded |= w.isGrounded;

        if (!grounded) return;

        // Cancel downward velocity so jump height is always consistent
        var v = _rb.linearVelocity;
        v.y = 0f;
        _rb.linearVelocity = v;
        _rb.AddForce(Vector3.up * 7f, ForceMode.VelocityChange);
    }

    // ─── Anti-roll bar ─────────────────────────────────────────────────────
    // Compares suspension travel between left & right wheel on the same axle.
    // If one side is more compressed, it pushes the other side down to level out.
    // This resists barrel rolls without touching suspension spring/damper values.
    void ApplyAntiRoll(int leftIdx, int rightIdx)
    {
        var   left  = _wheelColliders[leftIdx];
        var   right = _wheelColliders[rightIdx];
        float tL    = 1f, tR = 1f;   // 0 = fully compressed, 1 = fully extended

        if (left.GetGroundHit(out WheelHit h))
            tL = (-left.transform.InverseTransformPoint(h.point).y
                  - left.radius) / left.suspensionDistance;

        if (right.GetGroundHit(out h))
            tR = (-right.transform.InverseTransformPoint(h.point).y
                  - right.radius) / right.suspensionDistance;

        float force = (tL - tR) * _antiRollForce;

        if (left.isGrounded)
            _rb.AddForceAtPosition( left.transform.up *  force, left.transform.position);
        if (right.isGrounded)
            _rb.AddForceAtPosition(right.transform.up * -force, right.transform.position);
    }

    // ─── Visual sync ───────────────────────────────────────────────────────
    void SyncMeshes()
    {
        if (_wheelMeshes == null) return;
        int count = Mathf.Min(_wheelColliders.Length, _wheelMeshes.Length);
        for (int i = 0; i < count; i++)
        {
            if (_wheelMeshes[i] == null) continue;
            _wheelColliders[i].GetWorldPose(out Vector3 p, out Quaternion r);
            _wheelMeshes[i].SetPositionAndRotation(p, r);
        }
    }
}