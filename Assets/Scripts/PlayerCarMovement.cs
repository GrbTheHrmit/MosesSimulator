
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


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
    private Collider[] m_wheels;
    private float[] wheelSpringVelocity;

    private float currentWheelSpeed = 0;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();


        Collider[] children = GetComponentsInChildren<Collider>();
        m_wheels = new Collider[children.Length];
        wheelSpringVelocity = new float[children.Length];

        for(int i = 0;  i < children.Length; i++)
        {
            string name = children[i].name;
            if(name == "WheelFR")
            {
                m_wheels[WHEEL_FR] = children[i];
            }
            else if (name == "WheelFL")
            {
                m_wheels[WHEEL_FL] = children[i];
            }
            else if (name == "WheelBR")
            {
                m_wheels[WHEEL_BR] = children[i];
            }
            else if (name == "WheelBL")
            {
                m_wheels[WHEEL_BL] = children[i];
            }
            else
            {
                Debug.LogWarning("Unexpected Collider Found on Player: " + name);
            }
        }

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



    void FixedUpdate()
    {

        for (int i = m_wheels.Length - 1; i >= 0; i--)
        {
            // Spring Height
            {
                Vector3 newPosition = m_wheels[i].transform.localPosition;

                if (Mathf.Abs(wheelSpringVelocity[i]) > 0.001f || Mathf.Abs(newPosition.y) > 0.001f)
                {
                    float wheelHeight = m_wheels[i].transform.localPosition.y;
                    float springForce = -wheelHeight * SpringStrength; // Replace with variable later
                    float newWheelVelocity = (wheelSpringVelocity[i] * DamperStrength) + springForce * Time.fixedDeltaTime;
                    wheelSpringVelocity[i] = newWheelVelocity;

                    newPosition += new Vector3(0, newWheelVelocity * Time.fixedDeltaTime, 0);

                    if (Mathf.Abs(newPosition.y) > MaxSpringExtension)
                    {
                        newPosition.y = Mathf.Clamp(newPosition.y, -MaxSpringExtension, MaxSpringExtension);
                        wheelSpringVelocity[i] = 0;
                    }
                }

                if (i < 2)
                {
                    m_wheels[i].transform.SetLocalPositionAndRotation(newPosition, Quaternion.Euler(0, lastMoveInput.x * MaxTurnAngle, 0));
                }
                else
                {
                    m_wheels[i].transform.localPosition = newPosition;
                }
            }

            // Movement Input
            {
                // Rotate input by car and wheel orientation
                Vector3 moveInput = new Vector3(0, 0, lastMoveInput.y) * MoveSpeedIncrease;
                //Vector3 moveInput = velocityChange;

                //Vector3 pointVelocity = rb.GetPointVelocity(m_wheels[i].transform.position);
                if (moveInput.sqrMagnitude > 0)
                {
                    if (i < 2)
                    {
                        moveInput = gameObject.transform.rotation * Quaternion.Euler(0, lastMoveInput.x * MaxTurnAngle, 0) * moveInput;

                    }
                    else
                    {
                        moveInput *= 0.25f;
                        moveInput = gameObject.transform.rotation * moveInput;
                    }
                    moveInput.y = 0;
                }


                //Vector3 wheelVelocity = Vector3.MoveTowards(pointVelocity, pointVelocity + moveInput, MoveSpeedIncrease * Time.fixedDeltaTime);

                //Vector3 velocityDiff = wheelVelocity - pointVelocity;
                Vector3 XZOffset = m_wheels[i].transform.localPosition;
                XZOffset.y = 0;
                rb.AddForceAtPosition(moveInput, m_wheels[i].transform.position, ForceMode.Acceleration);
                Debug.DrawLine(m_wheels[i].transform.position, m_wheels[i].transform.position + moveInput, Color.red, 2);
            }
        }
        
/*
        Vector3 moveInput = gameObject.transform.rotation * new Vector3(lastMoveInput.x, 0, lastMoveInput.y) * 5;
        Vector3 carVelocity = Vector3.MoveTowards(rb.velocity, rb.velocity + moveInput, MoveSpeedIncrease * Time.fixedDeltaTime);
        carVelocity = Vector3.ClampMagnitude(carVelocity, MaxSpeed);

        Vector3 velocityDiff = carVelocity - rb.velocity;
        rb.AddForce(velocityDiff, ForceMode.VelocityChange);
*/
    }

    private void OnCollisionEnter(Collision collision)
    {
        int idx = StringToWheelIndex(collision.contacts[0].thisCollider.name);

        wheelSpringVelocity[idx] += collision.impulse.y;
    }

}
