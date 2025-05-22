using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;

public class CameraControl : MonoBehaviour
{
    [SerializeField]
    private float MinDistToPlayer = 25;
    [SerializeField]
    private float MaxDistToPlayer = 35;
    [SerializeField]
    private float CameraHeight = 8;

    [SerializeField]
    private float MinMoveSpeed = 5;

    [SerializeField]
    private float HSwivelSpeed = 90;
    [SerializeField]
    private float VSwivelSpeed = 15;

    [SerializeField]
    private float MaxHSwivelAngle = 120;
    [SerializeField]
    private float MaxVSwivelAngle = 45;

    public GameObject FocusObject;
    private PlayerCarMovement PlayerMovement;

    private Vector2 lastInput;
    private Vector2 cameraInput;
    private Vector3 lastRotationVector = Vector3.zero;
    private GameObject CameraObject;

    private float lastCameraPitch = 0;

    // Start is called before the first frame update
    void Start()
    {
        PlayerMovement = FindObjectOfType<PlayerCarMovement>();

        if (FocusObject == null)
        {
            FocusObject = PlayerMovement.gameObject;
        }

        if(PlayerMovement == null)
        {
            Debug.LogError("Camera could not find player object");
        }

        transform.position = FocusObject.transform.TransformPoint(new Vector3(0, CameraHeight, -MinDistToPlayer));
        transform.LookAt(FocusObject.transform);

        PlayerInput input = PlayerMovement.GetComponent<PlayerInput>();
        if(input != null )
        {
            input.currentActionMap.FindAction("Camera").performed += OnCameraAction;
        }

        CameraObject = GetComponentInChildren<Camera>().gameObject;
        
    }

    // Input Bindings
    public void OnCameraAction(InputAction.CallbackContext context)
    {
        lastInput = context.ReadValue<Vector2>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 playerVelocity = PlayerMovement.GetCurrentVelocity;

        float velocityFactor = (playerVelocity.magnitude - MinMoveSpeed) / PlayerMovement.GetMaxSpeed;
        float targetPlayerDist = velocityFactor * MaxDistToPlayer + (1 - velocityFactor) * MinDistToPlayer;
        Vector3 newPosition = FocusObject.transform.position;
        float camSpeed = playerVelocity.magnitude + MinMoveSpeed;
        if (playerVelocity.magnitude > 1f)
        {
            if (playerVelocity.magnitude < MinMoveSpeed)
            {
                camSpeed = MinMoveSpeed;
            }

            Vector3 lookDirection = playerVelocity;
            lookDirection.Normalize();
            lookDirection.y = Mathf.Clamp(lookDirection.y, -0.1f, 0.1f);

            if(lookDirection.magnitude <= 0.21f)
            {
                lookDirection = FocusObject.transform.forward;
                lookDirection.y = Mathf.Sign(playerVelocity.y) * 0.1f;
            }

            newPosition += Quaternion.FromToRotation(Vector3.forward, lookDirection.normalized) * new Vector3(0, CameraHeight, -targetPlayerDist);
        }
        else // No velocity
        {
            newPosition = FocusObject.transform.TransformPoint(new Vector3(0, CameraHeight, -MinDistToPlayer));
            camSpeed = 1f;
        }

        transform.position = Vector3.MoveTowards(transform.position, newPosition, camSpeed * Time.fixedDeltaTime);

        // Set Rotation, Using LookAt and then resetting pitch so we can set it in the camera object
        Quaternion lastRotation = transform.rotation;
        transform.LookAt(FocusObject.transform);
        float pitch = transform.eulerAngles.x; // Record pitch for later
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0); // Only rotate around Y
        // interpValue = (HSwivelSpeed * Time.fixedDeltaTime) / Mathf.Max(Quaternion.Angle(transform.rotation, lastRotation), HSwivelSpeed * Time.fixedDeltaTime);
        //transform.rotation = Quaternion.Lerp(lastRotation, transform.rotation, interpValue); // Make sure we dont rotate further than time allows

        // Set Camera Object location and rotation based on inputs and recorded pitch
        cameraInput = Vector2.MoveTowards(cameraInput, lastInput, 3 * Mathf.Abs(lastInput.magnitude - cameraInput.magnitude) * Time.fixedDeltaTime);
        Vector3 rotationVector = new Vector3(-cameraInput.y * MaxVSwivelAngle, cameraInput.x * MaxHSwivelAngle, 0);
        Vector3 localOffset = Quaternion.Euler(0, -transform.eulerAngles.y, 0) * (PlayerMovement.transform.position - transform.position);
        localOffset.y = 0;
        Vector3 targetLocalPos = localOffset + Quaternion.Euler(rotationVector) * -localOffset;
        CameraObject.transform.localPosition = targetLocalPos;

        /*if(Mathf.Abs(pitch - lastCameraPitch) > VSwivelSpeed * Time.fixedDeltaTime)
        {
            pitch = lastCameraPitch + Mathf.Sign(pitch - lastCameraPitch) * VSwivelSpeed * Time.fixedDeltaTime;
        }*/

        CameraObject.transform.localRotation = Quaternion.Euler(rotationVector) * Quaternion.Euler(pitch, 0, 0);
        lastCameraPitch = pitch;
    }
}
