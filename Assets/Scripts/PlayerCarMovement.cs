
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class WheelProperties
{
    [HideInInspector] public TrailRenderer skidTrail;
    [HideInInspector] public ParticleSystem skidParticles;

    [HideInInspector] public Vector3 localPosition;
    public bool canTurn = false;
    [HideInInspector] public float turnAngle = 30f;

    [HideInInspector] public float lastSuspensionLength = 0.0f;
    [HideInInspector] public float mass = 16f;
    [HideInInspector] public float size = 0.5f;
    public float engineTorque = 40f; // Engine power in Nm to wheel
    public float brakeStrength = 0.5f; // Brake torque
    public float lateralSlipFactor = 4f;
    public float travelSlipFactor = 1f;
    [Tooltip("This is the percentage of grip you can apply based on slip %")]
    public AnimationCurve lateralGripCurve;
    public AnimationCurve travelGripCurve;
    public bool slidding = false;
    [HideInInspector] public Vector3 worldSlipDirection;
    [HideInInspector] public Vector3 suspensionForceDirection;
    [HideInInspector] public Vector3 wheelWorldPosition;
    [HideInInspector] public float wheelCircumference;
    [HideInInspector] public float torque = 0.0f;
    [HideInInspector] public GameObject wheelObject;
    [HideInInspector] public Vector3 localVelocity;
    [HideInInspector] public float normalForce;
    [HideInInspector] public float angularVelocity = 0; // rad/sec
    [HideInInspector] public float lateralSlip;
    [HideInInspector] public float travelSlip;
    [HideInInspector] public Vector2 input = Vector2.zero;// horizontal=steering, vertical=gas/brake
    [HideInInspector] public float braking = 0;
}

public class PlayerCarMovement : MonoBehaviour
{
    [SerializeField]
    private float MaxSpeed = 40f;
    public float GetMaxSpeed {  get { return MaxSpeed; } }
    [SerializeField]
    private float MoveSpeedIncrease = 20f;
    [SerializeField]
    [Tooltip("Brake factor when accel and brake inputs are 0")]
    private float ReleaseBrake = 0.2f;

    [SerializeField]
    private float MaxStaticLateralForce = 15000;
    [SerializeField]
    private float MaxStaticTravelForce = 25000;
    [SerializeField]
    private float MinKineticLateralForce = 2500;
    [SerializeField]
    private float MinKineticTravelForce = 5000;

    [SerializeField]
    private float SpringStrength = 10f;
    [SerializeField]
    private float SpringClamp = 200f;
    [SerializeField]
    private float DamperStrength = 0.95f;

    [SerializeField]
    private float MaxTurnAngle = 30f;

    [SerializeField]
    private float MaxSpringExtension = 1f;

    [SerializeField]
    private float leaveGroundCoyoteTime = 0.1f;
    [SerializeField]
    private float hitGroundCoyoteTime = 0.1f;

    private float groundTimer = 0f;
    private bool grounded = false;

    [Header("Air Controls")]
    [SerializeField]
    private float airControlCoyoteTime = 0.25f;

    [SerializeField]
    private float airRollForce = 1;
    [SerializeField]
    private float airPitchForce = 1;

    private static int WHEEL_FR = 0;
    private static int WHEEL_FL = 1;
    private static int WHEEL_BR = 2;
    private static int WHEEL_BL = 3;


    private Vector2 lastMoveInput;
    private float lastBrakeInput;
    private float steerAngle = 0;
    private float effectiveBrakeInput;
    private int gear = 0;

    private Rigidbody rb;
    public Vector3 GetCurrentVelocity { get { return rb.velocity; } }
    private SphereCollider[] m_wheelObjs;
    public WheelProperties[] m_wheels;

    public GameObject skidMarkPrefab; // Assign a prefab with a TrailRenderer in the inspector

    public float smoothTurn = 0.03f;
    public float wheelGripX = 8f;
    public float wheelGripZ = 42f;

    

