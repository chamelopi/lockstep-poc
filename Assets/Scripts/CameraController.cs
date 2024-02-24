using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private float smoothTime;
    [SerializeField]
    private Vector2 heightRange;
    [SerializeField]
    private Vector2 angleRange;
    [SerializeField]
    private float minHeightAboveGround;
    [SerializeField]
    private LayerMask terrainLayers;
    [SerializeField]
    private float panSpeed = 0.1f;
    [SerializeField]
    private float minPanDistance = 0.25f;
    [SerializeField]
    private float moveSpeed = 0.75f;
    [SerializeField]
    private float zoomSpeed = 1.0f;
    [SerializeField]
    private bool invertZoom = false;

    [SerializeField]
    private float followStep = 0.0001f;
    private float progress;
    private bool following;

    private Vector2? panStart = null;
    private Vector3 moveVelocity;
    private float targetHeight;

    private void Awake()
    {
        this.targetHeight = this.transform.position.y;
    }

    private void Update()
    {

        this.UpdateMovement();
    }

    private void UpdateMovement()
    {
        float zoomInput = (this.invertZoom ? 1 : -1) * Input.GetAxis("Mouse ScrollWheel") * this.zoomSpeed;
        Vector2 moveInput = this.GetMoveInput();

        if (moveInput.magnitude != 0 || zoomInput != 0.0f)
        {
            Vector3 position = this.transform.position;
            if (zoomInput != 0.0f)
            {
                this.targetHeight = position.y + zoomInput;
            }

            Vector3 input = new Vector3(moveInput.x, zoomInput, moveInput.y);

            position = Vector3.SmoothDamp(position, position + input, ref this.moveVelocity, Time.unscaledDeltaTime * this.smoothTime);
            position = new Vector3(position.x, Mathf.Clamp(position.y, this.heightRange.x, this.heightRange.y), position.z);

            //check if is too close or too far away from ground
            const float origin = 100.0f;
            if (Physics.Raycast(position + new Vector3(0, origin, 0), Vector3.down, out RaycastHit hit, this.minHeightAboveGround + 2 * origin, this.terrainLayers)
               && (hit.point - position).magnitude < this.minHeightAboveGround)
            {
                float height = Mathf.Max(this.targetHeight, hit.point.y + this.minHeightAboveGround);
                position = new Vector3(position.x, height, position.z);
            }
            
            this.transform.position = position;
            this.UpdateCameraTilt();
        }
    }

    private Vector2 GetMoveInput()
    {
        Vector2 moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * this.moveSpeed;

        if (Input.GetMouseButtonDown(2))
        {
            this.panStart = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(2))
        {
            this.panStart = null;
        }

        if (this.panStart.HasValue)
        {
            Vector2 move = ((Vector2)Input.mousePosition - this.panStart.Value);
            if (move.magnitude > this.minPanDistance)
            {
                moveInput = move * this.panSpeed;
            }
        }

        return moveInput;
    }

    private void UpdateCameraTilt()
    {
        float angle = Mathf.Pow(this.transform.position.y - this.heightRange.x, 0.8f) + this.angleRange.x;
        Vector3 goal = new Vector3(Mathf.Clamp(angle, this.angleRange.x, this.angleRange.y), 0, 0);
        this.transform.rotation = Quaternion.Euler(goal);
    }
}