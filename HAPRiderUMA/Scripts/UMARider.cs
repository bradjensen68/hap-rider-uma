﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MalbersAnimations;
using MalbersAnimations.HAP;
using MalbersAnimations.Weapons;
using MalbersAnimations.Utilities;
using UMA;
using UMA.CharacterSystem;
using System;

[RequireComponent(typeof(DynamicCharacterAvatar))]
public class UMARider : MonoBehaviour
{
    private DynamicCharacterAvatar characterAvatar;
    private MWeaponManager weaponManager;
    private MInventory inventory;
    private Animator Anim;
    private IAim aim;
    private LookAt lookAt;
    private MalbersAnimations.Controller.MAnimal mAnimal;
    private MRider rider;

    private Dictionary<string, UMAWardrobeItem> equippedWardrobeItems = new Dictionary<string, UMAWardrobeItem>();

    private Transform root;
    private Transform head;
    private Transform neck;
    private Transform rightHand;
    private Transform leftHand;

    private GameObject rightHandEquipPoint;
    private GameObject leftHandEquipPoint;

    /*public GameObject backHolderWeapon;
    public GameObject rightHolderWeapon;
    public GameObject leftHolderWeapon;*/

    public Vector3 equipPointRightOffset = new Vector3(0.00980000012f, 0.0573000014f, -0.00779999979f);
    public Vector3 equipPointRightRotation = new Vector3(-65.5419998f, -65.0139999f, 60.3079987f);

    public Vector3 equipPointLeftOffset = new Vector3(0.0299999993f, 0.0110999998f, -0.0274999999f);
    public Vector3 equipPointLeftRotation = new Vector3(-105.530998f, -44.1660194f, 43.1020088f);

    void Start () {
        characterAvatar = GetComponent<DynamicCharacterAvatar>();
        weaponManager = gameObject.GetComponent<MWeaponManager>();
        inventory = gameObject.GetComponent<MInventory>();
        Anim = gameObject.GetComponent<Animator>();
        aim = gameObject.GetComponent<IAim>();
        lookAt = gameObject.GetComponent<LookAt>();
        mAnimal = gameObject.GetComponent<MalbersAnimations.Controller.MAnimal>();
        rider = gameObject.GetComponent<MRider>();
    }

