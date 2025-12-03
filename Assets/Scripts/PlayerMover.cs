using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    [SerializeField] private float speed = 100f;

    private void Update() =>
        transform.SetPositionAndRotation(new Vector3(transform.position.x, transform.position.y, transform.position.z + speed * Time.deltaTime), transform.rotation);
}
