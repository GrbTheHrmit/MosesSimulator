using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private float MaxSwivelAngle = 90;

    public GameObject FocusObject;
    private PlayerCarMovement PlayerMovement;

    private Vector2 cameraInput;

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
        
    }

    // Input Bindings
    public void OnCameraAction(InputAction.CallbackContext context)
    {
        cameraInput = context.ReadValue<Vector2>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 playerVelocity = PlayerMovement.GetCurrentVelocity;

        playerVelocity = Quaternion.Euler(-cameraInput.y * MaxSwivelAngle, cameraInput.x * MaxSwivelAngle, 0) * playerVelocity;


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
            lookDirection.y = Mathf.Clamp(lookDirection.y, -0.2f, 0.2f);

            if(lookDirection.magnitude <= 0.21f)
            {
                lookDirection = FocusObject.transform.forward;
                lookDirection.y = Mathf.Sign(playerVelocity.y) * 0.2f;
            }

            newPosition += Quaternion.FromToRotation(Vector3.forward, lookDirection.normalized) * new Vector3(0, CameraHeight, -targetPlayerDist);
        }
        else // No velocity
        {
            newPosition = FocusObject.transform.TransformPoint(new Vector3(0, CameraHeight, -MinDistToPlayer));
            camSpeed = 1f;
        }

        transform.position = Vector3.MoveTowards(transform.position, newPosition, camSpeed * Time.fixedDeltaTime);

        transform.LookAt(FocusObject.transform);
    }
}
