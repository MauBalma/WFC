using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public Vector3 lookAtOffset = new Vector3(0, 4, 0);
    public float rotationSpeed = 30f;

    private Vector3 offset;
    
    void Start()
    {
        offset =  transform.position - target.position;
    }

    void LateUpdate()
    {
        offset = Quaternion.Euler(0, Time.deltaTime * rotationSpeed, 0) * offset;
        transform.position = target.position + offset;
        transform.LookAt(target.position + lookAtOffset, Vector3.up);
    }
}
