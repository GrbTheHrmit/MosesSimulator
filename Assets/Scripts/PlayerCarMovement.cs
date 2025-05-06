
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
    public float coefStaticFriction = 6f;
    public float coefKineticFriction = 2.5f;
    [Tooltip("This is the percentage of grip you can apply based on % of the max sideways velocity")]
    public AnimationCurve gripFactor;
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
    [HideInInspector] public float slip;
    [HideInInspector] public Vector2 input = Vector2.zero;// horizontal=steering, vertical=gas/brake
    [HideInInspector] public float braking = 0;
}

public class PlayerCarMovement : MonoBehaviour
{
    [SerializeField]
    private float MaxSpeed = 40f;
    [SerializeField]
    private float MoveSpeedIncrease = 20f;

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

    private static int WHEEL_FR = 0;
    private static int WHEEL_FL = 1;
    private static int WHEEL_BR = 2;
    private static int WHEEL_BL = 3;


    private Vector2 lastMoveInput;
    private bool runToggle = false;

    private Rigidbody rb;
    private Collider[] m_wheelObjs;
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

        Collider[] children = GetComponentsInChildren<Collider>();
        m_wheelObjs = new Collider[children.Length];
        //m_wheels = new WheelProperties[children.Length];

        for (int i = 0; i < children.Length; i++)
        {
            m_wheelObjs[i] = children[i];
           /* string name = children[i].name;
            if (name == "WheelFR")
            {
                m_wheelObjs[WHEEL_FR] = children[i];
            }
            else if (name == "WheelFL")
            {
                m_wheelObjs[WHEEL_FL] = children[i];
            }
            else if (name == "WheelBR")
            {
                m_wheelObjs[WHEEL_BR] = children[i];
            }
            else if (name == "WheelBL")
            {
                m_wheelObjs[WHEEL_BL] = children[i];
            }
            else
            {
                Debug.LogWarning("Unexpected Collider Found on Player: " + name);
            }*/
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
        rb.centerOfMass = rb.centerOfMass + new Vector3(0, -1.5f, 0); // + aveLocalPos; // Adjust center of mass for better handling

        

        PlayerInput input = GetComponent<PlayerInput>();
        if (input)
        {
            input.ActivateInput();
            if (input.currentActionMap == null)
            {
                input.SwitchCurrentActionMap("Default");
            }

            input.currentActionMap.FindAction("Walk").performed += OnWalkAction;
            input.currentActionMap.FindAction("Run").performed += OnRunToggle;
        }

    }

    // Input Bindings
    public void OnWalkAction(InputAction.CallbackContext context)
    {

        lastMoveInput = context.ReadValue<Vector2>();
    }

    public void OnRunToggle(InputAction.CallbackContext context)
    {
        runToggle = context.ReadValueAsButton();
    }

    private void Update()
    {
        //Debug.Log(rb.velocity.magnitude);

    }

    void FixedUpdate()
    {
        rb.AddForce(-transform.up * rb.velocity.magnitude * 0.2f, ForceMode.Force);  // downforce

        Vector3 lateralImpulse = Vector3.zero;
        foreach(WheelProperties w in m_wheels)
        {
            Transform wheelTransform = w.wheelObject.transform;

            wheelTransform.localRotation = Quaternion.Euler(0, w.turnAngle * lastMoveInput.x, 0);

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

            float currentMaxFrictionForce = w.normalForce * (w.slidding ? w.coefKineticFriction : w.coefStaticFriction);
            float idealForceMagnitude = idealLocalForce.magnitude;
            //Debug.Log("MAX: " + currentMaxFrictionForce + " Ideal: " + idealForceMagnitude);

            // Calculate effect of friction
            w.slidding = idealForceMagnitude > currentMaxFrictionForce;
            w.slip = 0;

            Vector3 appliedLocalForce = idealLocalForce;

            if(w.slidding)
            {
                w.slip = Mathf.Clamp(idealLocalForce.magnitude / currentMaxFrictionForce, 0, 1);
                appliedLocalForce *= w.gripFactor.Evaluate(w.slip);
            }

            Vector3 appliedWorldForce = wheelTransform.TransformDirection(appliedLocalForce);
            w.worldSlipDirection = appliedWorldForce;

            // Torque calculations for next frame
            w.torque = w.engineTorque * lastMoveInput.y;
            float inertia = Mathf.Max(w.mass * w.size * w.size / 2f, 1f);
            w.angularVelocity += ((-w.torque + appliedLocalForce.z / w.wheelCircumference) / inertia) * Time.fixedDeltaTime;
            w.angularVelocity *= 1 - w.braking * w.brakeStrength * Time.fixedDeltaTime;

            w.angularVelocity = Mathf.Clamp(w.angularVelocity, -MaxSpeed / w.wheelCircumference, MaxSpeed / w.wheelCircumference);

            RaycastHit hit;
            if (Physics.Raycast(w.wheelWorldPosition, -transform.up, out hit, w.size * 2f, ~LayerMask.GetMask("Player")))
            {
                // Spring Force
                { 
                    float rayDist = w.size * 2f;
                    float compression = rayDist - hit.distance; // how much the spring is compressed
                    float damping = (w.lastSuspensionLength - hit.distance) * DamperStrength; // damping is difference from last frame
                    w.normalForce = (compression + damping) * SpringStrength;
                    w.normalForce = Mathf.Clamp(w.normalForce, 0f, SpringClamp); // clamp it

                    Vector3 springDir = hit.normal * w.normalForce; // direction is the surface normal
                    w.suspensionForceDirection = springDir;

                    rb.AddForceAtPosition(springDir + appliedWorldForce, hit.point); // Apply total forces at contact
                }

                lateralImpulse += w.wheelObject.transform.TransformDirection(appliedLocalForce);

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

            //w.wheelObject.transform.Rotate(Vector3.right, w.angularVelocity * Mathf.Rad2Deg * Time.fixedDeltaTime, Space.Self);
            //Debug.Log(lateralImpulse);

        } 
        
/*
        Vector3 moveInput = gameObject.transform.rotation * new Vector3(lastMoveInput.x, 0, lastMoveInput.y) * 5;
        Vector3 carVelocity = Vector3.MoveTowards(rb.velocity, rb.velocity + moveInput, MoveSpeedIncrease * Time.fixedDeltaTime);
        carVelocity = Vector3.ClampMagnitude(carVelocity, MaxSpeed);

        Vector3 velocityDiff = carVelocity - rb.velocity;
        rb.AddForce(velocityDiff, ForceMode.VelocityChange);
*/
    }

}
