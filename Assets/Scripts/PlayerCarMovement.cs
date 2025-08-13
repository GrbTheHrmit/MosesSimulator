
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
    public float torqueFactor = 1.0f;
    public float brakeFactor = 1.0f;
    public float lateralSlipFactor = 0.12f;
    public float travelSlipFactor = 0.12f;
    public float maxLateralSlipDivisor = 3.5f;
    public float maxTravelSlipDivisor = 3.5f;
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
    private float engineTorque = 40f; // Engine power in Nm to wheel
    [SerializeField]
    private float brakeStrength = 0.5f; // Brake torque

    [SerializeField]
    [Tooltip("Brake factor when accel and brake inputs are 0")]
    private float ReleaseBrake = 0.2f;

    [SerializeField]
    private float drag = 0.1f;
    [SerializeField]
    private float airDragFactor = 2f;
    [SerializeField]
    private float airForceFactor = 0.1f;

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
    private float SpringClamp = 20000f;
    [SerializeField]
    [Tooltip("How far from max compression when we use the max clamp value")]
    private float MaxSpringBuffer = 0.02f;
    [SerializeField]
    [Tooltip("How fast the spring is moving at max compression when we use max clamp value")]
    private float MaxSpringSpeed = 2f;
    [SerializeField]
    private float ReturnSpringStrength = 300f;
    [SerializeField]
    private float ReturnSpringClamp = 10000f;
    [SerializeField]
    [Tooltip("How fast the spring must move to trigger lower spring str")]
    private float ReturnSpringDistCutoff = 3f;
    [SerializeField]
    private float DamperStrength = 0.95f;
    [SerializeField]
    private float ReturnDamperStrength = 85f;
    

    [SerializeField]
    private float MaxTurnAngle = 30f;

    [SerializeField]
    private float MaxSpringExtension = 1f;

    [SerializeField]
    private float leaveGroundCoyoteTime = 0.1f;
    [SerializeField]
    private float hitGroundCoyoteTime = 0.1f;
    [SerializeField]
    private int numWheelsForGround = 2;

    private float groundTimer = 0f;
    private bool grounded = false;
    public bool IsGrounded { get { return groundTimer >= 0; } }

    [Header("Air Controls")]
    [SerializeField]
    private float airControlCoyoteTime = 0.25f;
    [SerializeField]
    private float releaseRotateFriction = 0.02f;
    [SerializeField]
    private float steerRotateFriction = 1f;
    [SerializeField]
    [Tooltip("Force factor applied to a wheel touching the ground when car is considered airborne")]
    private float initialGroundForceFactor = 0.2f;
    [SerializeField]
    private bool UseLocalTrickRotations = true;

    [Header("Trick Controls")]
    [SerializeField]
    private float MinTrickForce = 150;
    [SerializeField]
    private float MaxTrickForce = 500;
    [SerializeField]
    private float VerticalFlipFactor = 1.5f;
    [SerializeField]
    private float MaxTrickTime = 2f;
    [SerializeField]
    private float TrickCooldown = 0.5f;
    [SerializeField]
    private float TrickRotationCancel = 0.1f;

    [Header("Boost Controls")]
    [SerializeField]
    [Tooltip("If true the Torque will be multiplied by torque increase value instead of added on")]
    private bool MultiplyBoostIncrease = true;
    [SerializeField]
    private float BoostTorqueIncrease = 1.5f;
    [SerializeField]
    private float InitialBoostImpulse = 750f;
    [SerializeField]
    private float InitialBoostForce = 300f;
    [SerializeField]
    private float FinalBoostForce = 100f;
    [SerializeField]
    private float MaxBoostTime = 1.5f;
    [SerializeField]
    private float BoostRecoveryRate = 0.5f;

    [Header("Crash Settings")]
    [SerializeField]
    private float MinCrashImpulseSqr = 10000f;
    private float CrashLevelIntervalSqr = 500000;

    private static int WHEEL_FR = 0;
    private static int WHEEL_FL = 1;
    private static int WHEEL_BR = 2;
    private static int WHEEL_BL = 3;

    ////// INPUTS and Timers /////

    // Movement
    private Vector2 lastMoveInput;
    private float lastBrakeInput;
    private float steerAngle = 0;
    private float effectiveBrakeInput;
    private int gear = 1;

    // Tricks
    private Vector2 lastFlipDirInput;
    private bool trickInput = false;
    private bool activateTrick = false;
    private bool airFrictionInput = false;
    private float trickInputStartTime;
    private float trickCDTimer = 0;
    private float trickRotationPercent = 0;

    // Boost
    private float boostTimer = 0;
    private bool boostInput = false;
    private bool boosting = false;

    [SerializeField]
    private List<float> gearRatios = new List<float>();

    private Rigidbody rb;
    public Vector3 GetCurrentVelocity { get { return rb.velocity; } }
    private SphereCollider[] m_wheelObjs;
    public WheelProperties[] m_wheels;

    public GameObject skidMarkPrefab; // Assign a prefab with a TrailRenderer in the inspector

    public float smoothTurn = 0.03f;
    public float wheelGripX = 8f;
    public float wheelGripZ = 42f;

    private InGameUIController uiController = null;
    public InGameUIController UIController { set {  uiController = value; } get { return uiController; } }

    private PointManager pointManager = null;

    private Camera playerCamera = null;

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
        pointManager = GetComponent<PointManager>();
        if(pointManager == null)
        {
            Debug.LogError("Player Script could not find Point Manager");
        }

        SphereCollider[] children = GetComponentsInChildren<SphereCollider>();
        m_wheelObjs = new SphereCollider[children.Length];
        //m_wheels = new WheelProperties[children.Length];

        if(gearRatios.Count == 0)
        {
            gearRatios.Add(1);
        }

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
            input.currentActionMap.FindAction("Flip").performed += OnFlipAction;
            input.currentActionMap.FindAction("FlipDirection").performed += OnFlipDirectionAction;
            input.currentActionMap.FindAction("AirFriction").performed += OnAirFrictionAction;
            input.currentActionMap.FindAction("Boost").performed += OnBoostStartAction;
            input.currentActionMap.FindAction("Boost").canceled += OnBoostReleaseAction;

        }

        FollowManager.Instance().FollowObject = rb;

        playerCamera = FindObjectOfType<Camera>();
        if(playerCamera == null)
        {
            Debug.LogWarning("Could Not Find Player Camera");
        }

        // % Multiplier calculation for how much to damp the rotation each tick when charging tricks
        trickRotationPercent = Mathf.Pow(TrickRotationCancel, Time.fixedDeltaTime);
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

        gear = Mathf.Clamp(gear, -1, gearRatios.Count);
        
        if(uiController != null)
        {
            uiController.SetGear(gear);
        }
    }

    ////// BOOST FUNCTIONS //////

    public void OnBoostStartAction(InputAction.CallbackContext context)
    {
        if(!boosting && boostTimer >= 0)
        {
            StartBoosting();
        }
    }

    public void OnBoostReleaseAction(InputAction.CallbackContext context)
    {
        if(boosting)
        {
            FinishBoosting();
        }
    }

    private void StartBoosting()
    {
        rb.AddRelativeForce(new Vector3(0, 0, InitialBoostImpulse), ForceMode.Impulse);
        boosting = true;
        boostTimer = 0;
    }

    private void FinishBoosting()
    {
        boosting = false;
        boostTimer = -boostTimer;
    }

    private void UpdateBoost()
    {
        if (boostTimer < 0)
        {
            boostTimer += Time.fixedDeltaTime * BoostRecoveryRate;

            float percent = 1 - Mathf.Abs(boostTimer / MaxBoostTime);
            uiController.SetBoostPercent(percent);
        }
        else if (boosting)
        {
            boostTimer += Time.fixedDeltaTime;

            float percent = 1 - Mathf.Abs(boostTimer / MaxBoostTime);
            uiController.SetBoostPercent(percent);

            if (boostTimer > MaxBoostTime)
            {
                FinishBoosting();
            }
            else
            {
                float t = Mathf.Max(boostTimer / MaxBoostTime, 0); // Make sure this doesnt go below 0, upper bound taken care of by ifelse
                float currentBoostForce = t * FinalBoostForce + (1 - t) * InitialBoostForce;
                rb.AddRelativeForce(new Vector3(0, 0, currentBoostForce), ForceMode.Force);
            }
        }
    }

    ////// TRICK FUNCTIONS //////

    public void OnFlipAction(InputAction.CallbackContext context)
    {
        // Not on cooldown
        if (trickCDTimer <= 0)
        {
            if (context.ReadValue<float>() > 0.5f && !trickInput) // Start charge
            {
                trickInput = true;
                trickInputStartTime = Time.time;
            }
            else if (trickInput)
            {
                ActivateTrick();
            }
        }

    }

    public void OnFlipDirectionAction(InputAction.CallbackContext context)
    {
        lastFlipDirInput = context.ReadValue<Vector2>();
    }

    public void OnAirFrictionAction(InputAction.CallbackContext context)
    {
        airFrictionInput = context.ReadValue<float>() > 0.5f;
    }


    private void ActivateTrick()
    {
        trickInput = false;
        activateTrick = true;
        trickCDTimer = TrickCooldown;
        uiController.SetFlipChargePercent(0);
    }

    private void UpdateTricks()
    {
        
        if(trickCDTimer > 0)
        {
            // Tick CD
            trickCDTimer -= Time.fixedDeltaTime;
        }


        if (activateTrick)
        {
            activateTrick = false;

            float t = Mathf.Pow(Mathf.Clamp((Time.time - trickInputStartTime) / MaxTrickTime, 0, 1), 2);
            float trickForce = MinTrickForce * (1 - t) + MaxTrickForce * t;
            //float trickForce = MaxTrickForce;

            if (groundTimer < 0 && lastFlipDirInput.sqrMagnitude >= 0.01f)
            {
                Vector3 torqueVector = trickForce * (Quaternion.FromToRotation(Vector3.right, new Vector3(lastFlipDirInput.y, 0, -lastFlipDirInput.x)) * Vector3.right);
                torqueVector.x *= VerticalFlipFactor;

                // Local vs World coordinates for flipping
                if(UseLocalTrickRotations)
                {
                    rb.AddRelativeTorque(torqueVector, ForceMode.Impulse);
                }
                else
                {
                    Vector3 camForward = playerCamera.transform.forward;
                    camForward.y = 0;
                    camForward.Normalize();
                    torqueVector = Quaternion.FromToRotation(Vector3.forward, camForward) * torqueVector;
                    rb.AddTorque(torqueVector, ForceMode.Impulse);
                }
                
            }
            else if(groundTimer >= 0) // No Input Direction or on ground
            {
                rb.AddRelativeForce(new Vector3(0, trickForce, 0), ForceMode.Impulse);
            }
            
        }
        else if (trickInput) // Charging flip
        {
            rb.angularVelocity *= trickRotationPercent;
            float chargePercent = Mathf.Max((Time.time - trickInputStartTime) / MaxTrickTime, 0);
            // TODO: Something for charge ui
            uiController.SetFlipChargePercent(chargePercent);

            if (chargePercent >= 1)
            {
                // If we are still holding the input when max time hits activate on next frame
                ActivateTrick();
            }
            
        }

    }

    private float GetCurrentEngineTorque()
    {
        float torque = engineTorque;
        if(boosting)
        {
            if(MultiplyBoostIncrease)
            {
                torque *= BoostTorqueIncrease;
            }
            else
            {
                torque += BoostTorqueIncrease;
            }
        }

        return torque;
    }

    private void UpdateMovement()
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
        int groundedWheels = 0;

        float aveWheelSpeed = 0;

        foreach (WheelProperties w in m_wheels)
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
            aveWheelSpeed += -w.angularVelocity * w.wheelCircumference;

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

                appliedLocalForce.x *= 1 / Mathf.Clamp(w.lateralSlip * w.lateralSlipFactor, 1, w.maxLateralSlipDivisor);
                appliedLocalForce.z *= 1 / Mathf.Clamp(w.travelSlip * w.travelSlipFactor, 1, w.maxTravelSlipDivisor);
            }


            Vector3 appliedWorldForce = wheelTransform.TransformDirection(appliedLocalForce);
            w.worldSlipDirection = appliedWorldForce;

            // Torque calculations for next frame
            float dragTorque = drag * Vector3.Dot(rb.velocity, w.wheelObject.transform.forward); // Apply drag as negative torque relative to velocity in wheel direction
            float gearRatio = 1;
            if (gear == -1)
            {
                gearRatio = -gearRatios[0];
            }
            else if (gear == 0)
            {
                gearRatio = 0;
            }
            else
            {
                gearRatio = gearRatios[gear - 1];
            }

            w.torque = gearRatio * w.torqueFactor * GetCurrentEngineTorque() * lastMoveInput.y;
            float inertia = Mathf.Max(w.mass * w.size * w.size / 2f, 1f);

            RaycastHit hit;
            // Raycast at dist of MaxExtension + 2*wheel size to make sure we check just past the wheel
            if (Physics.Raycast(w.wheelWorldPosition, -transform.up, out hit, 2f * w.size + MaxSpringExtension, (~LayerMask.GetMask("Player") & ~LayerMask.GetMask("NonCollidable"))))
            {
                groundedWheels++;

                // Spring Force
                {
                    
                    float restDist = MaxSpringExtension + w.size;
                    float hitDist = Mathf.Min(hit.distance, restDist);
                    float compression = restDist - hitDist;
                    float returnSpeed = (w.lastSuspensionLength - hitDist);
                    
                    // Check if the spring is bottoming out and still compressing
                    if (returnSpeed > MaxSpringSpeed * Time.fixedDeltaTime && hitDist < MaxSpringBuffer)
                    {
                        w.normalForce = SpringClamp;
                        Debug.Log("Bottoming");
                    }
                    // Checks if the spring is extending fast enough for changed spring values
                    else if (returnSpeed < -ReturnSpringDistCutoff * Time.fixedDeltaTime)
                    {
                        float damping = returnSpeed * ReturnDamperStrength; // damping is difference from last frame
                        w.normalForce = (compression + damping) * ReturnSpringStrength;
                        w.normalForce = Mathf.Clamp(w.normalForce, 0f, ReturnSpringClamp);
                        //Debug.Log("Reduced Spring");
                    }
                    else
                    {
                        float damping = returnSpeed * DamperStrength; // damping is difference from last frame
                        w.normalForce = (compression + damping) * SpringStrength;
                        w.normalForce = Mathf.Clamp(w.normalForce, 0f, SpringClamp);
                    }

                    Vector3 springDir = hit.normal * w.normalForce; // direction is the surface normal
                    w.suspensionForceDirection = springDir;

                    // If the car is not on the ground limit the amount of force this wheel can apply
                    if (groundTimer < 0)
                    {
                        appliedWorldForce *= initialGroundForceFactor;
                    }

                    rb.AddForceAtPosition(springDir + appliedWorldForce, hit.point); // Apply total forces at contact
                    w.lastSuspensionLength = hitDist; // store for damping next frame
                }

                
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

                // Apply torque and drag to wheel rotation
                w.angularVelocity += ((-w.torque + dragTorque + appliedLocalForce.z / w.wheelCircumference) / inertia) * Time.fixedDeltaTime;
            }
            else
            {
                w.lastSuspensionLength = MaxSpringExtension + w.size;
                wheelTransform.position = w.wheelWorldPosition - transform.up * (w.size + MaxSpringExtension); // If not hitting anything, just position the wheel under the local anchor
                w.normalForce = 0;

                // If wheel is off ground stop skid effects
                if (w.skidTrail != null && w.skidParticles != null && w.skidTrail.emitting)
                {
                    w.skidTrail.emitting = false;
                    w.skidParticles.Stop();
                }

                // Apply torque to wheel rotation, drag is minimized by air drag factor
                w.angularVelocity += ((dragTorque * airDragFactor) + (airForceFactor * appliedLocalForce.z / w.wheelCircumference) - w.torque) / inertia * Time.fixedDeltaTime;

                w.slidding = true; // Helps to not get weird flips when landing
            } // End physics raycast section

            w.wheelObject.transform.GetChild(0).Rotate(Vector3.right, -w.angularVelocity * Mathf.Rad2Deg * Time.fixedDeltaTime, Space.Self);

            // Final Wheel speed calc (after applying forces)
            w.braking = effectiveBrakeInput;
            w.angularVelocity *= 1 - w.braking * w.brakeFactor * brakeStrength * Time.fixedDeltaTime;
            // Clamp to max speed
            w.angularVelocity = Mathf.Clamp(w.angularVelocity, -MaxSpeed / w.wheelCircumference, MaxSpeed / w.wheelCircumference);

        } // End Wheel Calculations

        if (uiController != null)
        {
            uiController.SetSpeed(aveWheelSpeed / m_wheels.Length);
        }

        grounded = groundedWheels >= numWheelsForGround;
        UpdateAirLogic(grounded);
        
    }

    private void UpdateAirLogic(bool currentlyGrounded)
    {
        if (!grounded)
        {
            groundTimer = Mathf.Min(leaveGroundCoyoteTime, groundTimer - Time.fixedDeltaTime);
            //ApplyRotationalInput();
            if (groundTimer < (leaveGroundCoyoteTime - airControlCoyoteTime))
            {
                if (airFrictionInput)
                {
                    rb.angularDrag = steerRotateFriction;
                }
                else
                {
                    rb.angularDrag = releaseRotateFriction;
                }
            }

        }
        else
        {
            groundTimer = Mathf.Max(-hitGroundCoyoteTime, groundTimer + Time.fixedDeltaTime);
            rb.angularDrag = 1f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.GetContact(0).thisCollider.tag == "CrashDetection" && collision.impulse.sqrMagnitude > MinCrashImpulseSqr && collision.gameObject.tag == "Ground")
        {
            HandleCrash(collision.impulse.sqrMagnitude);
        }
    }

    private void HandleCrash(float impulseSqr)
    {
        float crashMagnitude = (impulseSqr - MinCrashImpulseSqr) / (CrashLevelIntervalSqr - MinCrashImpulseSqr);

        pointManager.HandleCrash();
        FollowManager.Instance().HandleCrash(crashMagnitude);

        Debug.Log("Crash!! Magnitude: " + crashMagnitude);
    }

    void FixedUpdate()
    {
        UpdateMovement();
        UpdateBoost();
        UpdateTricks();
        pointManager.UpdatePoints();
    }

}
