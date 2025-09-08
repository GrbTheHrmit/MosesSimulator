using Cinemachine;
using Cinemachine.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;

public class CameraControl : MonoBehaviour
{
    [SerializeField]
    private float MinCamMoveSpeed = 15;
    [SerializeField]
    private float MinPlayerMoveSpeed = 5;

    [SerializeField]
    private float MinDistToPlayer = 25;
    [SerializeField]
    private float MaxDistToPlayer = 35;
    [SerializeField]
    private float CameraHeight = 8;
    [SerializeField]
    private float LookDist = 2;

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

    private bool reverseCamera = false;
    private bool flipCamera = false;
    private Vector2 lastInput;
    private Vector3 cameraRotation = Vector3.zero;
    private Vector3 lastLookUnrotated = Vector3.zero;
    private Vector3 lastFollowUnrotated = Vector3.zero;
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
        lastLookUnrotated = PlayerMovement.transform.TransformPoint(0, 0, LookDist);
        _lookAt.position = lastLookUnrotated;

        _follow = Instantiate(FollowPrefab).transform;
        lastFollowUnrotated = PlayerMovement.transform.TransformPoint(0, CameraHeight, -MinDistToPlayer);
        _follow.position = lastFollowUnrotated;

        _camera = GetComponentInChildren<CinemachineVirtualCamera>();
        _camera.LookAt = _lookAt;
        _camera.Follow = _follow;

        PlayerInput input = PlayerMovement.GetComponent<PlayerInput>();
        if(input != null )
        {
            input.currentActionMap.FindAction("Camera").performed += OnCameraAction;
            input.currentActionMap.FindAction("ReverseCamera").performed += OnCameraReverse;
            input.currentActionMap.FindAction("ReverseCamera").canceled += OnCameraReverse;
        }
        
    }

    // Input Bindings
    public void OnCameraAction(InputAction.CallbackContext context)
    {
        lastInput = context.ReadValue<Vector2>();
    }

    public void OnCameraReverse(InputAction.CallbackContext context)
    {
        Debug.Log(context.ReadValue<float>());
        bool shouldReverse = context.ReadValue<float>() > 0.5f;
        if(reverseCamera != shouldReverse)
        {
            reverseCamera = shouldReverse;
            flipCamera = true;
        }

    }

    // Update is called once per frame
    void FixedUpdate()
    {

        MoveCamera();
        AddCameraSwivel();
    }

    private void MoveCamera()
    {
        Vector3 playerVelocity = PlayerMovement.GetCurrentVelocity;

        float velocityFactor = (playerVelocity.magnitude - MinPlayerMoveSpeed) / PlayerMovement.GetMaxSpeed;
        float targetPlayerDist = velocityFactor * MaxDistToPlayer + (1 - velocityFactor) * MinDistToPlayer;

        // Move Follow position based on look at position and intended offset based on velocity
        Vector3 newLookPosition = PlayerMovement.transform.position;
        Vector3 newFollowPosition = PlayerMovement.transform.position;
        float camSpeed = playerVelocity.magnitude + MinCamMoveSpeed;
        if (playerVelocity.magnitude > MinPlayerMoveSpeed)
        {

            Vector3 followDirection = playerVelocity;
            followDirection.Normalize();
            // Minimize the y component of the follow offset
            followDirection.y = Mathf.Clamp(followDirection.y, -0.1f, 0.1f);

            // If velocity is all y component, use the player forward instead
            if (followDirection.magnitude <= 0.1f)
            {
                followDirection = PlayerMovement.transform.forward;
                followDirection.y = Mathf.Sign(playerVelocity.y) * 0.1f;
            }

            // Look location should always be in direction of motion
            newLookPosition += playerVelocity.normalized * LookDist * (reverseCamera ? -1 : 1);
            // Follow location is based on the above calculated direction
            newFollowPosition += Quaternion.FromToRotation(Vector3.forward, followDirection.normalized) * new Vector3(0, CameraHeight, -targetPlayerDist * (reverseCamera ? -1 : 1));
        }
        else // No velocity
        {
            newLookPosition = PlayerMovement.transform.TransformPoint(0, 0, LookDist * (reverseCamera ? -1 : 1));
            newFollowPosition = PlayerMovement.transform.TransformPoint(0, CameraHeight, -targetPlayerDist * (reverseCamera ? -1 : 1));
        }

        if(!flipCamera)
        {
            lastFollowUnrotated = Vector3.MoveTowards(lastFollowUnrotated, newFollowPosition, camSpeed * Time.fixedDeltaTime);
            lastLookUnrotated = Vector3.MoveTowards(lastLookUnrotated, newLookPosition, camSpeed * Time.fixedDeltaTime);
        }
        else // Flipping camera should happen instantly
        {
            flipCamera = false;
            // Need to do this first because using MoveTowards messes with rotational movement
            lastFollowUnrotated = newFollowPosition;
            lastLookUnrotated = newLookPosition;
        }

        _follow.position = lastFollowUnrotated;
        _lookAt.position = lastLookUnrotated;
    }

    private void AddCameraSwivel()
    {
        // Get the swivel rotation vector
        Vector3 rotationVector = new Vector3(-lastInput.y * MaxVSwivelAngle, lastInput.x * MaxHSwivelAngle, 0);

        // Adjust camera swivel inputs based on last input
        cameraRotation.x = Mathf.MoveTowards(cameraRotation.x, rotationVector.x, VSwivelSpeed * Time.fixedDeltaTime);
        cameraRotation.y = Mathf.MoveTowards(cameraRotation.y, rotationVector.y, HSwivelSpeed * Time.fixedDeltaTime);

        Vector3 followOffset = Quaternion.Euler(cameraRotation * (reverseCamera ? -1 : 1)) * PlayerMovement.transform.InverseTransformPoint(lastFollowUnrotated);
        Vector3 lookOffset = Quaternion.Euler(cameraRotation * (reverseCamera ? -1 : 1)) * PlayerMovement.transform.InverseTransformPoint(lastLookUnrotated);
        _follow.position = PlayerMovement.transform.TransformPoint(followOffset);
        _lookAt.position = PlayerMovement.transform.TransformPoint(lookOffset);
    }
}
