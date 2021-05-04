﻿using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform viewPoint;
    [SerializeField] private float mouseSensitivty = 1.0f;
    [SerializeField] private bool shouldInverseMouse = false;
    [SerializeField] private float moveSpeed = 5.0f;
    private float verticalRotationLimit;
    private Vector2 mouseInput;
    private Vector3 moveDirection, movement;
    
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        CameraMovement();
        PlayerMovement();
    }

    private void PlayerMovement()
    {
        moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        movement = ((transform.forward * moveDirection.z) + (transform.right * moveDirection.x)).normalized;
        transform.position += movement * moveSpeed * Time.deltaTime;
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
}