    private void FindCharacterTransforms()
    {
        root = transform.Find("Root");
        head = transform.Find("Root/Global/Position/Hips/LowerBack/Spine/Spine1/Neck/Head");
        neck = transform.Find("Root/Global/Position/Hips/LowerBack/Spine/Spine1/Neck");
        rightHand = transform.Find("Root/Global/Position/Hips/LowerBack/Spine/Spine1/RightShoulder/RightArm/RightForeArm/RightHand");
        leftHand = transform.Find("Root/Global/Position/Hips/LowerBack/Spine/Spine1/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
    }

    public void EquipUMAWardrobeItem(UMAWardrobeItem wardrobeItem)
    {
        if(equippedWardrobeItems.ContainsKey(wardrobeItem.wardrobeSlot))
        {
            //Debug.Log("UMA Unequip Previous item: <color=yellow>" + equippedWardrobeItems[wardrobeItem.wardrobeSlot].name + "</color>");
            characterAvatar.ClearSlot(wardrobeItem.textRecipe.wardrobeSlot);

            //equippedWardrobeItems[wardrobeItem.wardrobeSlot].inventorySlot.item.OnItemUnEquipped.Invoke(null); //Item Event
            //equippedWardrobeItems[wardrobeItem.wardrobeSlot].inventorySlot.inventory.OnItemUnEquipped.Invoke(null); //Inventory Event

            equippedWardrobeItems[wardrobeItem.wardrobeSlot].inventorySlot.equippedSlot = false;
            equippedWardrobeItems[wardrobeItem.wardrobeSlot].inventorySlot.EquippedText.gameObject.SetActive(false);

            equippedWardrobeItems.Remove(wardrobeItem.wardrobeSlot);
        }

        //Debug.Log("Equip new item: <color=yellow>" + wardrobeItem.name + "</color>");
        equippedWardrobeItems.Add(wardrobeItem.wardrobeSlot, wardrobeItem);
        wardrobeItem.inventorySlot.equippedSlot = true;
        wardrobeItem.inventorySlot.EquippedText.gameObject.SetActive(true);

        //Debug.Log("UMA Equip: <color=yellow>" + wardrobeItem.name + "</color>");
        characterAvatar.SetSlot(wardrobeItem.textRecipe);

        // catch holders before they are lost at rebuild of character - otherwise we possibly lose our weapons
        FireBeforeCharacterUpdate();

        characterAvatar.BuildCharacter(true);

    }

    public void UnequipUMAWardrobeItem(UMAWardrobeItem wardrobeItem)
    {
        if (equippedWardrobeItems.ContainsKey(wardrobeItem.wardrobeSlot))
        {
            equippedWardrobeItems.Remove(wardrobeItem.wardrobeSlot);
        }

        wardrobeItem.inventorySlot.item.OnItemUnEquipped.Invoke(null); //Item Event
        wardrobeItem.inventorySlot.inventory.OnItemUnEquipped.Invoke(null); //Inventory Event

        //Debug.Log("UMA Unequipped: <color=yellow>" + wardrobeItem.textRecipe.wardrobeSlot + "</color>");
        characterAvatar.ClearSlot(wardrobeItem.textRecipe.wardrobeSlot);
        characterAvatar.ReapplyWardrobeCollections();

        FireBeforeCharacterUpdate();

        characterAvatar.BuildCharacter(true);
    }

    /// <summary>
    /// <para>Unparent our created equip points.</para>
    /// <para>Used to catch them before they are lost at rebuild of character.</para>
    /// </summary>
    private void FireBeforeCharacterUpdate()
    {
        rightHandEquipPoint.transform.SetParent(null, true);
        leftHandEquipPoint.transform.SetParent(null, true);
    }

    // should be called on UMA updated event
    public void OnCharacterUpdated()
    {
        FindCharacterTransforms();

        SetRightHandEquipPointParent();
        
        SetLeftHandEquipPointParent();

        if (weaponManager)
        {
            SetWeaponManagerTransforms();
        }

        if (rider)
        {
            SetRiderTransforms();
        }

        mAnimal.ResetController();
    }

    // should be called on UMA created event
    public void OnCharacterCreated () {

        FindCharacterTransforms();

        rightHandEquipPoint = new GameObject("RightHandPoint");
        SetRightHandEquipPointParent();

        leftHandEquipPoint = new GameObject("LeftHandPoint");
        SetLeftHandEquipPointParent();

        if (weaponManager)
        {
            SetWeaponManagerTransforms();
        }

        if (rider)
        {
            SetRiderTransforms();
        }

        // this works with the Rider's holders, which expects that the weapons should already be instantiated
        /*if (backHolderWeapon)
        {
            GameObject backWeapon = Instantiate(backHolderWeapon, weaponManager.holsters[1].Slots[0].position, weaponManager.holsters[1].Slots[0].rotation);
            if (backWeapon)
            {
                backWeapon.transform.parent = weaponManager.holsters[1].Slots[0];
                inventory.Inventory.Add(backWeapon);
            }
        }

        if (rightHolderWeapon)
        {
            GameObject rightWeapon = Instantiate(rightHolderWeapon, weaponManager.holsters[2].Slots[0].position, weaponManager.holsters[2].Slots[0].rotation);
            if (rightWeapon)
            {
                rightWeapon.transform.parent = weaponManager.holsters[2].Slots[0];
                inventory.Inventory.Add(rightWeapon);
            }
        }

        if (leftHolderWeapon)
        {
            GameObject leftWeapon = Instantiate(leftHolderWeapon, weaponManager.holsters[0].Slots[0].position, weaponManager.holsters[0].Slots[0].rotation);
            if (leftWeapon)
            {
                leftWeapon.transform.parent = weaponManager.holsters[0].Slots[0];
                inventory.Inventory.Add(leftWeapon);
            }
        }*/

        mAnimal.ResetController();
    }

    private void SetRiderTransforms()
    {
        //rider.RightHandEquipPoint = rightHandEquipPoint.transform;
        //rider.LeftHandEquipPoint = leftHandPoint.transform;
        //rider.HolderBack = holderBack.transform;
        //rider.HolderRight = holderRight.transform;
        //rider.HolderLeft = holderLeft.transform;

        // we had to change set accessors for these on the RiderCombat class in MRider.cs
        //rider.RiderRoot = transform.root;
        rider.Chest = Anim.GetBoneTransform(HumanBodyBones.Chest);                   //Get the Rider Head transform
        rider.Spine = Anim.GetBoneTransform(HumanBodyBones.Spine);                     //Get the Rider Head transform
        //--------------
        //rider.Head = Anim.GetBoneTransform(HumanBodyBones.Head);                     //Get the Rider Head transform
        rider.RightHand = Anim.GetBoneTransform(HumanBodyBones.RightHand);           //Get the Rider Right Hand transform
        rider.LeftHand = Anim.GetBoneTransform(HumanBodyBones.LeftHand);             //Get the Rider Left  Hand transform
        //rider.RightShoulder = Anim.GetBoneTransform(HumanBodyBones.RightUpperArm);   //Get the Rider Right Shoulder transform
        //rider.LeftShoulder = Anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
    }

    private void SetWeaponManagerTransforms()
    {
        //weaponManager.RightShoulder = Anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        //weaponManager.LeftShoulder = Anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        weaponManager.RightHand = Anim.GetBoneTransform(HumanBodyBones.RightHand);
        weaponManager.LeftHand = Anim.GetBoneTransform(HumanBodyBones.LeftHand);
        //weaponManager.Head = Anim.GetBoneTransform(HumanBodyBones.Head);
        //weaponManager.Chest = Anim.GetBoneTransform(HumanBodyBones.Chest);

        weaponManager.LeftHandEquipPoint = leftHandEquipPoint.transform;
        weaponManager.RightHandEquipPoint = rightHandEquipPoint.transform;
    }

    void SetRightHandEquipPointParent()
    {
        rightHandEquipPoint.transform.parent = rightHand;
        rightHandEquipPoint.transform.position = rightHand.position;
        rightHandEquipPoint.transform.rotation = rightHand.rotation;
        rightHandEquipPoint.transform.localPosition = equipPointRightOffset;
        rightHandEquipPoint.transform.localRotation = Quaternion.Euler(equipPointRightRotation);
        rightHandEquipPoint.transform.localScale = new Vector3(1f, 1f, 1f);
    }

    void SetLeftHandEquipPointParent()
    {
        leftHandEquipPoint.transform.parent = leftHand;
        leftHandEquipPoint.transform.position = leftHand.position;
        leftHandEquipPoint.transform.rotation = leftHand.rotation;
        leftHandEquipPoint.transform.localPosition = equipPointLeftOffset;
        leftHandEquipPoint.transform.localRotation = Quaternion.Euler(equipPointLeftRotation);
        leftHandEquipPoint.transform.localScale = new Vector3(1f, 1f, 1f);
    }
}