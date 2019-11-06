﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CentralHumanController : MonoBehaviour
{
    public Transform target;                    // Human target to be chased (will always be the raccoon)
    List<HumanController> humans;               // List of humans currently in the game
    public bool spotted = false;                // Flag determining if the raccoon has been spotted

    // Intermediate variables
    GameObject[] H;
    HumanController h;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize variables
        humans = new List<HumanController>();

        H = GameObject.FindGameObjectsWithTag("Human");
        // Retrieve all enemies
        for (int i = 0; i < H.Length; i++)
        {
            h = H[i].GetComponent<HumanController>();
            humans.Add(h);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Check if any of the humans have spotted the raccoon
        spotted = false;
        // For each worker
        for (int i = 0; i < humans.Count; i++)
        {
            // If the selected worker has seen the raccoon
            if (humans[i].seesRaccoon)
            {
                spotted = true;
            }
        }

        // If the raccoon has been spotted
        if (spotted)
        {
            //Debug.Log(1);
            // For each worker
            for (int i = 0; i < humans.Count; i++)
            {
                //Debug.Log("2: " + i);
                // If the selected worker is on the same floor as the raccoon
                if (humans[i].onSameFloor(target))
                {
                    // Inform worker of the location of the raccoon
                    humans[i].lastKnownLocation = target.position;
                }
            }
        }
    }
}
