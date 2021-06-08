﻿using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Weapon[] weapons;
    [SerializeField] private bool shouldInverseMouse = false;
    [SerializeField] private Transform viewPoint;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private GameObject bulletImpact;
    [SerializeField] LayerMask groundLayers;
    [SerializeField] private float mouseSensitivty = 1.0f;
    [SerializeField] private float jumpForce = 12.0f;
    [SerializeField] private float gravityMod = 2.0f;
    [SerializeField] private float moveSpeed = 5.0f, runSpeed = 8.0f;
    public float muzzleDisplayTime;
    [SerializeField] float maxHeatValue = 10f, /*heatPerShot = 1f,*/ coolRate = 4f, overHeatCoolRate = 5f;
    private bool isGrounded, isOverheating;
    private float verticalRotationLimit, activeMoveSpeed, shotCounter, heatCounter, muzzleCounter;
    private Vector2 mouseInput;
    private Vector3 moveDirection, movement;
    private Camera _camera;
    private int selectedWeapon;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _camera = Camera.main;
        UIController.instance.weaponTempSlider.maxValue = maxHeatValue;
    }

    // Update is called once per frame
    void Update()
    {
        CameraMovement();
        PlayerMovement();
        InputController();
    }

    private void LateUpdate()
    {
        UpdateCamera();
    }
    
    
    private void PlayerMovement()
    {
        moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (Input.GetKey(KeyCode.LeftShift))
        {
            activeMoveSpeed = runSpeed;
        }
        else
        {
            activeMoveSpeed = moveSpeed;
        }

        float yVelocity = movement.y;
        movement = ((transform.forward * moveDirection.z) + (transform.right * moveDirection.x)).normalized * activeMoveSpeed;
        movement.y = yVelocity;
        if (_characterController.isGrounded) movement.y = 0;
        isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .25f, groundLayers);
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            movement.y = jumpForce;
        }
        movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;
        _characterController.Move(movement * Time.deltaTime);
    }

    private void InputController()
    {
        if (weapons[selectedWeapon].muzzleFlash.activeInHierarchy)
        {
            muzzleCounter -= Time.deltaTime;
            if (muzzleCounter <= 0)
            {
                weapons[selectedWeapon].muzzleFlash.SetActive(false);
            }
        }
        if (!isOverheating)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Shoot();
            }

            if (Input.GetMouseButton(0) && weapons[selectedWeapon].isAutomatic)
            {
                shotCounter -= Time.deltaTime;
                if (shotCounter <= 0)
                {
                    Shoot();
                }
            }

            heatCounter -= coolRate * Time.deltaTime;
        }
        else
        {
            heatCounter -= overHeatCoolRate * Time.deltaTime;
            if (heatCounter <= 0)
            {
                heatCounter = 0;
                isOverheating = false;
            }
        }

        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
        {
            selectedWeapon++;
            if (selectedWeapon >= weapons.Length)
            {
                selectedWeapon = 0;
            }
            SwitchWeapon();
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
        {
            selectedWeapon--;
            if (selectedWeapon < 0)
            {
                selectedWeapon = weapons.Length - 1;
            }
            SwitchWeapon();
        }

        for (int i = 0; i < weapons.Length; i++)
        {
            if (Input.GetKeyDown((i+1).ToString()))
            {
                selectedWeapon = i;
                SwitchWeapon();
            }
        }
        
        UIController.instance.weaponTempSlider.value = heatCounter;
            
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else if (Cursor.lockState == CursorLockMode.None)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    #region Weapom
    public void Shoot()
    {
        Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        ray.origin = _camera.transform.position;
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("Hit: "+hit.collider.gameObject.name);
            GameObject bulletInstantiate = Instantiate(bulletImpact, hit.point + (hit.normal * 0.002f), Quaternion.LookRotation(hit.normal, Vector3.up));
            Destroy(bulletInstantiate, 10f);
        }
        shotCounter = weapons[selectedWeapon].timeBetweenShots;
        heatCounter += weapons[selectedWeapon].heatPerShot;

        if (heatCounter >= maxHeatValue)
        {
            heatCounter = maxHeatValue;
            isOverheating = true;
        }
        
        weapons[selectedWeapon].muzzleFlash.SetActive(true);
        muzzleCounter = muzzleDisplayTime;
    }

    void SwitchWeapon()
    {
        foreach (Weapon weapon in weapons)
        {
            weapon.gameObject.SetActive(false);
        }
        weapons[selectedWeapon].gameObject.SetActive(true);
        weapons[selectedWeapon].muzzleFlash.SetActive(false);
    }
    
    #endregion

    #region Camera
    private void UpdateCamera()
    {
        _camera.transform.position = viewPoint.position;
        _camera.transform.rotation = viewPoint.rotation;
    }
    
    private void CameraMovement()
    {
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivty;
        transform.rotation = Quaternion.Euler(
            transform.rotation.eulerAngles.x,
            transform.rotation.eulerAngles.y + mouseInput.x,
            transform.rotation.eulerAngles.z
        );
        verticalRotationLimit += mouseInput.y;
        verticalRotationLimit = Mathf.Clamp(verticalRotationLimit, -60f, 60f);
        if (shouldInverseMouse)
        {
            viewPoint.rotation = Quaternion.Euler(
                verticalRotationLimit,
                viewPoint.rotation.eulerAngles.y,
                viewPoint.rotation.eulerAngles.z);
        }
        else
        {
            viewPoint.rotation = Quaternion.Euler(
                -verticalRotationLimit,
                viewPoint.rotation.eulerAngles.y,
                viewPoint.rotation.eulerAngles.z);
        }
    }
    #endregion
}
