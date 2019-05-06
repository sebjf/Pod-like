using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrimitiveGun : MonoBehaviour
{
    public float speed;
    public float mass;
    public GameObject target;

    private Vector3 previousmouse;
    private float distance;
    private float height;

    // Start is called before the first frame update
    void Start()
    {
        distance = (transform.position - target.transform.position).magnitude;
        height = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        var delta = Input.mousePosition - previousmouse;
        previousmouse = Input.mousePosition;

        if(Input.GetMouseButtonDown(0))
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var rb = obj.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            obj.transform.position = transform.position;

            var trajectory = GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);

            rb.velocity = trajectory.direction * speed;
        }

        if(Input.GetMouseButton(1))
        {
            transform.RotateAround(target.transform.position, Vector3.up, 10f * 0.1f);
        }

        var correction = (transform.position - target.transform.position).magnitude - distance;

        transform.position += transform.forward * correction;

        transform.position = new Vector3(transform.position.x, height, transform.position.z);
        transform.LookAt(target.transform.position + Vector3.up * 1f, Vector3.up);
    }
}