    private int StringToWheelIndex(string str)
    {
        if (str == "WheelFR")
        {
            return WHEEL_FR;
        }
        else if (str == "WheelFL")
        {
            return WHEEL_FL;
        }
        else if (str == "WheelBR")
        {
            return WHEEL_BR;
        }
        else if (str == "WheelBL")
        {
            return WHEEL_BL;
        }

        Debug.LogWarning("Could not find index for: " + str);

        return -1;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        SphereCollider[] children = GetComponentsInChildren<SphereCollider>();
        m_wheelObjs = new SphereCollider[children.Length];
        //m_wheels = new WheelProperties[children.Length];

        for (int i = 0; i < children.Length; i++)
        {
            m_wheelObjs[i] = children[i];
        }

        int wheelIdx = 0;
        Vector3 aveLocalPos = Vector3.zero;
        foreach (WheelProperties wheel in m_wheels)
        {
            //wheel.wheelObject = m_wheelObjs[wheelIdx]; // Assign the object we found earlier
            wheel.wheelObject = m_wheelObjs[wheelIdx].gameObject;
            wheelIdx++;
            wheel.size = wheel.wheelObject.transform.localScale.magnitude * 0.5f;
            //wheel.wheelObject.transform.localScale = 2f * new Vector3(wheel.size, wheel.size, wheel.size);
            wheel.turnAngle = wheel.canTurn ? MaxTurnAngle : 0;
            wheel.wheelCircumference = 2f * Mathf.PI * wheel.size; // Calculate wheel circumference for rotation logic
            wheel.angularVelocity = 0;
            wheel.localPosition = wheel.wheelObject.transform.localPosition; // Setup visual match then translate to this
            aveLocalPos += wheel.localPosition;

            // Instantiate and setup the skid trail (if a prefab is assigned)
            if (skidMarkPrefab != null)
            {
                GameObject skidTrailObj = Instantiate(skidMarkPrefab, wheel.wheelObject.transform);
                // Reset local position if needed
                skidTrailObj.transform.localPosition = Vector3.zero;
                wheel.skidTrail = skidTrailObj.GetComponent<TrailRenderer>();
                if (wheel.skidTrail != null)
                {
                    wheel.skidTrail.emitting = false; // start with emission off
                }

                wheel.skidParticles = skidTrailObj.GetComponent<ParticleSystem>();
                if(wheel.skidParticles)
                {
                    wheel.skidParticles.Stop();
                }
            }
            
        }
        aveLocalPos /= m_wheels.Length;
        //rb.centerOfMass = rb.centerOfMass + new Vector3(0, -1.5f, 0); // + aveLocalPos; // Adjust center of mass for better handling

        Transform centerOfMass = gameObject.transform.Find("CoM");
        if(centerOfMass != null)
        {
            rb.centerOfMass = centerOfMass.localPosition;
        }
        else
        {
            Debug.LogWarning("Could not find CoM object on player!");
        }
        
        PlayerInput input = GetComponent<PlayerInput>();
        if (input)
        {
            input.ActivateInput();
            if (input.currentActionMap == null)
            {
                input.SwitchCurrentActionMap("Default");
            }

            input.currentActionMap.FindAction("Steer").performed += OnSteerAction;
            input.currentActionMap.FindAction("Accelerate").performed += OnAccelerateAction;
            input.currentActionMap.FindAction("Brake").performed += OnBrakeAction;
            input.currentActionMap.FindAction("Shift").performed += OnShiftAction;
        }

        FollowManager.Instance().FollowObject = rb;

    }

    // Input Bindings
    public void OnSteerAction(InputAction.CallbackContext context)
    {
        lastMoveInput.x = context.ReadValue<float>();
    }

    public void OnAccelerateAction(InputAction.CallbackContext context)
    {

        lastMoveInput.y = context.ReadValue<float>();
    }

    public void OnBrakeAction(InputAction.CallbackContext context)
    {
        lastBrakeInput = context.ReadValue<float>();
    }

    public void OnShiftAction(InputAction.CallbackContext context)
    {
        gear += Mathf.RoundToInt(context.ReadValue<float>());
        Debug.Log("Shift to gear" + gear);
    }

    /*public void OnAirRollAction(InputAction.CallbackContext context)
    {

        lastMoveInput = context.ReadValue<Vector2>();
    }

    public void OnAirPitchAction(InputAction.CallbackContext context)
    {

        lastMoveInput = context.ReadValue<Vector2>();
    */

