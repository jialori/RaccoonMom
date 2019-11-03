﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class HumanController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxAngle = 30.0f;              // Field of view of this human
    [SerializeField] private float maxRadius = 15.0f;             // The farthest distance that this human can see
    [SerializeField] private int level;
    Vector3 initialPosition;                // Starting position of this human. Will return here after losing sight of raccoon
    Quaternion initialDirection;            // Direction this human initially faces. Will rotate to face this direction after returning to initialPosition
    NavMeshAgent agent;                     // Pathfinding AI
    NavMeshPath p;
    CentralHumanController CHC;             // Reference to the Central Human Controller
    
    [Header("Target")]
    [SerializeField] private Transform target;                // Human target to be chased (will always be the raccoon)
    public Vector3 lastKnownLocation;       // Location where this human last saw the raccoon
    [SerializeField] private float attackRange = 1; // Range of attack for this worker
    private float rotationSpeed = 5.0f;         // How fast this human rotates
    private float attackCooldown = 10.0f;       // The cooldown timer for the worker's stun attack
    public bool seesRaccoon = false;        // Flag determining whether this human can see the raccoon or not
    private bool canAttack = true;          // Flag determining whether this human can attack
    private bool chasing = false;           // Human status: The human knows where the raccoon is and is currently chasing her
    private bool searching = false;         // Human status: The raccoon has escaped the human's sight and the human is looking for her
    private bool idle = true;               // Human status: The human does not know where the raccoon is and is not looking for her

    [Header("Voice Over")]
    AudioSource WorkerAudio;                // Audiosource files and script
    public AudioClip[] workerChaseVO;       // Chasing VO
    public AudioClip[] workerStunVO;        // Chasing VO
    public float replayInterval;            // Time till replay is ready
    private float _timer = 0;
    private bool alreadyPlayed = false;     // Helps with OneShot trigger to only have one instance of sound    

    // Animation
    private Animator anim;

    // For Play/Pause toggle 
    private bool pause = false;

    private void Awake()
    {
        workerChaseVO = Resources.LoadAll<AudioClip>("Audio/ChaseVO");
        workerStunVO = Resources.LoadAll<AudioClip>("Audio/StunVO");
        Debug.Log("[HumanController] workerChaseVO.length: " + workerChaseVO.Length);
        Debug.Log("[HumanController] workerStunVO.length: " + workerStunVO.Length);

    }

    // Outline detection cones in the editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxRadius);

        Vector3 fovLine1 = Quaternion.AngleAxis(maxAngle, transform.up) * transform.forward * maxRadius;
        Vector3 fovLine2 = Quaternion.AngleAxis(-maxAngle, transform.up) * transform.forward * maxRadius;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);

        if (seesRaccoon)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, (target.position - transform.position).normalized * maxRadius);
        }        

        Gizmos.color = Color.black;
        Gizmos.DrawRay(transform.position, transform.forward * maxRadius);
    }

    // Determine if the human and the player are on the same floor
    public bool onSameFloor(Transform target)
    {
        return level == target.GetComponent<RaccoonController>().level;
    }

    // Determine if the player has been seen by this human
    public bool inFOV(NavMeshAgent nav, Transform checkingObject, Transform target, float maxAngle, float maxRadius)
    {
        Vector3 directionBetween = (target.position - checkingObject.position).normalized;
        directionBetween.y *= 0;

        float angle = Vector3.Angle(checkingObject.forward, directionBetween);

        // If the player is within the field of view angles
        if (angle <= maxAngle)
        {
            //Debug.Log(1);
            //Ray ray = new Ray(checkingObject.position, target.position - checkingObject.position);

            // If the player is close enough to the human to be seen
            if (Vector3.Distance(checkingObject.position, target.position) < maxRadius)
            {
                //Debug.Log(2);
                NavMeshHit hit;

                // If the player and human are on the same floor
                if (onSameFloor(target))
                {
                    //Debug.Log(3);
                    // If the human can directly see the player (i.e. line of sight is not blocked by wall or bush)
                    if (!nav.Raycast(target.position, out hit))
                    {
                        //Debug.Log("HumanController: Worker level = " + level.ToString() + "Raccoon level = " 
                        //    + target.GetComponent<RaccoonController>().level.ToString());
                        return true;
                    }
                }
                
            }
        }

        return false;
    }

    void Start()
    {
        // Register itself at GameManager
        GameManager.instance.Workers.Add(this);

        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = 2f;
        CHC = GameObject.Find("CentralHumanController").GetComponent<CentralHumanController>();
        //Debug.Log(CHC);
        p = new NavMeshPath();
        initialPosition = transform.position;
        initialDirection = transform.rotation;

        anim = gameObject.GetComponent<Animator>();
        //Audio Component
        WorkerAudio = GetComponent<AudioSource>();
    }

    void Update() 
    {
        if (pause) return;

        /*
        if (!onSameFloor(target))
            return;
        */

        _timer += Time.deltaTime;
        if (alreadyPlayed && _timer > replayInterval)
        {
            alreadyPlayed = false;
        }

        seesRaccoon = inFOV(agent, transform, target, maxAngle, maxRadius);
        /*
        if (seesRaccoon)
        {
            Debug.Log("detected raccoon");
        }
        */
        // Animation
        anim.SetBool("scared", seesRaccoon);
        

        // 'lastKnownLocation' is assigned by the Central Human Controller. If a new 'lastKnownLocation' is assigned, then the raccoon has been spotted
        // somewhere and the human will head to that location if he can reach it

        // Debug Animation
        //if (GameObject.Find("C_worker_Rigged").transform == transform)
        //{
        //Debug.Log(agent.CalculatePath(lastKnownLocation, p));
        //Debug.Log(p.status);
        //}

        // When the worker is at the same floor and not reach lastKnownLocation yet
        if (CHC.spotted && agent.CalculatePath(lastKnownLocation, p) 
            && System.Math.Abs(transform.position.x - lastKnownLocation.x) > 2)
        {
            //Debug.Log("Now chasing Raccoon");
            chasing = true;
            searching = false;
            idle = false;
            agent.SetDestination(lastKnownLocation);

            //Audio trigger for sighting Raccoon
            if (!seesRaccoon && !alreadyPlayed)
            {
                chaseVO();
            }

            // Animation
            anim.SetBool("chasing", true);
            anim.SetBool("scared", false);
        }
        //Debug.Log("seesRaccoon: " + seesRaccoon);
        //Debug.Log("Distance: " + Vector3.Distance(transform.position, lastKnownLocation));

        /*
        // If the raccoon leaves the nav mesh, the worker will return to the 'idle' state
        if (!agent.pathPending)
        {
            Debug.Log("Raccoon not in nav mesh");
            chasing = false;
            searching = false;
            idle = true;
        }
        */

        // The human will stop if he is at 'lastKnownLocation' and can't see the raccoon
        if (!seesRaccoon && inRange(lastKnownLocation, attackRange))
        {
            //Debug.Log("Lost Raccoon");
            chasing = false;
            searching = true;
            idle = false;
            agent.ResetPath();

            // Animation
            anim.SetBool("chasing", false);
        }
        if (inRange(lastKnownLocation, attackRange))
        {
            Debug.Log("current position: " + transform.position.ToString() + ", destination: " + lastKnownLocation.ToString());
        }

        // Raccoon is in range
        if (seesRaccoon && inRange(lastKnownLocation, attackRange) && canAttack)
        {
            // Stun attack here
            Debug.Log("stun attack used");
            target.GetComponent<RaccoonController>().isStunned = true;
            anim.SetBool("attack", true);

            // Start cooldown after attack
            canAttack = false;
            attackCooldown = 10.0f;

            //Audio
            if (!canAttack)
            {
                stunVO();
            }

            // Animation
            //anim.Play("kicking");
        }
        else if (!canAttack)
        {
            if (attackCooldown > 0)
            {
                attackCooldown -= Time.deltaTime;
            }
            else
            {
                canAttack = true;
                //Debug.Log("stun attack ready");
            }
            anim.SetBool("attack", false);
        }
        
        // The human will turn to his left and right in case the raccoon is beside him
        if (searching && !chasing && !idle)
        {
            /*
            Debug.Log("Searching for Raccoon");
            agent.isStopped = true;
            transform.Rotate(new Vector3(0.0f, 90.0f, 0.0f), Time.deltaTime * rotationSpeed);
            transform.Rotate(new Vector3(0.0f, 270.0f, 0.0f), Time.deltaTime * rotationSpeed);
            transform.Rotate(new Vector3(0.0f, 270.0f, 0.0f), Time.deltaTime * rotationSpeed);
            agent.isStopped = false;
            */
            searching = false;
            idle = true;
        }

        // The human will return to his original position if he can't find the raccoon
        if (idle)
        {
            if (inRange(initialPosition, 1))
            {
                Debug.Log("Now rotating to initial direction");
                transform.rotation = Quaternion.RotateTowards(transform.rotation, initialDirection, rotationSpeed);
            }
            else
            {
                //Debug.Log("Can't find Raccoon. Returning to initial position");
                Debug.Log("current position: " + transform.position.ToString() + " initial position: " + initialPosition.ToString());
                Debug.Log("X: " + (transform.position.x != initialPosition.x).ToString() + ", Y: " + (transform.position.y != initialPosition.y).ToString() + ", Z: " + (transform.position.z != initialPosition.z).ToString());
                if (agent.SetDestination(initialPosition))
                {
                    //Debug.Log("Now heading to " + agent.destination.ToString() + ". Initial position is " + initialPosition.ToString());
                }
            }
        }


        if (anim.GetBool("scared"))
            agent.isStopped = true;
        else
            agent.isStopped = false;

        if (anim.GetBool("attack"))
            agent.isStopped = true;
        else
            agent.isStopped = false;
    }

    private void chaseVO()
    {
        // randomize        
        int randIdx = Random.Range(0, workerChaseVO.Length);
        WorkerAudio.PlayOneShot(workerChaseVO[randIdx], 0.8F);
        // Ensures a true OneShot and no repeated sound
        alreadyPlayed = true;
        _timer = 0;

    }

    private void stunVO()
    {
        // randomize        
        int randIdx = Random.Range(0, workerStunVO.Length);
        WorkerAudio.PlayOneShot(workerStunVO[randIdx], 0.5F);
        // Ensures a true OneShot and no repeated sound
        alreadyPlayed = true;
        _timer = 0;

    }

    public void TogglePlay()
    {
        if (pause)
        {
            agent.isStopped = false;
        } else 
        {
            agent.isStopped = true;
        }
        pause = !pause;
        anim.enabled = !anim.enabled;
    }

    private bool inRange(Vector3 target, float range) 
    {
        return System.Math.Abs(transform.position.x - target.x) < range && System.Math.Abs(transform.position.z - target.z) < range;
    }
}
