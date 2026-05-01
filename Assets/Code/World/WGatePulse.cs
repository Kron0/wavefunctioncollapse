using UnityEngine;

// Animates a W-gate: pulses the emissive glow, bobs orbiting particles,
// and plays a proximity hum. Attached by WGatePlacer to the wall GameObject.
[RequireComponent(typeof(Renderer))]
public class WGatePulse : MonoBehaviour {
	public Color Accent = Color.cyan;
	public int   WGateLayer;

	private Renderer   wallRenderer;
	private Light      gateLight;
	private Transform[] orbs;
	private AudioSource proximityHum;
	private Transform   player;
	private float       baseIntensity;
	private float       timeOffset;

	private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

	void Start() {
		this.wallRenderer  = this.GetComponent<Renderer>();
		this.timeOffset    = (this.transform.position.x * 1.37f + this.transform.position.z * 2.11f) % (Mathf.PI * 2f);

		// Point light parented to gate
		var lightGO = new GameObject("GateLight");
		lightGO.transform.SetParent(this.transform);
		lightGO.transform.localPosition = Vector3.zero;
		this.gateLight  = lightGO.AddComponent<Light>();
		this.gateLight.type      = LightType.Point;
		this.gateLight.color     = this.Accent;
		this.gateLight.range     = AbstractMap4D.BLOCK_SIZE * 2.2f;
		this.baseIntensity       = 1.2f;
		this.gateLight.intensity = this.baseIntensity;

		// Orbiting energy orbs — parented to the map root (not wall) to avoid non-uniform scale distortion
		this.orbs = new Transform[4];
		float bs = AbstractMap4D.BLOCK_SIZE;
		Transform orbParent = this.transform.parent != null ? this.transform.parent : this.transform;
		for (int i = 0; i < this.orbs.Length; i++) {
			var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			orb.name = "GateOrb" + i;
			orb.transform.SetParent(orbParent);
			orb.transform.localScale = Vector3.one * 0.055f * bs;
			Object.Destroy(orb.GetComponent<Collider>());

			var mat = new Material(Shader.Find("Standard"));
			mat.color = this.Accent * 0.2f;
			mat.SetColor(EmissionColorID, this.Accent * 4f);
			mat.EnableKeyword("_EMISSION");
			orb.GetComponent<Renderer>().sharedMaterial = mat;
			this.orbs[i] = orb.transform;
		}

		// Proximity hum — synthesised drone
		this.proximityHum = this.gameObject.AddComponent<AudioSource>();
		this.proximityHum.clip         = this.MakeDrone(WGateLayer);
		this.proximityHum.loop         = true;
		this.proximityHum.spatialBlend = 1f;
		this.proximityHum.minDistance  = 1f;
		this.proximityHum.maxDistance  = AbstractMap4D.BLOCK_SIZE * 4f;
		this.proximityHum.rolloffMode  = AudioRolloffMode.Linear;
		this.proximityHum.volume       = 0.0f;
		this.proximityHum.Play();

		var playerGO = GameObject.FindGameObjectWithTag("Player");
		if (playerGO != null) this.player = playerGO.transform;
	}

	void Update() {
		float t = Time.time + this.timeOffset;

		// When player is at the gate's layer, gate is passable — reduce orb intensity
		bool open  = MapBehaviour4D.ActiveWLayer == this.WGateLayer;
		float fade = open ? 0.30f : 1.00f;

		// Pulse: slow breathing rhythm + a faster secondary shimmer
		float breath  = 0.5f + 0.5f * Mathf.Sin(t * 1.4f);
		float shimmer = 0.5f + 0.5f * Mathf.Sin(t * 7.3f) * 0.30f;
		float pulse   = (breath + shimmer * 0.25f) * fade;

		// Emissive glow
		if (this.wallRenderer != null) {
			Color em = this.Accent * (0.5f + pulse * 1.6f);
			this.wallRenderer.material.SetColor(EmissionColorID, em);
		}

		// Light intensity
		if (this.gateLight != null) {
			this.gateLight.intensity = this.baseIntensity * (0.6f + pulse * 0.8f);
		}

		// Orbiting orbs: ellipse in world space centred on the gate
		float bs = AbstractMap4D.BLOCK_SIZE;
		float orbitR = bs * 0.36f;
		Vector3 gateCenter = this.transform.position;
		// Gate's local right and up axes (gate faces along its Z)
		Vector3 right = this.transform.right;
		Vector3 up    = Vector3.up;
		for (int i = 0; i < this.orbs.Length; i++) {
			if (this.orbs[i] == null) continue;
			float angle  = t * 1.8f + i * (Mathf.PI * 2f / this.orbs.Length);
			float yBob   = Mathf.Sin(t * 2.6f + i * 1.5f) * bs * 0.12f;
			this.orbs[i].position = gateCenter
				+ right * (Mathf.Cos(angle) * orbitR)
				+ up    * (Mathf.Sin(angle) * orbitR * 0.7f + yBob);
			float orbScale = (0.8f + 0.2f * Mathf.Sin(t * 3.1f + i)) * bs * 0.055f;
			this.orbs[i].localScale = Vector3.one * orbScale;
		}

		// Proximity hum volume
		if (this.proximityHum != null && this.player != null) {
			float dist = Vector3.Distance(this.transform.position, this.player.position);
			float maxD = AbstractMap4D.BLOCK_SIZE * 3.5f;
			this.proximityHum.volume = Mathf.Lerp(0.28f, 0f, dist / maxD);
		}
	}

	private AudioClip MakeDrone(int wLayer) {
		const int sampleRate = 22050;
		const float duration = 3f;
		int n = Mathf.RoundToInt(sampleRate * duration);
		var clip = AudioClip.Create("gate_drone", n, 1, sampleRate, false);
		var buf  = new float[n];

		// Frequency chosen from W-layer accent palette analogy: low drone per layer
		float[] freqs = { 55f, 65.4f, 73.4f, 82.4f, 98f, 110f };
		float freq = freqs[((wLayer % freqs.Length) + freqs.Length) % freqs.Length];

		for (int i = 0; i < n; i++) {
			float t   = (float)i / sampleRate;
			float lfo = 1f + 0.015f * Mathf.Sin(2f * Mathf.PI * 0.3f * t);
			buf[i] = (Mathf.Sin(2f * Mathf.PI * freq * lfo * t) * 0.55f
			        + Mathf.Sin(2f * Mathf.PI * freq * 2f   * t) * 0.20f
			        + Mathf.Sin(2f * Mathf.PI * freq * 3.1f * t) * 0.08f) * 0.65f;
		}
		clip.SetData(buf, 0);
		return clip;
	}
}
