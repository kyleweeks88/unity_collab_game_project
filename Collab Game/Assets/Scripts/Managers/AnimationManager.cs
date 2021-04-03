﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    public Animator myAnim;
    [SerializeField] PlayerManager playerMgmt;

    #region Animator Parameters
    // My Animator parameters turned from costly Strings to cheap Ints
    [HideInInspector] public int isSprintingParam = Animator.StringToHash("isSprinting");
    int isJumpingParam = Animator.StringToHash("isJumping");
    int isGroundedParam = Animator.StringToHash("isGrounded");
    int yVelocityParam = Animator.StringToHash("yVelocity");
    [HideInInspector] public int inputXParam = Animator.StringToHash("InputX");
    [HideInInspector] public int inputYParam = Animator.StringToHash("InputY");
    int inCombatParam = Animator.StringToHash("inCombat");
    int isInteractingParam = Animator.StringToHash("isInteracting");
    int isSlidingParam = Animator.StringToHash("isSliding");
    #endregion


    void Update()
    {
        myAnim.SetFloat("moveAnimSpeed", 1f);
        myAnim.SetBool(isSlidingParam, playerMgmt.playerMovement.isSliding);
        myAnim.SetBool(isSprintingParam, playerMgmt.playerMovement.isSprinting);
        myAnim.SetBool(isJumpingParam, playerMgmt.playerMovement.isJumping);
        myAnim.SetBool(isGroundedParam, playerMgmt.playerMovement.isGrounded);
        myAnim.SetFloat(yVelocityParam, playerMgmt.myRb.velocity.y);
        myAnim.SetBool(inCombatParam, playerMgmt.combatMgmt.inCombat);

        if (playerMgmt.combatMgmt.attackInputHeld)
        {
            if (playerMgmt.equipmentMgmt.currentlyEquippedWeapon != null &&
                !playerMgmt.equipmentMgmt.currentlyEquippedWeapon.weaponData.isChargeable)
            {
                MeleeWeapon myWeapon = playerMgmt.equipmentMgmt.currentlyEquippedWeapon as MeleeWeapon;
                if((playerMgmt.playerStats.GetCurrentStamina() - myWeapon.meleeData.staminaCost) > 0)
                    myAnim.SetTrigger(playerMgmt.combatMgmt.attackAnim);
            }
        }

        if (playerMgmt.combatMgmt.attackInputHeld)
        {
            myAnim.SetFloat("moveAnimSpeed", 0.5f);
        }
        else
        {
            myAnim.SetFloat("moveAnimSpeed", 1f);
        }
    }

    /// <summary>
    /// Used to set the determined AnimatorOverrideController
    /// </summary>
    /// <param name="overrideCtrl"></param>
    public void SetAnimation(AnimatorOverrideController overrideCtrl)
    {
        myAnim.runtimeAnimatorController = overrideCtrl;
    }

    public void MovementAnimation(float xMove, float zMove)
    {
        myAnim.SetFloat(inputXParam, xMove);
        myAnim.SetFloat(inputYParam, zMove);
    }

    public void HandleMeleeAttackAnimation(bool boolVal)
    {
        myAnim.SetBool(playerMgmt.combatMgmt.attackAnim, boolVal);
        Debug.Log(boolVal);
    }

    public void HandleRangedAttackAnimation(bool boolVal)
    {
        myAnim.SetBool(playerMgmt.combatMgmt.attackAnim, boolVal);
    }
}
