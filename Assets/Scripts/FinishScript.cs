using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinishScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            FollowManager.Instance().SaveAllFollowing();
        }
        else if (other.gameObject.tag == "Follower")
        {
            AnimatedFollowerScript follower = other.gameObject.GetComponent<AnimatedFollowerScript>();
            if (follower != null)
            {
                FollowManager.Instance().SaveFollower(follower);
            }
        }
    }
}
