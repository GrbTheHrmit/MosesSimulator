using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class InGameUIController : MonoBehaviour
{
    private GameObject SpeedoNeedle = null;
    private TextMeshProUGUI SpeedText = null;
    private float MaxSpeed = 0;
    private TextMeshProUGUI SpeedoEast = null;
    private TextMeshProUGUI SpeedoNorthEast = null;
    private TextMeshProUGUI SpeedoNorth = null;
    private TextMeshProUGUI SpeedoNorthWest = null;


    private TextMeshProUGUI GearText = null;

    private TextMeshProUGUI FollowerNumber = null;

    private TextMeshProUGUI PointNumber = null;
    private TextMeshProUGUI MultiplierNumber = null;

    // Start is called before the first frame update
    void Start()
    {
        SpeedoNeedle = transform.Find("Speedometer").Find("SpeedGauge").Find("Needle").gameObject;
        SpeedText = transform.Find("Speedometer").Find("SpeedNumber").GetComponent<TextMeshProUGUI>();
        if(SpeedText == null)
        {
            Debug.LogWarning("Could not find speed text object");
        }
        SpeedoEast = transform.Find("Speedometer").Find("NumberLabels").Find("East").GetComponent<TextMeshProUGUI>();
        SpeedoNorthEast = transform.Find("Speedometer").Find("NumberLabels").Find("NorthEast").GetComponent<TextMeshProUGUI>();
        SpeedoNorth = transform.Find("Speedometer").Find("NumberLabels").Find("North").GetComponent<TextMeshProUGUI>();
        SpeedoNorthWest = transform.Find("Speedometer").Find("NumberLabels").Find("NorthWest").GetComponent<TextMeshProUGUI>();

        GearText = transform.Find("GearIndicator").Find("GearText").GetComponent<TextMeshProUGUI>();

        FollowerNumber = transform.Find("FollowerCount").Find("FollowerNumber").GetComponent<TextMeshProUGUI>();

        PointNumber = transform.Find("PointIndicator").Find("PointText").GetComponent<TextMeshProUGUI>();
        MultiplierNumber = transform.Find("PointIndicator").Find("MultiplierText").GetComponent<TextMeshProUGUI>();

        PlayerCarMovement playermovement = FindObjectOfType<PlayerCarMovement>();
        if(playermovement != null )
        {
            MaxSpeed = playermovement.GetMaxSpeed;
            playermovement.UIController = this;

            SpeedoEast.SetText(MaxSpeed.ToString());
            SpeedoNorth.SetText((MaxSpeed * 0.5f).ToString());
            SpeedoNorthEast.SetText((MaxSpeed * 0.75f).ToString());
            SpeedoNorthWest.SetText((MaxSpeed * 0.25f).ToString());
        }

        FollowManager.Instance().GameUIController = this;
    }

    public void SetSpeed(float speed)
    {
        SpeedText.text = ((int)speed).ToString() + " MPH";

        float factor = Mathf.Max(speed / MaxSpeed, 0);
        SpeedoNeedle.transform.rotation = Quaternion.Euler(0, 0, -factor * 180);
    }

    public void SetGear(int gear)
    {
        string gearString = "G: ";
        switch(gear)
        {
            case -1:
                gearString += "R";
                break;

            case 0:
                gearString += "N";
                break;

            default:
                gearString += gear.ToString();
                break;

        }

        GearText.text = gearString;
    }

    public void SetFollowerNum(int followerCount)
    {
        FollowerNumber.text = followerCount.ToString();
    }

    public void SetPoints(int points)
    {
        PointNumber.text = points.ToString();
    }

    public void SetMulitplier(double multiplier)
    {
        MultiplierNumber.text = "x" + multiplier.ToString("0.00");
    }


}
