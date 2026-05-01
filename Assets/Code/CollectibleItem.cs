using UnityEngine;

public class CollectibleItem : MonoBehaviour {
	public int requiredWLayer;

	private bool collected;
	private Transform player;

	// Animation references
	private Transform core;
	private Transform ring;
	private Transform glowSphere;
	private Light itemLight;
	private float originY;
	private float timeOffset;

	// Audio
	private AudioSource ambientSource;

	private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
	private static readonly Color GoldEmission = DimensionColors.ArtifactGold;

	void Start() {
		var playerGO = GameObject.FindGameObjectWithTag("Player");
		if (playerGO != null) this.player = playerGO.transform;

		this.core       = this.transform.Find("Core");
		this.ring       = this.transform.Find("Ring");
		this.glowSphere = this.transform.Find("Glow");
		this.itemLight  = this.GetComponentInChildren<Light>();
		this.originY    = this.transform.position.y;
		this.timeOffset = (this.transform.position.x * 1.3f + this.transform.position.z * 0.7f) % (Mathf.PI * 2f);

		// Ambient hum — spatial, very quiet, loops the 2s generated clip
		this.ambientSource = this.gameObject.AddComponent<AudioSource>();
		this.ambientSource.clip         = CollectibleAudio.GetAmbient(this.requiredWLayer);
		this.ambientSource.loop         = true;
		this.ambientSource.volume       = 0.08f;
		this.ambientSource.spatialBlend = 1f;   // fully 3D
		this.ambientSource.minDistance  = 2f;
		this.ambientSource.maxDistance  = 20f;
		this.ambientSource.rolloffMode  = AudioRolloffMode.Linear;
		this.ambientSource.Play();
	}

	void Update() {
		if (this.collected) return;

		// ── Animation ────────────────────────────────────────────────────────
		float t = Time.time + this.timeOffset;

		Vector3 p = this.transform.position;
		p.y = this.originY + Mathf.Sin(t * 1.7f) * 0.18f;
		this.transform.position = p;

		if (this.core != null) {
			this.core.Rotate(50f * Time.deltaTime, 35f * Time.deltaTime, 0f, Space.Self);
		}
		if (this.ring != null) {
			this.ring.Rotate(0f, -65f * Time.deltaTime, 0f, Space.World);
		}
		if (this.itemLight != null) {
			float pulse = 0.5f + 0.5f * Mathf.Sin(t * 2.8f);
			this.itemLight.intensity = 0.4f + pulse * 0.8f;
		}
		if (this.glowSphere != null) {
			float pulse = 0.5f + 0.5f * Mathf.Sin(t * 2.8f);
			var ren = this.glowSphere.GetComponent<Renderer>();
			if (ren != null) {
				ren.material.SetColor(EmissionColorID, GoldEmission * (3f + pulse * 4f));
			}
		}

		// ── Collection check ─────────────────────────────────────────────────
		if (this.player == null) return;
		if (MapBehaviour4D.ActiveWLayer != this.requiredWLayer) return;
		if (Vector3.Distance(this.transform.position, this.player.position) > 1.8f) return;

		this.collected = true;

		// Play chime at player position (non-spatial, always audible)
		var chime = CollectibleAudio.GetChime(CollectiblePlacer.TotalCollected);
		AudioSource.PlayClipAtPoint(chime, this.player.position, 0.65f);

		CollectiblePlacer.TotalCollected++;
		Destroy(this.gameObject);
	}
}
