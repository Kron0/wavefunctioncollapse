using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FourDController : MonoBehaviour, IWPositionProvider {

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

	[Header("World")]
	[Tooltip("Number of W-layers. Must match NumWLayers in GenerateMap4DNearPlayer.")]
	public int NumWLayers = 6;

	[Header("Logging")]
	public bool VerboseLogging = false;

	public float WPosition { get; private set; }

	private CharacterController characterController;
	private Transform cameraTransform;

	private float cameraTilt  = 0f;
	private float verticalSpeed = 0f;
	private float timeInAir   = 0f;
	private bool  jumpLocked  = false;

	// Inertia state
	private Vector3 moveVelocity  = Vector3.zero;
	private float   wVelocity     = 0f;

	private bool physicsEnabled      = false;
	private float physicsEnableDelay  = 0.5f;
	private int   snapAttempts        = 0;

	public LayerMask CollisionLayers;

	void OnEnable() {
		this.characterController = this.GetComponent<CharacterController>();
		var cam = this.GetComponentInChildren<Camera>();
		if (cam != null) {
			this.cameraTransform = cam.transform;
			this.cameraTilt = this.cameraTransform.localRotation.eulerAngles.x;
		}
		this.WPosition = 0f;
		if (this.CollisionLayers.value == 0) {
			this.CollisionLayers = ~0;
		}
		GameState.NumWLayers = this.NumWLayers;
	}

	void Update() {
		if (!GameState.StartupComplete) return;
		if (GameState.IsPaused) return;

		if (!this.physicsEnabled) {
			// Wait until enough geometry is built before attempting snap
			if (GameState.BuiltSlotCount < 30) return;
			this.physicsEnableDelay -= Time.deltaTime;
			if (this.physicsEnableDelay <= 0f) {
				this.snapAttempts++;
				if (this.SnapToGround()) {
					this.physicsEnabled = true;
					if (this.VerboseLogging) Debug.Log($"[4D] SnapToGround succeeded on attempt {this.snapAttempts} at {this.transform.position} (BuiltSlots={GameState.BuiltSlotCount})");
				} else {
					this.physicsEnableDelay = 0.5f;
					if (this.VerboseLogging) Debug.Log($"[4D] SnapToGround failed (attempt {this.snapAttempts}), BuiltSlots={GameState.BuiltSlotCount}, retrying in 0.5s");
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

		// Clamp to valid layer range and kill velocity at boundaries
		int maxLayer = Mathf.Max(0, GameState.NumWLayers - 1);
		if (this.WPosition <= 0f) {
			this.WPosition = 0f;
			if (this.wVelocity < 0f) this.wVelocity = 0f;
		} else if (this.WPosition >= maxLayer) {
			this.WPosition = maxLayer;
			if (this.wVelocity > 0f) this.wVelocity = 0f;
		}
	}

	public void ClampWPosition(int targetLayer) {
		this.WPosition = Mathf.MoveTowards(this.WPosition, targetLayer, this.WSpeed * Time.deltaTime * 2f);
		// Bleed off W velocity toward zero so inertia doesn't fight the clamp
		this.wVelocity = Mathf.MoveTowards(this.wVelocity, 0f, this.WSpeed * Time.deltaTime * 4f);
	}

	public bool SnapToGround() {
		RaycastHit hit;
		// Cast from a fixed high altitude so interior ceilings aren't hit first
		Vector3 castOrigin = new Vector3(this.transform.position.x, 200f, this.transform.position.z);
		if (Physics.Raycast(castOrigin, Vector3.down, out hit, 210f, this.CollisionLayers)) {
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
