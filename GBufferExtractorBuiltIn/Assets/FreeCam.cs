using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeCam : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float lookSpeed = 2f;
    public bool lockCursor = true;

    float yaw = 0f;
    float pitch = 0f;

    void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void Update()
    {
        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        transform.eulerAngles = new Vector3(pitch, yaw, 0f);

        // Movement
        Vector3 move = new Vector3(
            Input.GetAxis("Horizontal"), 
            (Input.GetKey(KeyCode.Space) ? 1 : 0) - (Input.GetKey(KeyCode.LeftShift) ? 1 : 0), 
            Input.GetAxis("Vertical")
        );

        transform.Translate(move * moveSpeed * Time.unscaledDeltaTime, Space.Self);
    }
}
