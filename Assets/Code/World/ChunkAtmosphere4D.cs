using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Spawns neighbourhood atmosphere (lights, creature sounds) per 4D chunk.
// Sub-district theme from DistrictBiasProvider drives which creatures and
// light colour appear in each area.
//
// Also blends global fog and ambient light based on surrounding chunk palettes,
// porting the 3D ChunkAtmosphere logic to the 4D map.
public class ChunkAtmosphere4D : MonoBehaviour, IMapGenerationCallbackReceiver4D {

	// ── Internal state ────────────────────────────────────────────────────────
	private GenerateMap4DNearPlayer generator;
	private MapBehaviour4D          mapBehaviour;

	// All creature/light GOs owned by this component, keyed by chunk
	private readonly Dictionary<Vector4Int, List<GameObject>> chunkObjects
		= new Dictionary<Vector4Int, List<GameObject>>();

	// Emotional notation — dominant sub-district and crossing beat
	private int   dominantSubDistrict = -1;
	private float beatPhase           = 0f;

	// ── Lifecycle ─────────────────────────────────────────────────────────────

	void OnEnable() {
		this.generator    = this.GetComponent<GenerateMap4DNearPlayer>();
		this.mapBehaviour = this.GetComponent<MapBehaviour4D>();
		if (this.generator != null) this.generator.RegisterCallback4D(this);
	}

	void OnDisable() {
		if (this.generator != null) this.generator.UnregisterCallback4D(this);
	}

	// ── Chunk callback ────────────────────────────────────────────────────────

	public void OnGenerateChunk4D(Vector4Int chunkAddress, GenerateMap4DNearPlayer source) {
		// Only populate W=0 — creatures live in XZ-space, not across W layers
		if (chunkAddress.w != 0) return;

		int   chunkSize = source.ChunkSize;
		float blockSize = AbstractMap4D.BLOCK_SIZE;
		float worldChunk = blockSize * chunkSize;

		// Chunk centre in world space
		float cx = (chunkAddress.x + 0.5f) * worldChunk;
		float cz = (chunkAddress.z + 0.5f) * worldChunk;

		int subType = DistrictBiasProvider.GetSubDistrictType(
			chunkAddress.x * chunkSize + chunkSize * 0.5f,
			chunkAddress.z * chunkSize + chunkSize * 0.5f);
		var theme = SubDistrictTheme.Get(subType);

		// Spawn all atmosphere objects as children of this transform
		var objects = new List<GameObject>();
		this.chunkObjects[chunkAddress] = objects;

		// Ground height estimate (top of MapHeight)
		float groundY  = this.mapBehaviour != null
			? this.mapBehaviour.MapHeight * blockSize
			: 12f;
		float streetY  = groundY * 0.15f; // street level approximation

		var rng = new System.Random(chunkAddress.GetHashCode() ^ 0xA7F3C12B);

		// ── Neighbourhood point lights ────────────────────────────────────────
		int lightCount = 2 + rng.Next(2); // 2-3 per chunk
		for (int i = 0; i < lightCount; i++) {
			float lx = cx + ((float)rng.NextDouble() - 0.5f) * worldChunk * 0.8f;
			float lz = cz + ((float)rng.NextDouble() - 0.5f) * worldChunk * 0.8f;
			float ly = streetY + blockSize * 1.5f + (float)rng.NextDouble() * blockSize;

			var lightGO = new GameObject($"AtmLight_{chunkAddress}_{i}");
			lightGO.transform.SetParent(this.transform);
			lightGO.transform.position = new Vector3(lx, ly, lz);

			var lt = lightGO.AddComponent<Light>();
			lt.type      = LightType.Point;
			lt.color     = theme.LightColor;
			lt.intensity = theme.LightIntensity * (0.7f + (float)rng.NextDouble() * 0.6f);
			lt.range     = theme.LightRange     * (0.8f + (float)rng.NextDouble() * 0.4f);
			lt.shadows   = LightShadows.None;

			objects.Add(lightGO);
		}

		// ── Creature emitters ─────────────────────────────────────────────────
		this.SpawnCreatures(objects, theme, cx, cz, streetY, groundY, worldChunk, rng);
	}

	// ── Creature placement ────────────────────────────────────────────────────

