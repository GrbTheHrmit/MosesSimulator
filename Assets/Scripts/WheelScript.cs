using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelScript : MonoBehaviour
{
    PlayerCarMovement my_movement;
    // Start is called before the first frame update
    void Start()
    {
        my_movement = GetComponentInParent<PlayerCarMovement>();
    }

}
