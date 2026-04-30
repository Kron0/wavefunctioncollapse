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

	// How quickly velocity ramps up / decays (higher = snappier, lower = floatier)
	[Range(2f, 20f)]
	public float Acceleration = 10f;

	[Range(2f, 20f)]
	public float Deceleration = 12f;

	[Range(2f, 20f)]
	public float WAcceleration = 6f;

	public float WPosition { get; private set; }

	private CharacterController characterController;
	private Transform cameraTransform;

	private float cameraTilt  = 0f;
	private float verticalSpeed = 0f;
	private float timeInAir   = 0f;
	private bool  jumpLocked  = false;

	// Inertia state
	private Vector3 moveVelocity  = Vector3.zero;  // current XZ velocity in world space
	private float   wVelocity     = 0f;            // current W velocity

	private bool physicsEnabled    = false;
	private float physicsEnableDelay = 0.5f;

	public LayerMask CollisionLayers;

	void OnEnable() {
		this.characterController = this.GetComponent<CharacterController>();
		this.cameraTransform = this.GetComponentInChildren<Camera>().transform;
		this.cameraTilt = this.cameraTransform.localRotation.eulerAngles.x;
		this.WPosition = 0f;
		if (this.CollisionLayers.value == 0) {
			this.CollisionLayers = ~0;
		}
	}

	void Update() {
		if (!WLayerHUD.StartupComplete) return;
		if (PauseMenu.IsPaused) return;

		if (!this.physicsEnabled) {
			this.physicsEnableDelay -= Time.deltaTime;
			if (this.physicsEnableDelay <= 0f) {
				if (this.SnapToGround()) {
					this.physicsEnabled = true;
				} else {
					this.physicsEnableDelay = 0f;
				}
			}
			return;
		}

		bool  touchesGround  = this.onGround();
		float runMultiplier  = 1f + 2f * Input.GetAxis("Run");
		float dt             = Time.deltaTime;

		// ── XZ movement with inertia ─────────────────────────────────────────
		Vector3 inputDir = this.transform.forward * Input.GetAxis("Move Y")
			+ this.transform.right * Input.GetAxis("Move X");
		if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

		Vector3 targetVel = inputDir * this.MovementSpeed * runMultiplier;
		float   blend     = inputDir.sqrMagnitude > 0.01f ? this.Acceleration : this.Deceleration;
		this.moveVelocity = Vector3.Lerp(this.moveVelocity, targetVel, dt * blend);

		float yBefore = this.transform.position.y;
		this.characterController.Move(this.moveVelocity * dt);
		// Prevent being pushed up by geometry
		float yMoved = this.transform.position.y - yBefore;
		if (yMoved < 0f) this.transform.position += Vector3.down * yMoved;

		// ── Look ─────────────────────────────────────────────────────────────
		this.transform.localRotation = Quaternion.AngleAxis(
			Input.GetAxis("Mouse Look X") * this.MouseSensitivity
			+ Input.GetAxis("Look X") * this.LookSensitivity * dt,
			Vector3.up) * this.transform.rotation;

		this.cameraTilt = Mathf.Clamp(
			this.cameraTilt
			- Input.GetAxis("Mouse Look Y") * this.MouseSensitivity
			- Input.GetAxis("Look Y") * this.LookSensitivity * dt,
			-90f, 90f);
		this.cameraTransform.localRotation = Quaternion.AngleAxis(this.cameraTilt, Vector3.right);

		// ── Vertical / jump / gravity ────────────────────────────────────────
		if (touchesGround) {
			this.timeInAir = 0f;
		} else {
			this.timeInAir += dt;
		}

		if (touchesGround && this.verticalSpeed < 0f) {
			this.verticalSpeed = 0f;
		} else {
			this.verticalSpeed -= 9.18f * dt;
		}

		if (Input.GetAxisRaw("Jump") < 0.1f) this.jumpLocked = false;
		if (!this.jumpLocked && this.timeInAir < 0.5f && Input.GetAxisRaw("Jump") > 0.1f) {
			this.timeInAir    = 0.5f;
			this.verticalSpeed = this.JumpStrength;
			this.jumpLocked   = true;
		}
		if (Input.GetAxisRaw("Jetpack") > 0.1f) this.verticalSpeed = 2f;

		this.characterController.Move(Vector3.up * dt * this.verticalSpeed);

		// ── W axis with inertia ───────────────────────────────────────────────
		float wInput = 0f;
		if (Input.GetKey(KeyCode.E)) wInput =  1f;
		else if (Input.GetKey(KeyCode.Q)) wInput = -1f;

		float targetWVel  = wInput * this.WSpeed;
		this.wVelocity    = Mathf.Lerp(this.wVelocity, targetWVel, dt * this.WAcceleration);
		this.WPosition   += this.wVelocity * dt;
	}

	public void ClampWPosition(int targetLayer) {
		this.WPosition = Mathf.MoveTowards(this.WPosition, targetLayer, this.WSpeed * Time.deltaTime * 2f);
		// Bleed off W velocity toward zero so inertia doesn't fight the clamp
		this.wVelocity = Mathf.MoveTowards(this.wVelocity, 0f, this.WSpeed * Time.deltaTime * 4f);
	}

	public bool SnapToGround() {
		RaycastHit hit;
		Vector3 castOrigin = this.transform.position + Vector3.up * 5f;
		if (Physics.Raycast(castOrigin, Vector3.down, out hit, 30f, this.CollisionLayers)) {
			this.characterController.enabled = false;
			this.transform.position = hit.point + Vector3.up * (this.characterController.height / 2f + 0.05f);
			this.characterController.enabled = true;
			this.verticalSpeed = 0f;
			this.moveVelocity  = Vector3.zero;
			return true;
		}
		return false;
	}

	private bool onGround() {
		var ray = new Ray(this.transform.position, Vector3.down);
		return Physics.SphereCast(ray, this.characterController.radius,
			this.characterController.height / 2f - this.characterController.radius + 0.1f,
			this.CollisionLayers);
	}
}
