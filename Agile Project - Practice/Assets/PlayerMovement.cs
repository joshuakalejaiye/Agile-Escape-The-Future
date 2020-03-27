using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody rb;
    public float forwardForce = 500.0f;
    public float sidewaysForce = 400.5f;
    public float jumpForce = 20000.5f;

    bool movingLeft = false;
    bool movingRight = false;
    // Update is called once per frame
   
    void Update()
    {
        rb.AddForce(0, 0, forwardForce * Time.deltaTime);

        movingLeft = Input.GetKey("a");
        movingRight = Input.GetKey("d");

        if (movingLeft)
            rb.AddForce(-sidewaysForce * Time.deltaTime, 0, 0);

        if (movingRight)
            rb.AddForce(sidewaysForce * Time.deltaTime, 0, 0);
    }
}

