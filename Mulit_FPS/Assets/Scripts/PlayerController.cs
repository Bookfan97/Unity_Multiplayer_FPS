using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform viewPoint;
    [SerializeField] private float mouseSensitivty = 1.0f;
    [SerializeField] private bool shouldInverseMouse = false;
    private float verticalRotationLimit;
    private Vector2 mouseInput;
    
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivty;
        transform.rotation = Quaternion.Euler(
            transform.rotation.eulerAngles.x, 
            transform.rotation.eulerAngles.y + mouseInput.x,
            transform.rotation.eulerAngles.z
            );
        verticalRotationLimit += mouseInput.y;
        if (shouldInverseMouse)
        {
            verticalRotationLimit *= -1;
        }
        verticalRotationLimit = Mathf.Clamp(verticalRotationLimit, -60f, 60f);
        viewPoint.rotation = Quaternion.Euler(
            verticalRotationLimit,
            viewPoint.rotation.eulerAngles.y,
            viewPoint.rotation.eulerAngles.z); 
    }
}
