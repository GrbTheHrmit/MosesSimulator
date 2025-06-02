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
    private Rigidbody rb;

    [SerializeField]
    private float MaxMultiplier = 5;
    [SerializeField]
    private float MultiIncreasePerFollower = 0.15f;

    [SerializeField]
    [Tooltip("Max time allowed on ground before trick cancels")]
    private float GroundTimeout = 1.5f;

    [SerializeField]
    private float ChainMultiIncrease = 0.5f;
    [SerializeField]
    [Tooltip("Max time allowed after a trick ends to chain into another")]
    private float MaxChainTime = 3;

    [Header("Point Settings")]
    [SerializeField]
    private int TrickPointsPerSecond = 100;
    [SerializeField]
    private int FlipPoints = 3000;
    [SerializeField]
    private int SpinPoints = 2000;
    [SerializeField]
    private int RollPoints = 1000;
    [SerializeField]
    private int CornerFlipPoints = 5000;
    [SerializeField]
    private int PointLossPerDrop = 500;

    [Header("Flip Settings")]
    [SerializeField]
    [Tooltip("Degrees of rotation (per main axis half flip) required to count a second axis")]
    private float SecondaryAxisDeg = 60f;
    [SerializeField]
    [Tooltip("Degrees off of a half rotation to count")]
    private float TrickBufferDegrees = 10f;
    [SerializeField]
    private float multiAxisFactor = 1.4f;

    /*[SerializeField]
    private List<TrickDefinition> trickList = new List<TrickDefinition>();
    private List<float> yRotations = new List<float>();*/

    //
    private bool tricking = false;
    private float points = 0;
    private double multiplier = 1;
    private double chainMulti = 1;
    private float currentTrickPoints = 0;

    private float groundTimer = 0;

    private Vector3Int spinDirection = Vector3Int.zero;
    private Vector3 spinAmount = Vector3.zero;
    private List<Vector3> rotationList = new List<Vector3>();


    public void AddFollower()
    {
        multiplier += MultiIncreasePerFollower;
        playerMovement.UIController.SetMultiplier(multiplier);
    }

    public void SubtractFollower(int numLost)
    {
        ResetMultiplier();

        points -= numLost * PointLossPerDrop;
    }

    public void ResetMultiplier()
    {
        multiplier = 1;
        playerMovement.UIController.SetMultiplier(multiplier);
    }

    void Start()
    { 
        playerMovement = GetComponent<PlayerCarMovement>();
        if(playerMovement == null )
        {
            Debug.LogError("Point Manager could not find player movement script");
        }

        rb = GetComponent<Rigidbody>();
        if (playerMovement == null)
        {
            Debug.LogError("Point Manager could not find rigidbody");
        }

        FollowManager.Instance().PlayerPointManager = this;
    }

    public void UpdatePoints()
    {
        if (playerMovement == null) return;

        Vector3 eulerDiff = rb.transform.InverseTransformVector(rb.angularVelocity * Mathf.Rad2Deg * Time.fixedDeltaTime);

        if(tricking)
        {
            spinAmount += eulerDiff;
            CalculatePoints();
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
            playerMovement.UIController.ToggleTrickPoints(true);
        }

        bool crashed = false;
        // TODO: Some crash logic including resetting multiplier

        // Reset spin tracker on ground timeout or crashing
        if(tricking && (groundTimer >= GroundTimeout || crashed))
        {
            FinishTrick();
        }

        //Debug.Log(spinAmount);
    }

    private float CalculateTrickPoints(int primaryAxis, int secondaryAxis)
    {
        double trickPoints = chainMulti;

        Vector3 rotation = rotationList[rotationList.Count - 1];
        float primaryAxisRotation = rotation[primaryAxis];
        float secondaryAxisRotation = 0;
        if (secondaryAxis != -1)
        {
            secondaryAxisRotation = rotation[secondaryAxis];
        }
        


        float flipDegrees = Mathf.Abs(primaryAxisRotation) + Mathf.Abs(secondaryAxisRotation);

        // Multiply trick buffer by 2 since its per half flip
        int fullFlips = (int)( (flipDegrees + TrickBufferDegrees * 2) / (360 * (secondaryAxis == -1 ? 1 : multiAxisFactor)) );

        if(fullFlips > 0)
        {
            string trickString = "";
            chainMulti += ChainMultiIncrease;
            trickString += "Did " + fullFlips.ToString() + " ";
            if (secondaryAxis != -1)
            {
                trickString += "Corner Flip";
                trickPoints *= CornerFlipPoints;
            }
            else
            {
                switch (primaryAxis)
                {
                    case 0:
                        if (primaryAxisRotation < 0)
                        {
                            trickString += "Back";
                        }
                        else
                        {
                            trickString += "Front";
                        }
                        trickString += " Flip";
                        trickPoints *= FlipPoints;
                        break;

                    case 1:
                        if (primaryAxisRotation < 0)
                        {
                            trickString += "Counter";
                        }
                        else
                        {
                            trickString += "Clockwise";
                        }
                        trickString += " Spin";
                        trickPoints *= SpinPoints;
                        break;

                    case 2:
                        if (primaryAxisRotation < 0)
                        {
                            trickString += "Clockwise";
                        }
                        else
                        {
                            trickString += "Counter";
                        }
                        trickString += " Roll";
                        trickPoints *= RollPoints;
                        break;

                    default:
                        break;
                }
            }

            if (fullFlips != 1)
            {
                trickString += "s";
            }

            trickString += "\n" + trickPoints.ToString() + " Points!";

            Debug.Log(trickString);
        }

        return (float)trickPoints;
    }

    private void CalculatePoints()
    {
        // If in the air start with default trick points
        float addedPoints = playerMovement.IsGrounded ? 0 : TrickPointsPerSecond * Time.fixedDeltaTime;

        float primaryAxisRotation = 0;
        int primaryAxis = -1;
        float secondaryAxisRotation = 0;
        int secondaryAxis = -1;

        for (int i = 0; i < 3; i++)
        {
            if (Mathf.Abs(spinAmount[i]) > Mathf.Abs(primaryAxisRotation))
            {
                secondaryAxisRotation = primaryAxisRotation;
                secondaryAxis = primaryAxis;

                primaryAxisRotation = spinAmount[i];
                primaryAxis = i;

            }
            else if (Mathf.Abs(spinAmount[i]) > Mathf.Abs(secondaryAxisRotation))
            {
                secondaryAxisRotation = spinAmount[i];
                secondaryAxis = i;
            }
        }

        float flipDegrees = 0;
        if (primaryAxis != -1)
        {
            flipDegrees += Mathf.Abs(primaryAxisRotation);
        }

        int flips = (int)flipDegrees / 180;
        float axisFactor = 1;

        if (secondaryAxis != -1 && Mathf.Abs(secondaryAxisRotation) > SecondaryAxisDeg / Mathf.Max(flips, 1))
        {
            flipDegrees += Mathf.Abs(secondaryAxisRotation);
            axisFactor = multiAxisFactor; // Approx extra rotation required for corner flips
        }
        else
        { 
            // Reset secondary axis since the rotation isn't large enough
            secondaryAxis = -1;
        }

        bool hasTrick = flipDegrees >= (180 - TrickBufferDegrees) * axisFactor;
        bool continuedTrick = true;

        if(hasTrick)
        {
            if (rotationList.Count <= 0)
            {
                continuedTrick = false;
            }
            else
            {
                continuedTrick &= Mathf.Sign(primaryAxisRotation) == Mathf.Sign(rotationList[rotationList.Count - 1][primaryAxis]);
                
                if(continuedTrick && secondaryAxis != -1)
                {
                    // Technically we should also check if last secondary axis is valid but this is ok for now
                    continuedTrick &= Mathf.Sign(secondaryAxisRotation) == Mathf.Sign(rotationList[rotationList.Count - 1][secondaryAxis]);
                }
            }

            spinAmount += spinAmount.normalized * TrickBufferDegrees * axisFactor;

            if (continuedTrick)
            {
                rotationList[rotationList.Count - 1] += spinAmount;
            }
            else
            {
                rotationList.Add(spinAmount);
            }

            spinAmount = Vector3.zero;

            // Already multiplied by chain multi
            addedPoints += CalculateTrickPoints(primaryAxis, secondaryAxis);
        }

        currentTrickPoints += addedPoints;
        playerMovement.UIController.SetTrickPoints((int)currentTrickPoints);
        playerMovement.UIController.SetTrickMulti(chainMulti);
    }

    private void FinishTrick()
    {
        spinAmount = Vector3.zero;
        tricking = false;
        rotationList.Clear();

        points += (float)(currentTrickPoints * multiplier);
        playerMovement.UIController.SetPoints((int)points);
        currentTrickPoints = 0;
        playerMovement.UIController.SetTrickPoints((int)currentTrickPoints);
        chainMulti = 1;
        playerMovement.UIController.SetTrickMulti(chainMulti);
        playerMovement.UIController.ToggleTrickPoints(false);
        
    }

}
