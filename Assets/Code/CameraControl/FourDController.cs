using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FourDController : MonoBehaviour {

	[Range(1f, 5f)]
	public float MovementSpeed = 1f;

	[Range(1, 500f)]
	public float LookSensitivity = 200f;

	[Range(1, 500f)]
	public float MouseSensitivity = 3;

	[Range(1, 100f)]
	public float JumpStrength = 2f;

	[Range(0.5f, 5f)]
	public float WSpeed = 1.5f;

	public float WPosition { get; private set; }

	private CharacterController characterController;
	private Transform cameraTransform;

	private float cameraTilt = 0f;
	private float verticalSpeed = 0f;
	private float timeInAir = 0f;
	private bool jumpLocked = false;

	private bool physicsEnabled = false;
	private float physicsEnableDelay = 0.5f;

	public LayerMask CollisionLayers;

	void OnEnable() {
		this.characterController = this.GetComponent<CharacterController>();
		this.cameraTransform = this.GetComponentInChildren<Camera>().transform;
		this.cameraTilt = this.cameraTransform.localRotation.eulerAngles.x;
		this.WPosition = 0f;
	}

	void Update() {
		if (!this.physicsEnabled) {
			this.physicsEnableDelay -= Time.deltaTime;
			if (this.physicsEnableDelay <= 0f) {
				if (this.SnapToGround()) {
					this.physicsEnabled = true;
				} else {
					// Keep retrying each frame until ground exists
					this.physicsEnableDelay = 0f;
				}
			}
			return;
		}

		bool touchesGround = this.onGround();
		float runMultiplier = 1f + 2f * Input.GetAxis("Run");
		float y = this.transform.position.y;

		Vector3 movementVector = this.transform.forward * Input.GetAxis("Move Y") + this.transform.right * Input.GetAxis("Move X");
		if (movementVector.sqrMagnitude > 1) {
			movementVector.Normalize();
		}
		this.characterController.Move(movementVector * Time.deltaTime * this.MovementSpeed * runMultiplier);
		float verticalMovement = this.transform.position.y - y;
		if (verticalMovement < 0) {
			this.transform.position += Vector3.down * verticalMovement;
		}

		this.transform.localRotation = Quaternion.AngleAxis(Input.GetAxis("Mouse Look X") * this.MouseSensitivity + Input.GetAxis("Look X") * this.LookSensitivity * Time.deltaTime, Vector3.up) * this.transform.rotation;
		this.cameraTilt = Mathf.Clamp(this.cameraTilt - Input.GetAxis("Mouse Look Y") * this.MouseSensitivity - Input.GetAxis("Look Y") * this.LookSensitivity * Time.deltaTime, -90f, 90f);
		this.cameraTransform.localRotation = Quaternion.AngleAxis(this.cameraTilt, Vector3.right);

		if (touchesGround) {
			this.timeInAir = 0;
		} else {
			this.timeInAir += Time.deltaTime;
		}

		if (touchesGround && this.verticalSpeed < 0) {
			this.verticalSpeed = 0;
		} else {
			this.verticalSpeed -= 9.18f * Time.deltaTime;
		}
		if (Input.GetAxisRaw("Jump") < 0.1f) {
			this.jumpLocked = false;
		}
		if (!this.jumpLocked && this.timeInAir < 0.5f && Input.GetAxisRaw("Jump") > 0.1f) {
			this.timeInAir = 0.5f;
			this.verticalSpeed = this.JumpStrength;
			this.jumpLocked = true;
		}
		if (Input.GetAxisRaw("Jetpack") > 0.1f) {
			this.verticalSpeed = 2f;
		}
		this.characterController.Move(Vector3.up * Time.deltaTime * this.verticalSpeed);

		float wInput = 0f;
		if (Input.GetKey(KeyCode.E)) {
			wInput = 1f;
		} else if (Input.GetKey(KeyCode.Q)) {
			wInput = -1f;
		}
		this.WPosition += wInput * this.WSpeed * Time.deltaTime;
	}

	public void ClampWPosition(int targetLayer) {
		// Smoothly push W back toward the safe layer
		this.WPosition = Mathf.MoveTowards(this.WPosition, targetLayer, this.WSpeed * Time.deltaTime * 2f);
	}

	public bool SnapToGround() {
		RaycastHit hit;
		Vector3 castOrigin = this.transform.position + Vector3.up * 5f;
		if (Physics.Raycast(castOrigin, Vector3.down, out hit, 30f, this.CollisionLayers)) {
			this.characterController.enabled = false;
			this.transform.position = hit.point + Vector3.up * (this.characterController.height / 2f + 0.05f);
			this.characterController.enabled = true;
			this.verticalSpeed = 0f;
			return true;
		}
		return false;
	}

	private bool onGround() {
		var ray = new Ray(this.transform.position, Vector3.down);
		return Physics.SphereCast(ray, this.characterController.radius, this.characterController.height / 2 - this.characterController.radius + 0.1f, this.CollisionLayers);
	}
}