    private void Update()
    {
        //Debug.Log(rb.velocity.magnitude);

    }

    public void ApplyRotationalInput()
    {
        if(groundTimer < (leaveGroundCoyoteTime - airControlCoyoteTime) )
        {
            if (lastMoveInput.sqrMagnitude > 0)
            {
                Vector3 input = new Vector3(lastMoveInput.y * airPitchForce, 0, -lastMoveInput.x * airRollForce);
                rb.AddRelativeTorque(input, ForceMode.Force);
            }
            else
            {
                rb.angularVelocity *= 0.99f;
            }

            Debug.Log("airing");
        }
    }

    void FixedUpdate()
    {
        steerAngle = Mathf.MoveTowards(steerAngle, lastMoveInput.x, smoothTurn * Time.fixedDeltaTime);

        if (Mathf.Abs(lastMoveInput.y) <= 0.01f && Mathf.Abs(lastBrakeInput) < 0.01f)
        {
            effectiveBrakeInput = 0.2f;
        }
        else
        {
            effectiveBrakeInput = lastBrakeInput;
        }

        rb.AddForce(-transform.up * rb.velocity.magnitude * 0.2f, ForceMode.Force);  // downforce

        grounded = false;

        foreach(WheelProperties w in m_wheels)
        {
            Transform wheelTransform = w.wheelObject.transform;

            wheelTransform.localRotation = Quaternion.Euler(0, w.turnAngle * steerAngle, 0);

            // Get velocity in wheel space
            w.wheelWorldPosition = transform.TransformPoint(w.localPosition);
            Vector3 velocityAtWheel = rb.GetPointVelocity(w.wheelWorldPosition);

            w.localVelocity = wheelTransform.InverseTransformDirection(velocityAtWheel);

            float lateralVelocity = Vector3.Dot(velocityAtWheel, w.wheelObject.transform.right);
            float travelVelocity = Vector3.Dot(velocityAtWheel, w.wheelObject.transform.forward);

            float idealLateralForce = wheelGripX * rb.mass * (-lateralVelocity / Time.fixedDeltaTime);
            float idealTravelForce = wheelGripZ * rb.mass * (-travelVelocity - w.angularVelocity * w.wheelCircumference) / Time.fixedDeltaTime;

            // Theoretical force we can apply if this wheel is on the ground with no slipping
            Vector3 idealLocalForce = new Vector3(
                idealLateralForce,
                0f,
                idealTravelForce
            ) * Time.fixedDeltaTime;

            //float currentMaxLateralForce = wheelGripX * w.normalForce * (w.slidding ? w.lateralKineticFriction : w.lateralStaticFriction);
            //float currentMaxTravelForce = wheelGripZ * w.normalForce * (w.slidding ? w.travelKineticFriction : w.travelStaticFriction);
            float currentMaxLateralForce = wheelGripX * (w.slidding ? MinKineticLateralForce : MaxStaticLateralForce);
            float currentMaxTravelForce = wheelGripZ * (w.slidding ? MinKineticTravelForce : MaxStaticTravelForce);

            //float idealForceMagnitude = idealLocalForce.magnitude;
            //Debug.Log("MAX: " + currentMaxFrictionForce + " Ideal: " + idealForceMagnitude);

            // Calculate effect of friction
            w.slidding = (Mathf.Abs(idealLateralForce) > currentMaxLateralForce) || (Mathf.Abs(idealTravelForce) > currentMaxTravelForce);
            w.lateralSlip = 0;
            w.travelSlip = 0;

            Vector3 appliedLocalForce = idealLocalForce;

            if (w.slidding)
            {
                w.lateralSlip = Mathf.Abs(idealLateralForce) / currentMaxLateralForce;
                w.travelSlip = Mathf.Abs(idealTravelForce) / currentMaxTravelForce;

                float slipFactor = Mathf.Max(w.lateralSlip, w.travelSlip);
                //appliedLocalForce *= slipFactor;// w.lateralGripCurve.Evaluate(slipFactor);
                appliedLocalForce.x *= w.lateralSlipFactor / Mathf.Clamp(w.lateralSlip * 0.1f, 1, 3);
                appliedLocalForce.z *= 1 / Mathf.Clamp(w.travelSlip * 0.1f, 1, 3);
                //appliedLocalForce.x = Mathf.Sign(appliedLocalForce.x) * Mathf.Min(Mathf.Abs(appliedLocalForce.x), currentMaxLateralForce);// * w.lateralGripCurve.Evaluate(w.lateralSlip);
                //appliedLocalForce.z = Mathf.Sign(appliedLocalForce.z) * Mathf.Min(Mathf.Abs(appliedLocalForce.z), currentMaxTravelForce);// * w.travelGripCurve.Evaluate(w.travelSlip);
            }
            

            Vector3 appliedWorldForce = wheelTransform.TransformDirection(appliedLocalForce);
            w.worldSlipDirection = appliedWorldForce;

            // Torque calculations for next frame
            w.torque = w.engineTorque * lastMoveInput.y;
            float inertia = Mathf.Max(w.mass * w.size * w.size / 2f, 1f);
            w.angularVelocity += ((-w.torque + appliedLocalForce.z / w.wheelCircumference) / inertia) * Time.fixedDeltaTime;
            w.braking = effectiveBrakeInput;
            w.angularVelocity *= 1 - w.braking * w.brakeStrength * Time.fixedDeltaTime;
            // Clamp to max speed
            w.angularVelocity = Mathf.Clamp(w.angularVelocity, -MaxSpeed / w.wheelCircumference, MaxSpeed / w.wheelCircumference);

            RaycastHit hit;
            if (Physics.Raycast(w.wheelWorldPosition, -transform.up, out hit, w.size * 2f, (~LayerMask.GetMask("Player") & ~LayerMask.GetMask("NonCollidable")) ))
            {
                grounded = true;

                // Spring Force
                { 
                    float restDist = w.size * 2f;
                    float compression = restDist - hit.distance; // how much the spring is compressed
                    float damping = (w.lastSuspensionLength - hit.distance) * DamperStrength; // damping is difference from last frame
                    w.normalForce = (compression + damping) * SpringStrength;
                    w.normalForce = Mathf.Clamp(w.normalForce, 0f, SpringClamp); // clamp it

                    Vector3 springDir = hit.normal * w.normalForce; // direction is the surface normal
                    w.suspensionForceDirection = springDir;

                    //appliedLocalForce

                    rb.AddForceAtPosition(springDir + appliedWorldForce, hit.point); // Apply total forces at contact
                }

                w.lastSuspensionLength = hit.distance; // store for damping next frame
                wheelTransform.position = hit.point + transform.up * w.size; // Move wheel visuals to the contact point + offset

                
                // ---- Skid marks ----
                if (w.slidding)
                {
                    if (w.skidTrail != null && w.skidParticles != null && !w.skidTrail.emitting)
                    {
                        // Continue emitting and update its position to the contact point.
                        w.skidTrail.emitting = true;
                        w.skidParticles.Play();
                    }
                }
                else if (w.skidTrail != null && w.skidParticles != null && w.skidTrail.emitting)
                {
                    // Stop emitting and detach the skid trail so it remains in the scene to fade out.
                    w.skidTrail.emitting = false;
                    w.skidParticles.Stop();
                }
                
            }
            else
            {
                wheelTransform.position = w.wheelWorldPosition - transform.up * w.size; // If not hitting anything, just position the wheel under the local anchor
                w.normalForce = 0;
                
                // If wheel is off ground stop skid effects
                if (w.skidTrail != null && w.skidParticles != null && w.skidTrail.emitting)
                {
                    w.skidTrail.emitting = false;
                    w.skidParticles.Stop();
                }
                
            } // End physics raycast section

            w.wheelObject.transform.GetChild(0).Rotate(Vector3.right, -w.angularVelocity * Mathf.Rad2Deg * Time.fixedDeltaTime, Space.Self);

        } // End Wheel Calculations
        
        if(!grounded)
        {
            groundTimer = Mathf.Min(leaveGroundCoyoteTime, groundTimer - Time.fixedDeltaTime);
            //ApplyRotationalInput();
        }
        else
        {
            groundTimer = Mathf.Max(-hitGroundCoyoteTime, groundTimer + Time.fixedDeltaTime);
        }
    }

}
