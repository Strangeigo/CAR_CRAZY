using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KartController : MonoBehaviour
{


    public Transform kartModel;
    public Transform kartNormal;
    public Rigidbody sphere;

    float speed, currentSpeed;
    float rotate, currentRotate;
    int driftDirection;
    float driftPower;
    int driftMode = 0;
    bool first, second, third;
    Color c;

    Coroutine punchCoroutine;
    Coroutine boostCoroutine;
    Coroutine rotateCoroutine;

    [Header("Bools")]
    public bool drifting;
    public bool grounded;
    [Header("Parameters")]

    public float acceleration = 30f;
    public float steering = 80f;
    public float steeringSpeedInfluenceFactor = 1.5f;
    public float minSpeedToSteer = 1f;
    public float minSteering = .5f;
    public float maxSteering = 1f;
    public float driftSteering = 40f;
    public float driftTiltAmount = 8f;
    public float maxTerrainTilt = 45f;
    public float lateralFriction = 0.15f;
    public float gravity = 10f;
    public LayerMask layerMask;

    [Header("Model Parts")]

    public Transform frontWheels;
    public Transform backWheels;
    public Transform steeringWheel;

    [Header("Particles")]
    public Color[] turboColors;

    void Start()
    {
    }

    void Update()
    {
        //Follow Collider
        transform.position = sphere.transform.position - new Vector3(0, 0.4f, 0);

        //Accelerate
        if (Input.GetButton("Vertical"))
            speed = acceleration;
        else
            speed = 0;

        //Steer
        if (Input.GetAxis("Horizontal") != 0 && currentSpeed > minSpeedToSteer)
        {
            int dir = Input.GetAxis("Horizontal") > 0 ? 1 : -1;
            float amount = Mathf.Abs(Input.GetAxis("Horizontal"));
            Steer(dir, amount);
        }

        //Drift
        if (Input.GetButtonDown("Jump") && !drifting)
        {
            // Start punch effect if not already running
            if (punchCoroutine == null)
            {
                punchCoroutine = StartCoroutine(PunchPosition(kartModel.parent, transform.up * .35f, .6f));
            }
            
            if(Input.GetAxis("Horizontal") != 0 && currentSpeed > minSpeedToSteer)
            {
                drifting = true;
                driftDirection = Input.GetAxis("Horizontal") > 0 ? 1 : -1;
            }
        }

        if (drifting)
        {
            float control = (driftDirection == 1) ? Remap(Input.GetAxis("Horizontal"), -1, 1, 0, 2) : Remap(Input.GetAxis("Horizontal"), -1, 1, 2, 0);
            float powerControl = (driftDirection == 1) ? Remap(Input.GetAxis("Horizontal"), -1, 1, .2f, 1) : Remap(Input.GetAxis("Horizontal"), -1, 1, 1, .2f);
            Steer(driftDirection, control, driftSteering);
            driftPower += powerControl;

            ColorDrift();
        }

        if (Input.GetButtonUp("Jump") && drifting)
        {
            Boost();
        }

        currentSpeed = Mathf.SmoothStep(currentSpeed, speed, Time.deltaTime * 12f); speed = 0f;
        currentRotate = Mathf.Lerp(currentRotate, rotate, Time.deltaTime * 4f); rotate = 0f;

        //Animations    

        //a) Kart
        if (!drifting)
        {
            kartModel.localEulerAngles = Vector3.Lerp(kartModel.localEulerAngles, new Vector3(0, 90 + (Input.GetAxis("Horizontal") * 15), kartModel.localEulerAngles.z), .2f);
        }
        else
        {
            float control = (driftDirection == 1) ? Remap(Input.GetAxis("Horizontal"), -1, 1, .5f, 2) : Remap(Input.GetAxis("Horizontal"), -1, 1, 2, .5f);
            kartModel.parent.localRotation = Quaternion.Euler(0, Mathf.LerpAngle(kartModel.parent.localEulerAngles.y,(control * driftTiltAmount) * driftDirection, .2f), 0);
        }

        //b) Wheels
        frontWheels.localEulerAngles = new Vector3(0, (Input.GetAxis("Horizontal") * 15), frontWheels.localEulerAngles.z);
        frontWheels.localEulerAngles += new Vector3(0, 0, sphere.linearVelocity.magnitude/2);
        backWheels.localEulerAngles += new Vector3(0, 0, sphere.linearVelocity.magnitude/2);

        //c) Steering Wheel
        steeringWheel.localEulerAngles = new Vector3(-25, 90, ((Input.GetAxis("Horizontal") * 45)));

    }

    private void FixedUpdate()
    {
        //Forward Acceleration - applied horizontally to prevent flying on slopes
        Vector3 accelerationDirection = !drifting ? transform.forward : transform.forward;
        sphere.AddForce(accelerationDirection * currentSpeed, ForceMode.Acceleration);

        // Calculate tilt angle from vertical
        float tiltAngle = Vector3.Angle(kartNormal.up, Vector3.up);
        float tiltCompensation = Mathf.Sin(tiltAngle * Mathf.Deg2Rad) * currentSpeed * 0.5f;
        
        //Gravity with compensation for tilted slopes
        sphere.AddForce(Vector3.down * (gravity + tiltCompensation), ForceMode.Acceleration);

        //Lateral Friction - Reduce drift without losing forward speed
        if (grounded)
        {
            Vector3 localVelocity = transform.worldToLocalMatrix.MultiplyVector(sphere.linearVelocity);
            localVelocity.x *= (1f - lateralFriction); // Dampen sideways movement
            sphere.linearVelocity = transform.localToWorldMatrix.MultiplyVector(localVelocity);
        }

        //Steering
        transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, new Vector3(0, transform.eulerAngles.y + currentRotate, 0), Time.deltaTime * 5f);

        RaycastHit hitOn;
        RaycastHit hitNear;

        // Check grounded with close raycast
        grounded = Physics.Raycast(transform.position + (transform.up*.1f), Vector3.down, out hitOn, 1.1f, layerMask);
        // Get surface normal from longer raycast
        bool hitNearGround = Physics.Raycast(transform.position + (transform.up * .1f), Vector3.down, out hitNear, 2.0f, layerMask);

        //Normal Rotation - clamp and apply quickly
        if (hitNearGround)
        {
            // Clamp the normal to prevent excessive tilting
            Vector3 clampedNormal = Vector3.Lerp(Vector3.up, hitNear.normal, Mathf.Clamp01(Vector3.Angle(Vector3.up, hitNear.normal) / maxTerrainTilt));
            kartNormal.up = clampedNormal; // Instant rotation instead of lerp
            kartNormal.Rotate(0, transform.eulerAngles.y, 0);
        }
    }

    public void Boost()
    {
        drifting = false;

        // Stop previous coroutines and reset position
        if (punchCoroutine != null)
        {
            StopCoroutine(punchCoroutine);
            punchCoroutine = null;
        }
        if (boostCoroutine != null)
            StopCoroutine(boostCoroutine);
        if (rotateCoroutine != null)
            StopCoroutine(rotateCoroutine);

        // Ensure position is reset
        kartModel.parent.localPosition = Vector3.zero;

        if (driftMode > 0)
        {
            boostCoroutine = StartCoroutine(FloatTween(currentSpeed * 3, currentSpeed, .3f * driftMode, Speed));
        }

        driftPower = 0;
        driftMode = 0;
        first = false; second = false; third = false;

        rotateCoroutine = StartCoroutine(LocalRotate(kartModel.parent, Vector3.zero, .5f));
    }

    public void Steer(int direction, float amount)
    {
        float actualSpeed = sphere.linearVelocity.magnitude;
        float speedInfluence = Mathf.Clamp01(actualSpeed / (acceleration * steeringSpeedInfluenceFactor));
        float speedMultiplier = Mathf.Lerp(minSteering, maxSteering, speedInfluence);
        rotate = (steering * speedMultiplier * direction) * amount;
    }

    public void Steer(int direction, float amount, float customSteering)
    {
        float actualSpeed = sphere.linearVelocity.magnitude;
        float speedInfluence = Mathf.Clamp01(actualSpeed / (acceleration * steeringSpeedInfluenceFactor));
        float speedMultiplier = Mathf.Lerp(minSteering, maxSteering, speedInfluence);
        rotate = (customSteering * speedMultiplier * direction) * amount;
    }

    void PlayFlashParticle(Color c)
    {
        // Particle effect placeholder
        
    }

    public void ColorDrift()
    {
        if(!first)
            c = Color.clear;

        if (driftPower > 50 && driftPower < 100-1 && !first)
        {
            first = true;
            c = turboColors[0];
            driftMode = 1;

            PlayFlashParticle(c);
        }

        if (driftPower > 100 && driftPower < 150- 1 && !second)
        {
            second = true;
            c = turboColors[1];
            driftMode = 2;

            PlayFlashParticle(c);
        }

        if (driftPower > 150 && !third)
        {
            third = true;
            c = turboColors[2];
            driftMode = 3;

            PlayFlashParticle(c);
        }
    }



    private void Speed(float x)
    {
        currentSpeed = x;
    }


    // Custom methods to replace DOTween and ExtensionMethods

    /// <summary>
    /// Remaps a value from one range to another
    /// </summary>
    private float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    /// <summary>
    /// Animates a punch position effect on a transform
    /// </summary>
    private IEnumerator PunchPosition(Transform target, Vector3 punch, float duration, float vibrato = 2.5f)
    {
        Vector3 startPos = target.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Simulate punch with damping oscillation
            float damping = 1f - (elapsed / duration);
            float oscillation = Mathf.Sin(progress * Mathf.PI * vibrato) * damping;
            
            target.localPosition = startPos + punch * oscillation;
            yield return null;
        }

        target.localPosition = startPos;
        punchCoroutine = null;
    }

    /// <summary>
    /// Animates a float value from start to end over duration
    /// </summary>
    private IEnumerator FloatTween(float from, float to, float duration, System.Action<float> callback)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float value = Mathf.Lerp(from, to, progress);
            callback?.Invoke(value);
            yield return null;
        }

        callback?.Invoke(to);
    }

    /// <summary>
    /// Animates local rotation to target with easing
    /// </summary>
    private IEnumerator LocalRotate(Transform target, Vector3 to, float duration)
    {
        Quaternion startRot = target.localRotation;
        Quaternion endRot = Quaternion.Euler(to);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            // OutBack easing
            float easeProgress = 1f + 2.70158f * Mathf.Pow(progress - 1f, 3f) + 1.70158f * Mathf.Pow(progress - 1f, 2f);
            target.localRotation = Quaternion.Lerp(startRot, endRot, easeProgress);
            yield return null;
        }

        target.localRotation = endRot;
    }



    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawLine(transform.position + transform.up, transform.position - (transform.up * 2));
    //}
}
