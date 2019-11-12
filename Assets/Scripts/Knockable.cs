﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
	Note: 
    >> Any GameObject attaching the Breakable component should also be assigned a ** Layer "Knockable" **.
	>> Any GameObject using the Knockable component should also have a ** RigidBody ** component.
	>> The RigidBody component must have useGravity checked.
*/
public class Knockable : MonoBehaviour
{
    [Header("Object Attributes")]
    // public float firstTimeScorePoint;
    // public float regularScorePoint;
    public string objName;
    public float scorePoint;
    public int level;                       // The floor this object is on
    public bool toppled;                    // Flag determining whether this object has been knocked over or not

    private Rigidbody rb;
    private Collider cl;

    // The action type which is acted on this object
    private ScoreManager.ActionTypes aType = ScoreManager.ActionTypes.KNOCK;
    // The point at which force is applid
    private Vector3 collidePoint;
    //Audio Engine
    AudioSource KnockedSound;
    public AudioClip objectKnock;
    private bool _hasAudio;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cl = GetComponent<Collider>();
        // rb.centerOfMass = 0;
        collidePoint = transform.position;// + (rb.centerOfMass + Vector3.up * cl.bounds.size.y * 0.8f);
        toppled = false;

        KnockedSound = GetComponent<AudioSource>();
        _hasAudio = (KnockedSound && objectKnock) ? true : false;

        //Audio Engine
        if (_hasAudio)
        {
            KnockedSound.clip = objectKnock;
        }
    }


  	public void trigger(Vector3 pushForce) 
  	{
  		pushForce.y = - Mathf.Abs(pushForce.x);
  		rb.AddForceAtPosition(pushForce, collidePoint);

        //Debug.Log("collide at" + collidePoint);
        if (!toppled) {
            ScoreManager.instance.AddScore(objName, aType, scorePoint);
            toppled = true;
        }

  	}

    public void OnCollisionEnter (Collision col)
    {
        // Debug.Log("hit");
        if (_hasAudio)
        {
            float volume = Mathf.Clamp(col.relativeVelocity.magnitude / 45.0f, 0.0f, 1.0f);        
            KnockedSound.PlayOneShot(objectKnock, volume); 
        }
    }

}
