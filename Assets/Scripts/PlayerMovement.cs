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
    private float MaxRunSpeed = 18f;
    [SerializeField]
    private float MoveSpeedIncrease = 7.5f;
    [SerializeField]
    private float RunSpeedIncrease = 10f;
    [SerializeField]
    private float ReleaseSpeedDecrease = 2f;
    [SerializeField]
    [Tooltip("Measured in Degrees per Second")]
    private float RotationSpeed = 120;
    [SerializeField]
    private float RotationDeadzone = 5;

    [SerializeField]
    private float floatHeight = 0.5f;

    private Vector2 lastMoveInput;
    private Rigidbody rigidbody;
    private bool runToggle = false;

    private bool grounded = false;
    private Vector3 groundNormal = Vector3.up;

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

    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        if(!rigidbody)
        {
            Debug.LogError("Could not find Player Rigidbody");
        }

        FollowManager.Instance().FollowObject = rigidbody;
    }


    void FixedUpdate()
    {
        //CheckGround(Time.fixedDeltaTime);
        MovePlayer(Time.fixedDeltaTime);
        RotatePlayer(Time.fixedDeltaTime);
    }

    Vector3 GetWorldInputDirection()
    {
        return rigidbody.rotation * new Vector3(lastMoveInput.x, 0, lastMoveInput.y);
    }

    void CheckGround(float dt)
    {
        RaycastHit hit;
        Vector3 start = transform.position;
        Debug.DrawLine(start, start + 2 * -Vector3.up, Color.red);
        if (Physics.SphereCast(start, 2, -transform.up, out hit, floatHeight * 10, LayerMask.GetMask("Ground")))
        {
            groundNormal = hit.normal;

            /*if (hit.distance < floatHeight * 1.5f)
            {
                rigidbody.transform.position += groundNormal * (floatHeight - hit.distance);

                if(!grounded)
                {
                    rigidbody.useGravity = false;
                }
                grounded = true;
            }
            else
            {
                if (grounded)
                {
                    rigidbody.useGravity = true;
                }
                grounded = false;
            }*/
            
        }
        else
        {
            /*if (grounded)
            {
                rigidbody.useGravity = true;
            }*/
            grounded = false;
            groundNormal = Vector3.up;
        }

    }

    void MovePlayer(float dt)
    {
        float lastVelocity = new Vector3(rigidbody.velocity.x, 0, rigidbody.velocity.z).magnitude;
        float velocityChange = lastMoveInput.y * dt * (runToggle ? RunSpeedIncrease : MoveSpeedIncrease);
        if(lastMoveInput.y == 0)
        {
            velocityChange = -ReleaseSpeedDecrease * dt;
        }

        float newSpeed = runToggle ?
            Mathf.Clamp(lastVelocity + velocityChange, -0.2f * MaxRunSpeed, MaxRunSpeed) :
            Mathf.Clamp(lastVelocity + velocityChange, -0.2f * MaxMoveSpeed, MaxMoveSpeed);

        newSpeed = Mathf.MoveTowards(lastVelocity, newSpeed, ReleaseSpeedDecrease * dt);

        Vector3 moveForward = Quaternion.FromToRotation(transform.up, groundNormal) * transform.forward;
        Vector3 velocityVector = moveForward * newSpeed;

        if (!grounded)
        {
            velocityVector.y = rigidbody.velocity.y;
        }

        rigidbody.velocity = velocityVector;
    }

    void RotatePlayer(float dt)
    {
        float angle = Mathf.Rad2Deg * Mathf.Asin(lastMoveInput.x);
        if(Mathf.Abs(angle) > RotationDeadzone)
        {
            Vector3 inputDirection = new Vector3(lastMoveInput.x, 0, lastMoveInput.y);
            float rotationChange = Mathf.Clamp(angle, -RotationSpeed * dt, RotationSpeed * dt);
            //Quaternion groundRotation = Quaternion.FromToRotation(transform.up, groundNormal);
            rigidbody.MoveRotation(Quaternion.RotateTowards(rigidbody.rotation, Quaternion.Euler(0, rotationChange, 0) * rigidbody.rotation, RotationSpeed * dt));
        }
    }

}
