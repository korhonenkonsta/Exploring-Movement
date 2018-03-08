using UnityEngine;
using System.Collections.Generic;

public class BoostItem : MonoBehaviour {

    #region Variables
    public Vector3 originalPosition;
    public Vector3 direction;
    #endregion

    void Start() 
	{
        originalPosition = transform.position;
        direction = Vector3.up;
    }

    void Update() 
	{
        transform.Rotate(transform.up, 0.5f);

        if (transform.position.y < originalPosition.y - 0.2f)
        {
            direction = Vector3.up;
        }

        if (transform.position.y > originalPosition.y + 0.2f)
        {
            direction = Vector3.down;
        }

        transform.Translate(direction * Time.deltaTime * 0.25f);
    }
}
