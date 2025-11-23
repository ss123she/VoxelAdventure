using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        transform.SetPositionAndRotation(new Vector3(transform.position.x, transform.position.y, transform.position .z + 1 * Time.deltaTime), transform.rotation);
    }
}
