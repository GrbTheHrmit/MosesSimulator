
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

    [HideInInspector] public Vector3 localPosition;
    public float turnAngle = 30f;

    public float lastSuspensionLength = 0.0f;
    [HideInInspector] public float mass = 16f;
    [HideInInspector] public float size = 0.5f;
    public float engineTorque = 40f; // Engine power in Nm to wheel
    public float brakeStrength = 0.5f; // Brake torque
    public bool slidding = false;
    [HideInInspector] public Vector3 worldSlipDirection;
    [HideInInspector] public Vector3 suspensionForceDirection;
    [HideInInspector] public Vector3 wheelWorldPosition;
    [HideInInspector] public float wheelCircumference;
    [HideInInspector] public float torque = 0.0f;
    [HideInInspector] public GameObject wheelObject;
    [HideInInspector] public Vector3 localVelocity;
    [HideInInspector] public float normalForce;
    [HideInInspector] public float angularVelocity; // rad/sec
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
    public float coefStaticFriction = 2.95f;
    public float coefKineticFriction = 0.85f;
    public float wheelGripX = 8f;
    public float wheelGripZ = 42f;
    public float suspensionForce = 90f;// spring constant
    public float dampAmount = 2.5f;// damping constant
    public float suspensionForceClamp = 200f;// cap on total suspension force

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
            string name = children[i].name;
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
            }
        }

        int wheelIdx = 0;
        foreach (WheelProperties wheel in m_wheels)
        {
            //wheel.wheelObject = m_wheelObjs[wheelIdx]; // Assign the object we found earlier
            wheel.wheelObject = m_wheelObjs[wheelIdx].gameObject;
            wheelIdx++;
            wheel.size = wheel.wheelObject.transform.localScale.magnitude * 0.5f;
            //wheel.wheelObject.transform.localScale = 2f * new Vector3(wheel.size, wheel.size, wheel.size);
            wheel.wheelCircumference = 2f * Mathf.PI * wheel.size; // Calculate wheel circumference for rotation logic

            wheel.localPosition = wheel.wheelObject.transform.localPosition; // Setup visual match then translate to this

            /*
            // Instantiate and setup the skid trail (if a prefab is assigned)
            if (skidMarkPrefab != null)
            {
                GameObject skidTrailObj = Instantiate(skidMarkPrefab, transform);
                // Parent it to the wheel so its position can be updated relative to it
                skidTrailObj.transform.SetParent(w.wheelObject.transform);
                // Optionally, reset local position if needed
                skidTrailObj.transform.localPosition = Vector3.zero;
                w.skidTrail = skidTrailObj.GetComponent<TrailRenderer>();
                if (w.skidTrail != null)
                {
                    w.skidTrail.emitting = false; // start with emission off
                }
            }
            */
        }
        rb.centerOfMass = rb.centerOfMass + new Vector3(0, -1.5f, 0); // Adjust center of mass for better handling

        

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
        Debug.Log(rb.velocity.magnitude);

    }

    void FixedUpdate()
    {
        // Credit to https://github.com/SimonVutov/SimpleCar2.git for basic setup
        rb.AddForce(-transform.up * rb.velocity.magnitude * 0.2f, ForceMode.Force);  // downforce

        foreach(WheelProperties w in m_wheels)
        {
            Transform wheelTransform = w.wheelObject.transform;

            wheelTransform.localRotation = Quaternion.Euler(0, w.turnAngle * lastMoveInput.x, 0);

            // Get velocity in wheel space
            w.wheelWorldPosition = transform.TransformPoint(w.localPosition);
            Vector3 velocityAtWheel = rb.GetPointVelocity(w.wheelWorldPosition);

            w.localVelocity = wheelTransform.InverseTransformDirection(velocityAtWheel);

            w.torque = w.engineTorque * lastMoveInput.y;

            float inertia = w.mass * w.size * w.size / 2f;

            float lateralFriction = -wheelGripX * w.localVelocity.x;
            float longitudinalFriction = -wheelGripZ * (w.localVelocity.z - w.angularVelocity * w.size);

            w.angularVelocity += (w.torque - longitudinalFriction * w.size) / inertia * Time.fixedDeltaTime;
            w.angularVelocity *= 1 - w.braking * w.brakeStrength * Time.fixedDeltaTime;
            

            // Theoretical force we can apply if this wheel is on the ground
            Vector3 totalLocalForce = new Vector3(
                lateralFriction,
                0f,
                longitudinalFriction
            ) * w.normalForce * coefStaticFriction * Time.fixedDeltaTime;
            float currentMaxFrictionForce = w.normalForce * coefStaticFriction;
            Debug.Log(currentMaxFrictionForce);

            // Calculate effect of friction
            w.slidding = totalLocalForce.magnitude > currentMaxFrictionForce;
            w.slip = totalLocalForce.magnitude / currentMaxFrictionForce;
            totalLocalForce = Vector3.ClampMagnitude(totalLocalForce, currentMaxFrictionForce);
            totalLocalForce *= w.slidding ? (coefKineticFriction / coefStaticFriction) : 1;

            Vector3 totalWorldForce = wheelTransform.TransformDirection(totalLocalForce);
            w.worldSlipDirection = totalWorldForce;


            RaycastHit hit;
            if (Physics.Raycast(w.wheelWorldPosition, -transform.up, out hit, w.size * 2f, ~LayerMask.GetMask("Player")))
            {
                float rayLen = w.size * 2f; 
                float compression = rayLen - hit.distance; // how much the spring is compressed
                float damping = (w.lastSuspensionLength - hit.distance) * dampAmount; // damping is difference from last frame
                w.normalForce = (compression + damping) * suspensionForce;
                w.normalForce = Mathf.Clamp(w.normalForce, 0f, suspensionForceClamp); // clamp it

                Vector3 springDir = hit.normal * w.normalForce; // direction is the surface normal
                w.suspensionForceDirection = springDir;

                rb.AddForceAtPosition(springDir + totalWorldForce, hit.point); // Apply total forces at contact

                w.lastSuspensionLength = hit.distance; // store for damping next frame
                wheelTransform.position = hit.point + transform.up * w.size; // Move wheel visuals to the contact point + offset

                /*
                // ---- Skid marks ----
                if (w.slidding)
                {
                    // If no skid trail exists or if it was detached previously, instantiate a new one.
                    if (w.skidTrail == null && skidMarkPrefab != null)
                    {
                        GameObject skidTrailObj = Instantiate(skidMarkPrefab, transform);
                        skidTrailObj.transform.SetParent(w.wheelObject.transform);
                        skidTrailObj.transform.localPosition = Vector3.zero;
                        w.skidTrail = skidTrailObj.GetComponent<TrailRenderer>();
                        if (w.skidTrail != null)
                        {
                            w.skidTrail.emitting = true;
                        }
                    }
                    else if (w.skidTrail != null)
                    {
                        // Continue emitting and update its position to the contact point.
                        w.skidTrail.emitting = true;
                        w.skidTrail.transform.position = hit.point;
                        // Align the skid trail so its up vector is the road normal.
                        // This projects the wheel's forward direction onto the road plane to preserve skid direction.
                        Vector3 projectedForward = Vector3.ProjectOnPlane(wheelTransform.transform.forward, hit.normal).normalized;
                        w.skidTrail.transform.rotation = Quaternion.LookRotation(projectedForward, hit.normal);
                    }
                }
                else if (w.skidTrail != null && w.skidTrail.emitting)
                {
                    // Stop emitting and detach the skid trail so it remains in the scene to fade out.
                    w.skidTrail.emitting = false;
                    w.skidTrail.transform.parent = null;
                    // Optionally, destroy the skid trail after its lifetime has elapsed.
                    Destroy(w.skidTrail.gameObject, w.skidTrail.time);
                    w.skidTrail = null;
                }
                */
            }
            else
            {
                wheelTransform.position = w.wheelWorldPosition - transform.up * w.size; // If not hitting anything, just position the wheel under the local anchor
                /*
                // If wheel is off ground, detach skid trail if needed.
                if (w.skidTrail != null && w.skidTrail.emitting)
                {
                    w.skidTrail.emitting = false;
                    w.skidTrail.transform.parent = null;
                    Destroy(w.skidTrail.gameObject, w.skidTrail.time);
                    w.skidTrail = null;
                }
                */
            } // End physics raycast section

            w.wheelObject.transform.Rotate(Vector3.right, w.angularVelocity * Mathf.Rad2Deg * Time.fixedDeltaTime, Space.Self);

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
