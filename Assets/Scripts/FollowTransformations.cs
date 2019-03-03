using UnityEngine;
using System;

/// <summary>
/// Script that allows the containing game object to follow the movements of another game object.
/// </summary>
public class FollowTransformations : MonoBehaviour {
    
    /// <summary>
    /// The game object to follow.
    /// </summary>
    public GameObject leader;
    
    /// <summary>
    /// Sets how to follow the leader's translation.
    /// </summary>
    public MovementMode movementMode = MovementMode.Default;
    public enum MovementMode
    {
        None = 0,
        Default = 1,
        ToScale = 2
    }

    private Vector3 scaleFactor;
    private Vector3 lastLeaderPos;
    
	void Start () {
        // determine scaling factor and initial leader position
        scaleFactor = ElementWiseDivision(transform.localScale, leader.transform.localScale);
        lastLeaderPos = leader.transform.position;
    }
	
	void Update () {

        // follow rotation
        transform.rotation = leader.transform.rotation;

        // follow translation
        if (movementMode == MovementMode.Default)
        {
            transform.position += leader.transform.position - lastLeaderPos;
        }
        else if (movementMode == MovementMode.ToScale)
        {
            transform.position += Vector3.Scale(scaleFactor, leader.transform.position - lastLeaderPos);
        }
        lastLeaderPos = leader.transform.position;

        // follow scaling
        transform.localScale = Vector3.Scale(scaleFactor, leader.transform.localScale);
    }

    /// <summary>
    /// Performs element-wise division of two 3-dimensional vectors.
    /// </summary>
    Vector3 ElementWiseDivision(Vector3 a, Vector3 b)
    {
        if (b[0] == 0 || b[1] == 0 || b[2] == 0)
            throw new Exception("Trying to divide by zero in element wise division");

        return new Vector3(a[0] / b[0], a[1] / b[1], a[2] / b[2]);
    }
}