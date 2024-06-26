using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayMovementController : MonoBehaviour
{

    public bool keyboardController = false;
    public float rotationSpeed = 10f;
    public float movementSpeed = 10f;
    public Transform compassHalo;
    public Transform windHalo;
    public Transform camera;

    Vector3 rotationVector = new Vector3(0, 0, 0);

    void Update()
    {
        float movementAmount = movementSpeed * Time.deltaTime;


        if (keyboardController)
        {
            if (Input.GetKey(KeyCode.W))
            {
                SerialReader.GPSY += movementAmount;
            }
            if (Input.GetKey(KeyCode.A))
            {
                SerialReader.GPSX += -movementAmount;
            }
            if (Input.GetKey(KeyCode.S))
            {
                SerialReader.GPSY += -movementAmount;
            }
            if (Input.GetKey(KeyCode.D))
            {
                SerialReader.GPSX += movementAmount;
            }
            if (Input.GetKey(KeyCode.Space))
            {
                SerialReader.Altimeter += movementAmount;
            }
            if (Input.GetKey(KeyCode.C))
            {
                SerialReader.Altimeter += -movementAmount;
            }

            // Handle rotation inputs
            float rotationAmount = rotationSpeed * Time.deltaTime;

            if (Input.GetKey(KeyCode.K))
            {
                // Rotate up
                //compassHalo.Rotate(Vector3.right, rotationAmount);
                rotationVector += Vector3.right * rotationAmount;
            }

            if (Input.GetKey(KeyCode.I))
            {
                // Rotate down
                //compassHalo.Rotate(Vector3.left, rotationAmount);
                rotationVector += Vector3.left * rotationAmount;
            }

            if (Input.GetKey(KeyCode.U))
            {
                // Rotate cork screw left
                //compassHalo.Rotate(Vector3.forward, rotationAmount);
                rotationVector += Vector3.forward * rotationAmount;
            }

            if (Input.GetKey(KeyCode.O))
            {
                // Rotate cork screw right
                //compassHalo.Rotate(Vector3.back, rotationAmount);
                rotationVector += Vector3.back * rotationAmount;
            }

            if (Input.GetKey(KeyCode.J))
            {
                SerialReader.heading += rotationAmount;
            }
            if (Input.GetKey(KeyCode.L))
            {
                SerialReader.heading -= rotationAmount;
            }

            Quaternion rotation = Quaternion.Euler(rotationVector);
            //Debug.Log("controller: " + rotation.x + " " + rotation.y + " " + rotation.z + " " + rotation.w);

            SerialReader.IMU = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
        }

        Vector3 target = new Vector3(SerialReader.GPSX, SerialReader.Altimeter, SerialReader.GPSY);

        //Debug.Log("target: " + SerialReader.GPSX + " " + SerialReader.Altimeter + " " + SerialReader.GPSY + "\n");
        //Debug.Log("transform.position: " + transform.position.x + " " + transform.position.y + " " + transform.position.z);

        transform.position = Vector3.MoveTowards(transform.position, target, movementAmount);
        camera.position = Vector3.MoveTowards(camera.position, target, movementAmount);

        compassHalo.rotation = Quaternion.Inverse(SerialReader.IMU);
        compassHalo.Rotate(0, -SerialReader.heading, 0);

        windHalo.rotation = compassHalo.rotation;

    }
}