	private void SpawnCreatures(List<GameObject> objects, SubDistrictTheme theme,
		float cx, float cz, float streetY, float groundY, float worldChunk, System.Random rng) {

		// Birds — overhead
		for (int i = 0; i < theme.BirdCount; i++) {
			float bx = cx + ((float)rng.NextDouble() - 0.5f) * worldChunk;
			float bz = cz + ((float)rng.NextDouble() - 0.5f) * worldChunk;
			float by = groundY + 15f + (float)rng.NextDouble() * 10f;
			objects.Add(this.MakeCreatureSource(
				$"Bird_{i}", new Vector3(bx, by, bz),
				maxDist: 45f, vol: 0.55f,
				clip:  CreatureAudio.GetBird(theme.BirdPitch, i),
				minInterval: 9f,  maxInterval: 22f,
				clip2: rng.NextDouble() < 0.4f ? CreatureAudio.GetBird(theme.BirdPitch, (i + 1) % 4) : null));
		}

		// Cats — street level, near walls
		for (int i = 0; i < theme.CatCount; i++) {
			float kx = cx + ((float)rng.NextDouble() - 0.5f) * worldChunk * 0.7f;
			float kz = cz + ((float)rng.NextDouble() - 0.5f) * worldChunk * 0.7f;
			objects.Add(this.MakeCreatureSource(
				$"Cat_{i}", new Vector3(kx, streetY, kz),
				maxDist: 22f, vol: 0.50f,
				clip:  CreatureAudio.GetCat(i % 3),
				minInterval: 28f, maxInterval: 70f));
		}

		// Dogs — street level, more energetic
		for (int i = 0; i < theme.DogCount; i++) {
			float dx = cx + ((float)rng.NextDouble() - 0.5f) * worldChunk;
			float dz = cz + ((float)rng.NextDouble() - 0.5f) * worldChunk;
			objects.Add(this.MakeCreatureSource(
				$"Dog_{i}", new Vector3(dx, streetY, dz),
				maxDist: 32f, vol: 0.65f,
				clip:  CreatureAudio.GetDog(i % 3),
				minInterval: 20f, maxInterval: 50f,
				clip2: rng.NextDouble() < 0.6f ? CreatureAudio.GetDog((i + 1) % 3) : null));
		}

		// Mice — very close range, near ground
		for (int i = 0; i < theme.MouseCount; i++) {
			float mx = cx + ((float)rng.NextDouble() - 0.5f) * worldChunk * 0.6f;
			float mz = cz + ((float)rng.NextDouble() - 0.5f) * worldChunk * 0.6f;
			bool  sq  = rng.NextDouble() < 0.25f;
			objects.Add(this.MakeCreatureSource(
				$"Mouse_{i}", new Vector3(mx, streetY - 0.3f, mz),
				maxDist: 12f, vol: 0.40f,
				clip:  CreatureAudio.GetMouse(sq),
				minInterval: 10f, maxInterval: 28f));
		}
	}

	// Creates an invisible AudioSource GO and starts a coroutine that fires it periodically.
	private GameObject MakeCreatureSource(string label, Vector3 pos,
		float maxDist, float vol, AudioClip clip,
		float minInterval, float maxInterval, AudioClip clip2 = null) {

		var go = new GameObject(label);
		go.transform.SetParent(this.transform);
		go.transform.position = pos;

		var src = go.AddComponent<AudioSource>();
		src.playOnAwake    = false;
		src.loop           = false;
		src.spatialBlend   = 1f;
		src.rolloffMode    = AudioRolloffMode.Linear;
		src.minDistance    = 2f;
		src.maxDistance    = maxDist;
		src.volume         = vol;

		StartCoroutine(this.CreatureLoop(src, clip, clip2, minInterval, maxInterval));
		return go;
	}

	// Periodically plays one of the creature clips with a random interval.
	private IEnumerator CreatureLoop(AudioSource src, AudioClip primary, AudioClip alternate,
		float minInterval, float maxInterval) {

		// Stagger startup so chunks don't all fire at t=0
		yield return new WaitForSeconds(UnityEngine.Random.Range(0f, maxInterval));

		while (src != null && src.gameObject != null) {
			if (!GameState.IsPaused && src != null) {
				AudioClip toPlay = (alternate != null && UnityEngine.Random.value < 0.35f)
					? alternate : primary;
				if (toPlay != null) src.PlayOneShot(toPlay);
			}
			yield return new WaitForSeconds(UnityEngine.Random.Range(minInterval, maxInterval));
		}
	}

	// ── Emotional notation — position-granular sub-district atmosphere ────────
	// Blends fog colour, fog density, and ambient light continuously as the
	// player moves between neighbourhood Voronoi regions. Emits a brief ambient
	// "beat" (brightness pulse) the moment the dominant sub-district changes —
	// the player feels the crossing before reading the name card.

	void Update() {
		if (this.generator == null) return;
		var player = this.generator.Target;
		if (player == null) return;

		float blockSize = AbstractMap4D.BLOCK_SIZE;
		var   pPos      = player.position;

		// Position-granular blend between the two nearest sub-district seeds
		var blend = DistrictBiasProvider.GetSubDistrictBlend(pPos.x / blockSize, pPos.z / blockSize);

		// Atmospheric beat: brief ambient brightening when crossing neighbourhood boundary
		if (blend.nearIndex != this.dominantSubDistrict && this.dominantSubDistrict >= 0) {
			this.beatPhase = 1f;
		}
		this.dominantSubDistrict = blend.nearIndex;
		if (this.beatPhase > 0f) {
			this.beatPhase = Mathf.Max(0f, this.beatPhase - Time.deltaTime / 0.40f);
		}

		float t = blend.t;
		Color targetFog     = Color.Lerp(blend.near.LightColor * 0.18f, blend.far.LightColor * 0.18f, t);
		float targetDensity = Mathf.Lerp(blend.near.FogDensity, blend.far.FogDensity, t);
		Color targetAmbient = Color.Lerp(blend.near.LightColor * 0.06f, blend.far.LightColor * 0.06f, t);

		// Quadratic beat decay — sharp peak, smooth tail
		float beat = this.beatPhase * this.beatPhase;
		targetAmbient += Color.white * (beat * 0.05f);

		float lerp = Time.deltaTime * 1.8f;
		RenderSettings.fog         = true;
		RenderSettings.fogMode     = FogMode.Exponential;
		RenderSettings.fogColor    = Color.Lerp(RenderSettings.fogColor,    targetFog,     lerp);
		RenderSettings.fogDensity  = Mathf.Lerp(RenderSettings.fogDensity,  targetDensity, lerp);
		RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetAmbient, lerp);
	}
}
