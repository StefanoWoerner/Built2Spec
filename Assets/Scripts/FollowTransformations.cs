using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class FollowTransformations : MonoBehaviour {
    public GameObject leader;
    public bool scaleMovement = true;
    
    private Vector3 scaleFactor;
    private Vector3 lastLeaderPos;

	// Use this for initialization
	void Start () {
        scaleFactor = ElementWiseDivision(transform.localScale, leader.transform.localScale);
        lastLeaderPos = leader.transform.position;
    }
	
	// Update is called once per frame
	void Update () {

        // follow rotation
        transform.rotation = leader.transform.rotation;

        // follow movement
        if (scaleMovement)
        {
            transform.position += Vector3.Scale(scaleFactor, leader.transform.position - lastLeaderPos);
            lastLeaderPos = leader.transform.position;
        }
        else
        {
            transform.position += leader.transform.position - lastLeaderPos;
            lastLeaderPos = leader.transform.position;
        }

        // follow scaling
        transform.localScale = Vector3.Scale(scaleFactor, leader.transform.localScale);
    }

    Vector3 ElementWiseDivision(Vector3 a, Vector3 b)
    {
        if (b[0] == 0 || b[1] == 0 || b[2] == 0)
            throw new Exception("Trying to divide by zero in element wise division");

        return new Vector3(a[0] / b[0], a[1] / b[1], a[2] / b[2]);
    }
}