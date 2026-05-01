using System.Collections;
using UnityEngine;

public class CollectibleItem : MonoBehaviour {
	public int requiredWLayer;

	public static event System.Action<Vector3, int, int> OnCollected; // worldPos, count, total

	private bool collected;
	private Transform player;

	// Animation references
	private Transform core;
	private Transform ring;
	private Transform glowSphere;
	private Renderer  glowRenderer;
	private Light     itemLight;
	private float     originY;
	private float     timeOffset;

	// Audio
	private AudioSource ambientSource;

	private static readonly int   EmissionColorID = Shader.PropertyToID("_EmissionColor");
	private static readonly Color GoldEmission    = DimensionColors.ArtifactGold;

	void Start() {
		var playerGO = GameObject.FindGameObjectWithTag("Player");
		if (playerGO != null) this.player = playerGO.transform;

		this.core        = this.transform.Find("Core");
		this.ring        = this.transform.Find("Ring");
		this.glowSphere  = this.transform.Find("Glow");
		this.glowRenderer = this.glowSphere != null ? this.glowSphere.GetComponent<Renderer>() : null;
		this.itemLight   = this.GetComponentInChildren<Light>();
		this.originY     = this.transform.position.y;
		this.timeOffset  = (this.transform.position.x * 1.3f + this.transform.position.z * 0.7f) % (Mathf.PI * 2f);

		this.ambientSource = this.gameObject.AddComponent<AudioSource>();
		this.ambientSource.clip         = CollectibleAudio.GetAmbient(this.requiredWLayer);
		this.ambientSource.loop         = true;
		this.ambientSource.volume       = 0.08f;
		this.ambientSource.spatialBlend = 1f;
		this.ambientSource.minDistance  = 2f;
		this.ambientSource.maxDistance  = 20f;
		this.ambientSource.rolloffMode  = AudioRolloffMode.Linear;
		this.ambientSource.Play();
	}

	void Update() {
		if (this.collected) return;

		// ── Idle animation ────────────────────────────────────────────────────
		float t = Time.time + this.timeOffset;

		Vector3 p = this.transform.position;
		p.y = this.originY + Mathf.Sin(t * 1.7f) * 0.18f;
		this.transform.position = p;

		if (this.core != null)
			this.core.Rotate(50f * Time.deltaTime, 35f * Time.deltaTime, 0f, Space.Self);
		if (this.ring != null)
			this.ring.Rotate(0f, -65f * Time.deltaTime, 0f, Space.World);
		if (this.itemLight != null) {
			float pulse = 0.5f + 0.5f * Mathf.Sin(t * 2.8f);
			this.itemLight.intensity = 0.4f + pulse * 0.8f;
		}
		if (this.glowRenderer != null) {
			float pulse = 0.5f + 0.5f * Mathf.Sin(t * 2.8f);
			this.glowRenderer.material.SetColor(EmissionColorID, GoldEmission * (3f + pulse * 4f));
		}

		// ── Collection check ──────────────────────────────────────────────────
		if (this.player == null) return;
		if (MapBehaviour4D.ActiveWLayer != this.requiredWLayer) return;
		if (Vector3.Distance(this.transform.position, this.player.position) > 1.8f) return;

		this.collected = true;
		this.ambientSource.Stop();

		var chime = CollectibleAudio.GetChime(CollectiblePlacer.TotalCollected);
		AudioSource.PlayClipAtPoint(chime, this.player.position, 0.65f);

		CollectiblePlacer.TotalCollected++;
		OnCollected?.Invoke(this.transform.position, CollectiblePlacer.TotalCollected, CollectiblePlacer.TotalPlaced);

		StartCoroutine(this.CollectAnimation());
	}

	// =========================================================================
	// COLLECTION ANIMATION
	// =========================================================================

	private IEnumerator CollectAnimation() {
		Vector3 startPos  = this.transform.position;
		Vector3 targetPos = startPos + Vector3.up * 0.9f;

		this.SpawnRipples(startPos);

		// Phase 1 — flash surge (0.08 s)
		float phase1 = 0.08f;
		for (float e = 0f; e < phase1; e += Time.deltaTime) {
			float frac = e / phase1;
			if (this.itemLight != null) {
				this.itemLight.intensity = Mathf.Lerp(1.2f, 20f, frac);
				this.itemLight.range     = Mathf.Lerp(6f,   18f, frac);
			}
			if (this.glowRenderer != null) {
				this.glowRenderer.material.SetColor(EmissionColorID, Color.white * Mathf.Lerp(8f, 40f, frac));
			}
			yield return null;
		}

		// Phase 2 — rise (0.27 s, ease-out)
		float phase2 = 0.27f;
		for (float e = 0f; e < phase2; e += Time.deltaTime) {
			float raw  = e / phase2;
			float ease = 1f - (1f - raw) * (1f - raw);
			this.transform.position   = Vector3.Lerp(startPos, targetPos, ease);
			this.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.5f, ease);
			if (this.core != null) this.core.Rotate(240f * Time.deltaTime, 140f * Time.deltaTime, 0f, Space.Self);
			if (this.ring != null) this.ring.Rotate(0f, -320f * Time.deltaTime, 0f, Space.World);
			yield return null;
		}

		// Phase 3 — implode (0.22 s, ease-in)
		float phase3 = 0.22f;
		for (float e = 0f; e < phase3; e += Time.deltaTime) {
			float frac = e / phase3;
			float ease = frac * frac;
			this.transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 0f, ease);
			if (this.itemLight != null) {
				this.itemLight.intensity = Mathf.Lerp(20f, 0f, frac);
				this.itemLight.range     = Mathf.Lerp(18f, 26f, frac);
			}
			yield return null;
		}

		// Brief wait so the last ripple coroutine can clean itself up before we go
		yield return new WaitForSeconds(0.18f);

		Destroy(this.gameObject);
	}

	// =========================================================================
	// RIPPLE RINGS
	// =========================================================================

	private void SpawnRipples(Vector3 origin) {
		for (int i = 0; i < 3; i++) {
			StartCoroutine(this.AnimateRipple(origin, i * 0.07f));
		}
	}

	private IEnumerator AnimateRipple(Vector3 origin, float delay) {
		if (delay > 0f) yield return new WaitForSeconds(delay);

		var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		go.name = "CollectRipple";
		Object.Destroy(go.GetComponent<Collider>());
		go.transform.position   = origin;
		go.transform.localScale = new Vector3(0.05f, 0.004f, 0.05f);

		var mat = new Material(Shader.Find("Standard"));
		mat.color = new Color(GoldEmission.r, GoldEmission.g, GoldEmission.b, 0.75f);
		mat.SetFloat("_Mode", 3f);
		mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		mat.SetInt("_ZWrite", 0);
		mat.DisableKeyword("_ALPHATEST_ON");
		mat.EnableKeyword("_ALPHABLEND_ON");
		mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
		mat.renderQueue = 3000;
		mat.SetColor(EmissionColorID, GoldEmission * 2.5f);
		mat.EnableKeyword("_EMISSION");
		go.GetComponent<Renderer>().material = mat;

		float duration = 0.48f;
		for (float e = 0f; e < duration; e += Time.deltaTime) {
			float frac  = e / duration;
			float eased = 1f - (1f - frac) * (1f - frac);
			float r     = Mathf.Lerp(0.05f, 4f, eased);
			go.transform.localScale = new Vector3(r, 0.004f, r);
			mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, Mathf.Lerp(0.75f, 0f, frac));
			yield return null;
		}

		Object.Destroy(go);
	}
}
