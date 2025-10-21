using UnityEngine;

public class FreeFlyCameraTarget : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float boostMultiplier = 3f;

    [Header("Rotation")]
    public float mouseSensitivity = 2f;

    float _rotationX;
    float _rotationY;

    void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    void HandleMovement()
    {
        float speed = moveSpeed;

        if (Input.GetKey(KeyCode.LeftShift))
            speed *= boostMultiplier;

        Vector3 direction = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) direction += transform.forward;
        if (Input.GetKey(KeyCode.S)) direction -= transform.forward;
        if (Input.GetKey(KeyCode.A)) direction -= transform.right;
        if (Input.GetKey(KeyCode.D)) direction += transform.right;
        if (Input.GetKey(KeyCode.E)) direction += transform.up;
        if (Input.GetKey(KeyCode.Q)) direction -= transform.up;

        transform.position += direction.normalized * speed * Time.deltaTime;
    }

    void HandleRotation()
    {
        if (Input.GetMouseButton(1))
        {
            _rotationX += Input.GetAxis("Mouse X") * mouseSensitivity;
            _rotationY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            _rotationY = Mathf.Clamp(_rotationY, -90f, 90f);

            transform.rotation = Quaternion.Euler(_rotationY, _rotationX, 0f);
        }
    }
}
