using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    Vector3 body_rotation = Vector3.zero;
    Vector2 view_rotation = Vector2.zero;
    Vector3 inputs = Vector3.zero;
    Vector3 gravityAxis;
    public Rigidbody body;
    public Camera viewCam;
    public float rotateSpeed = 3;
    public float moveSpeed = 1;
    public float jumpHeight = 2f;
    public float gravity;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    void FixedUpdate()
    {
        inputs = Vector3.zero;
        gravityAxis = transform.position.normalized;
        body_rotation.y += Input.GetAxis("Mouse X");
        view_rotation.x += -Input.GetAxis("Mouse Y");

        var worldForward = Quaternion.Euler(body_rotation*rotateSpeed) * Vector3.forward;
        var rotation = Quaternion.LookRotation(worldForward, gravityAxis);
        body.rotation = rotation;

        inputs += transform.right * Input.GetAxis("Horizontal");
        inputs += transform.forward *Input.GetAxis("Vertical");
        if (Input.GetButtonDown("Jump"))
        {
            body.AddForce(transform.up * Mathf.Sqrt(jumpHeight*2*gravity), ForceMode.VelocityChange);
        }
        
        viewCam.transform.localEulerAngles = view_rotation * rotateSpeed;

        var force = -gravityAxis*gravity;
        body.AddForce(force,ForceMode.Acceleration);
        body.MovePosition(body.position + inputs * moveSpeed * Time.fixedDeltaTime);
        
    }
}
