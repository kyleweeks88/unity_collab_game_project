﻿using Cinemachine;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Component Ref")]
    [SerializeField] PlayerManager playerMgmt = null;

    [Header("Ground detection")]
    public LayerMask whatIsWalkable;
    public Transform groundColPos;
    public bool isGrounded;

    [Header("Slide settings")]
    public float slideVelocity;
    float currentSlideVelocity;
    public bool isSliding;

    Vector3 movement;
    Vector3 rotationMovement;
    Vector2 _previousMovementInput;

    //[HideInInspector] public float currentMoveSpeed = 0f;
    float turnSpeed = 15f;

    [HideInInspector] public bool isSprinting = false;
    public bool isJumping = false;
    bool jumpInputHeld = false;

    PhysicMaterial physMat;

    void Start()
    {
        playerMgmt.inputMgmt.jumpEventStarted += Jump;
        playerMgmt.inputMgmt.jumpEventCancelled += JumpReleased;
        playerMgmt.inputMgmt.sprintEventStarted += SprintPressed;
        playerMgmt.inputMgmt.sprintEventCancelled += SprintReleased;
        playerMgmt.inputMgmt.moveEvent += OnMove;

        physMat = gameObject.GetComponent<CapsuleCollider>().material;

        currentSlideVelocity = slideVelocity;
    }

    private void FixedUpdate()
    {
        // Handles the player's PhysicMaterial to prevent slow-sliding down shallow slopes when standing still.
        // but turns friction to zero when the player is moving.
        if (_previousMovementInput.sqrMagnitude != 0)
        {
            physMat.dynamicFriction = 0f;
        }
        else if(_previousMovementInput.sqrMagnitude == 0 && !isSliding)
        {
            physMat.dynamicFriction = 1f;
        }

        HandleSliding();
        GroundCheck();
        UpdateIsSprinting();
        Move();
        CameraControl();
    }

    public Vector3 GetPrevMovement()
    {
        return _previousMovementInput;
    }

    void HandleSliding()
    {
        Vector3 adjustedPos = new Vector3(transform.position.x, transform.position.y + 0.25f, transform.position.z);
        if (!CheckSlope(adjustedPos, Vector3.down, 10f))
        {
            isSliding = false;
            currentSlideVelocity = 0f;
        }
        else
        {
            isSliding = true;
            currentSlideVelocity = slideVelocity;
            physMat.dynamicFriction = 0f;
        }
    }

    void GroundCheck()
    {
        Collider[] groundCollisions = Physics.OverlapSphere(groundColPos.position, 0.25f, whatIsWalkable);

        if (groundCollisions.Length <= 0)
        {
            isGrounded = false;
            // Add the aerialMovementModifier if it isn't already affecting _moveSpeed.
            if (!playerMgmt.playerStats.moveSpeed.StatModifiers.Contains(playerMgmt.playerStats.aerialMovementModifier))
                playerMgmt.playerStats.moveSpeed.AddModifer(playerMgmt.playerStats.aerialMovementModifier);

            // Makes jumping and falling feel better
            if (playerMgmt.myRb.velocity.y < 0f)
            {
                playerMgmt.myRb.velocity += Vector3.up * Physics.gravity.y * (10f - 1f) * Time.deltaTime;
            }
            else if(playerMgmt.myRb.velocity.y > 0f && !jumpInputHeld)
            {
                playerMgmt.myRb.velocity += Vector3.up * Physics.gravity.y * (8f - 1f) * Time.deltaTime;
            }

            // If the player has jumped and is now falling downwards, cast a ray 
            // to check for ground and turn isJumping false if hit.
            if (isJumping && playerMgmt.myRb.velocity.y < 0f)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, Vector3.down, out hit, 0.5f, whatIsWalkable))
                {
                    //playerMgmt.playerStats._moveSpeed.RemoveModifier(playerMgmt.playerStats.aerialMovementModifier);
                    isJumping = false;
                }
            }
        }
        else
        {
            isGrounded = true;

            // Remove the aerialMovementModifier if it is still affecting _moveSpeed.
            if (playerMgmt.playerStats.moveSpeed.StatModifiers.Contains(playerMgmt.playerStats.aerialMovementModifier))
                playerMgmt.playerStats.moveSpeed.RemoveModifier(playerMgmt.playerStats.aerialMovementModifier);

            // This stops the ground collision from pre-emptively turning isJumping to false when the player jumps.
            if (Mathf.Abs(playerMgmt.myRb.velocity.y) < 0.01f && Mathf.Abs(playerMgmt.myRb.velocity.y) > -0.01f)
                isJumping = false;
        }
    }

    bool CheckSlope(Vector3 position, Vector3 desiredDirection, float distance)
    {
        Debug.DrawRay(position, desiredDirection, Color.green);

        Ray myRay = new Ray(position, desiredDirection); // cast a Ray from the position of our gameObject into our desired direction. Add the slopeRayHeight to the Y parameter.
        RaycastHit hit;

        if (Physics.Raycast(myRay, out hit, distance, whatIsWalkable))
        {
            float slopeAngle = Mathf.Deg2Rad * Vector3.Angle(Vector3.up, hit.normal); // Here we get the angle between the Up Vector and the normal of the wall we are checking against: 90 for straight up walls, 0 for flat ground.

            if (slopeAngle >= 45f * Mathf.Deg2Rad) //You can set "steepSlopeAngle" to any angle you wish.
            {
                return true; // return false if we are very near / on the slope && the slope is steep
            }

            return false; // return true if the slope is not steep
        }

        return false;
    }

    void OnMove(Vector2 movement)
    {
        _previousMovementInput = movement;
    }

    public void Move()
    {
        // CONVERTS THE INPUT INTO A NORMALIZED VECTOR3
        movement = new Vector3
        {
            x = _previousMovementInput.x,
            y = -currentSlideVelocity,
            z = _previousMovementInput.y
        }.normalized;

        // Only allows the player to sprint forwards
        if(isSprinting && movement.z <= 0)
        {
            SprintReleased();
        }

        // HANDLES ANIMATIONS
        playerMgmt.animMgmt.MovementAnimation(movement.x, movement.z);

        // MOVES THE PLAYER
        if (movement.z < 0)
        {
            playerMgmt.myRb.velocity += rotationMovement * (playerMgmt.playerStats.moveSpeed.value * .5f);
        }
        else
        {
            playerMgmt.myRb.velocity += rotationMovement * playerMgmt.playerStats.moveSpeed.value;
        }
    }

    void CameraControl()
    {
        // MAKES THE CHARACTER'S FORWARD AXIS MATCH THE CAMERA'S FORWARD AXIS
        rotationMovement = Quaternion.Euler(0, playerMgmt.myCamera.transform.rotation.eulerAngles.y, 0) * movement;

        // MAKES THE CHARACTER MODEL TURN TOWARDS THE CAMERA'S FORWARD AXIS
        // ... ONLY IF THE PLAYER IS MOVING, BLOCKING OR ATTACKING
        if (movement.sqrMagnitude > 0 || playerMgmt.combatMgmt.isBlocking || playerMgmt.combatMgmt.attackInputHeld)
        {
            float cameraYaw = playerMgmt.myCamera.transform.rotation.eulerAngles.y;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, cameraYaw, 0), turnSpeed * Time.deltaTime);
        }
    }

    #region Sprinting
    public void SprintPressed()
    {
        if (movement.z > 0.1 && playerMgmt.playerStats.GetCurrentStamina()
            - playerMgmt.playerStats.staminaDrainAmount > 0)
        {
            isSprinting = true;
            playerMgmt.isInteracting = true;

            // adds moveSpeed StatModifier
            playerMgmt.playerStats.moveSpeed.AddModifer(playerMgmt.playerStats.sprintMovementModifier);

            playerMgmt.sprintCamera.GetComponent<CinemachineVirtualCameraBase>().m_Priority = 11;
        }
    }

    public void SprintReleased()
    {
        isSprinting = false;

        // removes moveSpeed StatModifier
        playerMgmt.playerStats.moveSpeed.RemoveModifier(playerMgmt.playerStats.sprintMovementModifier);

        playerMgmt.sprintCamera.GetComponent<CinemachineVirtualCameraBase>().m_Priority = 9;
    }

    void UpdateIsSprinting()
    {
        if (isSprinting)
        {
            if (playerMgmt.playerStats.GetCurrentStamina()
                - playerMgmt.playerStats.staminaDrainAmount > 0)
            {
                playerMgmt.playerStats.StaminaDrainOverTime( 
                    playerMgmt.playerStats.staminaDrainAmount,
                    playerMgmt.playerStats.staminaDrainDelay);
            }
            else
            {
                SprintReleased();
                return;
            }
        }
    }
    #endregion

    public void Jump()
    {
        if (playerMgmt.isInteracting) { return; }
        if (isSliding) { return; }

        jumpInputHeld = true;
        
        if (!isJumping && isGrounded)
        {
            if (playerMgmt.playerStats.GetCurrentStamina() - 10f > 0)
            {
                isJumping = true;
                //playerMgmt.isInteracting = true;
                playerMgmt.playerStats.DamageStamina(10f);
                playerMgmt.myRb.velocity += Vector3.up * playerMgmt.playerStats.jumpForce.value;
                playerMgmt.myRb.velocity += rotationMovement * playerMgmt.playerStats.jumpForce.value;
            }
        }
    }

    void JumpReleased()
    {
        jumpInputHeld = false;
    }
}
