using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointManager : MonoBehaviour
{

    public enum DegreeIntervals
    {
        D_180,
        D_360,
        D_720,
        D_1080,
    }

    /*
    [System.Serializable]
    public struct TrickComponent
    {
        public DegreeIntervals intervalReq;
        [Range(-1, 1)]
        [Tooltip("0 if direction doesnt matter, 1 for positive rotation, -1 for negative rotation")]
        public int direction;
        [Range(0,2)]
        [Tooltip("Axis to rotate around 0,1,2 -> x,y,z")]
        public int axis;
    }


    [System.Serializable]
    public struct TrickDefinition
    {
        public string Name;
        public float points;
        public List<TrickComponent> Components;
    }*/

    private GameObject playerObject;
    private PlayerCarMovement playerMovement;

    [SerializeField]
    private float MaxMultiplier = 5;
    [SerializeField]
    private float MultiIncreasePerFollower = 0.15f;
    [Tooltip("Max time allowed on ground before trick cancels")]
    private float GroundTimeout = 1.5f;

    [SerializeField]
    private float ChainMultiIncrease = 0.5f;
    [SerializeField]
    [Tooltip("Max time allowed after a trick ends to chain into another")]
    private float MaxChainTime = 3;

    [SerializeField]
    private int PointLossPerDrop = 500;

    /*[SerializeField]
    private List<TrickDefinition> trickList = new List<TrickDefinition>();
    private List<float> yRotations = new List<float>();*/

    //
    private bool tricking = false;
    private int points = 0;
    private double multiplier = 1;
    private double chainMulti = 1;

    private float groundTimer = 0;

    private Vector3Int spinDirection = Vector3Int.zero;
    private Vector3 spinAmount = Vector3.zero;
    private Quaternion lastRotation = Quaternion.identity;
    private List<float> yRotations = new List<float>();

    public void AddFollower()
    {
        multiplier += MultiIncreasePerFollower;
        playerMovement.UIController.SetMulitplier(multiplier);
    }

    public void SubtractFollower(int numLost)
    {
        ResetMultiplier();

        points -= numLost * PointLossPerDrop;
    }

    public void ResetMultiplier()
    {
        multiplier = 1;
        playerMovement.UIController.SetMulitplier(multiplier);
    }

    void Start()
    { 
        playerMovement = GetComponent<PlayerCarMovement>();
        if(playerMovement == null )
        {
            Debug.LogError("Point Manager could not find player movement script");
        }

        lastRotation = transform.rotation;
        FollowManager.Instance().PlayerPointManager = this;
    }

    public void UpdatePoints()
    {
        if (playerMovement == null) return;

        Vector3 eulerDiff = transform.rotation.eulerAngles - lastRotation.eulerAngles;

        for (int i = 0; i < 3; i++)
        {
            float diff = eulerDiff[i];
            if(Mathf.Abs(diff) > 180)
            {
                diff += -360 * Mathf.Sign(diff);
                eulerDiff[i] = diff;
            }
        }

        spinAmount += eulerDiff;
        lastRotation = transform.rotation;

        if(tricking && Mathf.Abs(spinAmount.y) > 180)
        {
            if (yRotations.Count > 0 && Mathf.Sign(spinAmount.y) == Mathf.Sign(yRotations[yRotations.Count - 1]))
            {
                yRotations[yRotations.Count - 1] += spinAmount.y;
            }
            else
            {
                yRotations.Add(spinAmount.y);
            }

            Debug.Log("Last Trick: " + yRotations[yRotations.Count - 1]);

            spinAmount.y -= 180 * Mathf.Sign(spinAmount.y);
        }

        // Player movement script handles ground coyote times
        if(playerMovement.IsGrounded)
        {
            groundTimer += Time.fixedDeltaTime;
        }
        else
        {
            groundTimer = 0;
            tricking = true;
        }

        bool crashed = false;
        // TODO: Some crash logic including resetting multiplier

        // Reset spin tracker on ground timeout or crashing
        if(groundTimer >= GroundTimeout || crashed)
        {
            float rotations = Mathf.RoundToInt(2 * Mathf.Abs(spinAmount.y) / 360) / 2f;
            CalculatePoints();

            spinAmount = Vector3.zero;
            tricking = false;
        }

        //Debug.Log(spinAmount);
    }

    private void CalculatePoints()
    {
        /*
        if(rotations > 0.5f)
        {
            points += (int)(rotations * 1000 * multiplier);
            playerMovement.UIController.SetPoints(points);

            Debug.Log("Rotations Completed: " + rotations);
            // TODO: some VFX
        }
        */
        yRotations.Clear();
    }

}
