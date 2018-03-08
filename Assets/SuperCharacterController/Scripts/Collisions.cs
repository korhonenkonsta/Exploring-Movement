using UnityEngine;
using System.Collections.Generic;

public class Collisions : MonoBehaviour {

	#region Variables
    public PlayerMachine p;
	#endregion
	
	void Start() 
	{
        p = transform.parent.GetComponent<PlayerMachine>();
	}

    void OnTriggerEnter(Collider other)
    {
        print("NAME: "+other.name);
        if (other.name == "JumpUp")
        {
            p.maxJumpCount++;
        }
        else if (other.name == "WallJumpUnlock")
        {
            p.canWallJump = true;
        }
        else if (other.name == "WallRunUnlock")
        {
            p.canWallRun = true;
        }
        else if (other.name == "GrappleUnlock")
        {
            p.canGrapple = true;
        }
        else
        {
            p.originalWalkspeed += 3f;
            p.originalWalkAcceleration += 15f;
            p.originalJumpAcceleration += 15f * (1f / 2f);
        }
        

        print(other);
        Destroy(other.gameObject);
    }

    void Update() 
	{

	}
}
