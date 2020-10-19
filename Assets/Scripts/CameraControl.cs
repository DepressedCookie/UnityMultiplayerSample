using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float TurnSpeed = 1.0f;
    public Transform Target, Player;
    float mouseX, mouseY;


    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        CamControl();
    }

    void CamControl()
    {
        mouseX += Input.GetAxis("Mouse X") * TurnSpeed;
        mouseY -= Input.GetAxis("Mouse Y") * TurnSpeed;
        mouseY = Mathf.Clamp(mouseY, -35.0f, 60.0f);

        transform.LookAt(Target);

        Target.rotation = Quaternion.Euler(mouseY, mouseX, 0.0f);
        Player.rotation = Quaternion.Euler(0.0f, mouseX, 0.0f);
    }
}
