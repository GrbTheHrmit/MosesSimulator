using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float MaxMoveSpeed = 10f;
    [SerializeField]
    private float MoveSpeedIncrease = 7.5f;
    [SerializeField]
    private float ReleaseSpeedDecrease = 2f;
    [SerializeField]
    [Tooltip("Measured in Degrees per Second")]
    private float RotationSpeed = 120;
    [SerializeField]
    private float RotationDeadzone = 5;

    private Vector2 lastMoveInput;
    private Rigidbody rigidbody;

    private void Awake()
    {
        PlayerInput input = GetComponent<PlayerInput>();
        if(input)
        {
            input.ActivateInput();
            if(input.currentActionMap == null)
            {
                input.SwitchCurrentActionMap("Default");
            }

            input.currentActionMap.FindAction("Walk").performed += OnWalkAction;
        }
    }

    // Input Bindings
    public void OnWalkAction(InputAction.CallbackContext context)
    {
        lastMoveInput = context.ReadValue<Vector2>();
    }

    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        if(!rigidbody)
        {
            Debug.LogError("Could not find Player Rigidbody");
        }
    }


    void FixedUpdate()
    {
        MovePlayer(Time.fixedDeltaTime);
        RotatePlayer(Time.fixedDeltaTime);
    }

    Vector3 GetWorldInputDirection()
    {
        return rigidbody.rotation * new Vector3(lastMoveInput.x, 0, lastMoveInput.y);
    }

    void MovePlayer(float dt)
    {
        float velocityChange = lastMoveInput.y * MoveSpeedIncrease * dt;
        if(lastMoveInput.y == 0)
        {
            velocityChange = -ReleaseSpeedDecrease * dt;
        }

        float newVelocity = Mathf.Clamp(rigidbody.velocity.magnitude + velocityChange, -0.2f * MaxMoveSpeed, MaxMoveSpeed);
        rigidbody.velocity = rigidbody.rotation * Vector3.forward * newVelocity;
    }

    void RotatePlayer(float dt)
    {
        float angle = Mathf.Rad2Deg * Mathf.Asin(lastMoveInput.x);
        if(Mathf.Abs(angle) > RotationDeadzone)
        {
            Vector3 inputDirection = new Vector3(lastMoveInput.x, 0, lastMoveInput.y);
            float rotationChange = Mathf.Clamp(angle, -RotationSpeed * dt, RotationSpeed * dt);
            rigidbody.MoveRotation(Quaternion.Euler(0, rotationChange, 0) * rigidbody.rotation);
        }
    }
}
