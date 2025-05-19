using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


    public GameObject FocusObject;
    private PlayerCarMovement PlayerMovement;

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
            playerVelocity.y *= 0.5f;
            newPosition += Quaternion.FromToRotation(Vector3.forward, playerVelocity.normalized) * new Vector3(0, CameraHeight, -targetPlayerDist);

            if (playerVelocity.magnitude < MinMoveSpeed)
            {
                camSpeed = MinMoveSpeed;
            }
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
