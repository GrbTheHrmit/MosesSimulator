using Cinemachine;
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

    public GameObject LookAtPrefab;
    public GameObject FollowPrefab;
    private Transform _lookAt;
    private Transform _follow;
    private CinemachineVirtualCamera _camera;

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
        if(PlayerMovement == null)
        {
            Debug.LogError("Camera could not find player object");
        }

        _lookAt = Instantiate(LookAtPrefab).transform;
        _follow = Instantiate(FollowPrefab).transform;
        _camera = GetComponentInChildren<CinemachineVirtualCamera>();
        _camera.LookAt = _lookAt;
        _camera.Follow = _follow;

        PlayerInput input = PlayerMovement.GetComponent<PlayerInput>();
        if(input != null )
        {
            input.currentActionMap.FindAction("Camera").performed += OnCameraAction;
        }
        
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

        // Move Follow position based on look at position and intended offset based on velocity
        Vector3 newPosition = _lookAt.position;
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

            // If velocity is all y component, use the look forward instead
            if (lookDirection.magnitude <= 0.1f)
            {
                lookDirection = _lookAt.transform.forward;
                lookDirection.y = Mathf.Sign(playerVelocity.y) * 0.1f;
                Debug.Log("did we do this?");
            }

            // Add calculated offset (rotated by look) to the new follow position
            newPosition += Quaternion.FromToRotation(Vector3.forward, lookDirection.normalized) * new Vector3(0, CameraHeight, -targetPlayerDist);
            Debug.Log(newPosition);
        }
        else // No velocity
        {
            newPosition = _lookAt.transform.TransformPoint(new Vector3(0, CameraHeight, -MinDistToPlayer));
            camSpeed = MinMoveSpeed;
            Debug.Log("no vel");
        }
        _follow.position = Vector3.MoveTowards(_follow.position, newPosition, camSpeed * Time.fixedDeltaTime);

        _lookAt.position = PlayerMovement.transform.position;// + playerVelocity.normalized;

        //_lookAt.rotation = PlayerMovement.transform.rotation;
    }
}
