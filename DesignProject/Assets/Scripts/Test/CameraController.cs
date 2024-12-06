using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] Transform followTarget;

    [SerializeField] float distance = 5f;
    [SerializeField] float rotationSpeed = 2f;

    public float rotationY;
    public float rotationX;

    [SerializeField] float minVerticalAngle = -45f;
    [SerializeField] float maxVerticalAngle =  45f;

    [SerializeField] Vector2 framingOffset;

    [SerializeField] bool invertX;
    [SerializeField] bool invertY;

     float invertXVal;
     float invertYVal;

    private void Start()
    {
        //Fare imlecini gizledim
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }


    private void Update()
    {
       invertXVal = (invertX) ? -1 : 1;
       invertYVal = (invertY) ? -1 : 1;


        //Yatay kamera dönüþ için
        rotationY += Input.GetAxis("Mouse X")*invertXVal * rotationSpeed;

        //Dikey kamera dönüþü için
        rotationX += Input.GetAxis("Mouse Y") *invertYVal* rotationSpeed;
        rotationX = Mathf.Clamp(rotationX,minVerticalAngle,maxVerticalAngle);

        var targetRotation = Quaternion.Euler(rotationX, rotationY, 0f);

        var focusPosition = followTarget.position + new Vector3(framingOffset.x,framingOffset.y);
        transform.position = focusPosition - targetRotation * new Vector3(0,0,distance);

        //Kameranýn karaktere bakmasýný saðladým
        transform.rotation = targetRotation;
    }

    public Quaternion PlanarRotation => Quaternion.Euler(0,rotationY, 0);


}
