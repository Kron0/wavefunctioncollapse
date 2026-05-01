using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DistrictStyler : MonoBehaviour, IMapGenerationCallbackReceiver4D {
	public enum TextureSource { Shader, Procedural, Downloaded }
	// ArchStyle is defined in DistrictStyle.cs as a top-level enum

	[SerializeField] public TextureSource Mode = TextureSource.Procedural;
	[SerializeField] public int DistrictSeed = 42;
	[SerializeField] public int DistrictCount = 6;
	[Range(0f, 0.5f)]
	[SerializeField] public float BoundaryWidth = 0.35f; // fraction of inter-seed distance at which boundary zone starts
	[Range(0f, 0.25f)]
	[SerializeField] public float MixedDistrictChance = 0.20f; // fraction of seeds that blend two styles

	private GenerateMap4DNearPlayer generator;
	private MapBehaviour4D mapBehaviour;

	private struct SeedInfo {
		public Vector2 position;
		public ArchStyle primary;
		public ArchStyle secondary;
		public bool isMixed;
	}
	private SeedInfo[] seeds;

	private readonly Dictionary<ArchStyle, Texture2D> proceduralTextures = new Dictionary<ArchStyle, Texture2D>();
	private readonly Dictionary<string, Texture2D> downloadedTextures = new Dictionary<string, Texture2D>();
	private readonly Dictionary<ArchStyle, string> downloadedUrls = new Dictionary<ArchStyle, string>();
	private bool urlsFetched = false;

	private static readonly int SideTexID   = Shader.PropertyToID("_SideTex");
	private static readonly int TopTexID    = Shader.PropertyToID("_TopTex");
	private static readonly int BottomTexID = Shader.PropertyToID("_BottomTex");
	private static readonly int TexScaleID  = Shader.PropertyToID("_TexScale");
	private static readonly int BrickStrID  = Shader.PropertyToID("_BrickStrength");

	private static readonly Dictionary<ArchStyle, string> polyHavenCategories = new Dictionary<ArchStyle, string> {
		{ ArchStyle.Victorian,     "brick" },
		{ ArchStyle.Modernist,     "concrete" },
		{ ArchStyle.Mediterranean, "plaster" },
		{ ArchStyle.Industrial,    "metal" },
		{ ArchStyle.Brutalist,     "concrete" },
		{ ArchStyle.Ornate,        "brick" },
		{ ArchStyle.Park,          "ground" },
		{ ArchStyle.Commercial,    "tiles" },
	};

	void OnEnable() {
		this.generator    = this.GetComponent<GenerateMap4DNearPlayer>();
		this.mapBehaviour = this.GetComponent<MapBehaviour4D>();
		if (this.generator != null) this.generator.RegisterCallback4D(this);
		this.BuildSeeds();
		if (this.Mode != TextureSource.Shader) {
			this.GenerateAllProceduralTextures();
		}
		if (this.Mode == TextureSource.Downloaded) {
			StartCoroutine(this.FetchTextureUrls());
		} else {
			this.urlsFetched = true;
		}
	}

	void OnDisable() {
		if (this.generator != null) this.generator.UnregisterCallback4D(this);
	}

	// ── Seed generation ───────────────────────────────────────────────────

	private void BuildSeeds() {
		var rng = new System.Random(this.DistrictSeed);
		int count = Mathf.Clamp(this.DistrictCount, 2, 24);
		var styles = (ArchStyle[])Enum.GetValues(typeof(ArchStyle));

		this.seeds = new SeedInfo[count];
		var providerSeeds = new DistrictBiasProvider.SeedData[count];

		for (int i = 0; i < count; i++) {
			var primary   = styles[rng.Next(styles.Length)];
			var secondary = styles[rng.Next(styles.Length)];
			bool mixed = rng.NextDouble() < this.MixedDistrictChance;
			float px = (float)(rng.NextDouble() * 200f - 100f);
			float pz = (float)(rng.NextDouble() * 200f - 100f);

			this.seeds[i] = new SeedInfo {
				position  = new Vector2(px, pz),
				primary   = primary,
				secondary = mixed ? secondary : primary,
				isMixed   = mixed,
			};

			providerSeeds[i] = new DistrictBiasProvider.SeedData {
				x     = px,
				z     = pz,
				style = (int)primary,
			};
		}

		// Publish to static provider so the background generation thread can read district styles
		DistrictBiasProvider.SetSeeds(providerSeeds);
	}

	// ── Per-slot style resolution ─────────────────────────────────────────

	private struct SlotStyle {
		public ArchStyle style;
		public float texScale;
		public bool applyTexture;
	}

	private SlotStyle GetSlotStyle(Vector4Int slotPos) {
		float px = slotPos.x;
		float pz = slotPos.z;

		// Find nearest and second-nearest Voronoi seeds
		float d1 = float.MaxValue, d2 = float.MaxValue;
		int idx1 = 0, idx2 = 0;
		for (int i = 0; i < this.seeds.Length; i++) {
			float dx = px - this.seeds[i].position.x;
			float dz = pz - this.seeds[i].position.y;
			float d  = dx * dx + dz * dz;
			if (d < d1) { d2 = d1; idx2 = idx1; d1 = d; idx1 = i; }
			else if (d < d2) { d2 = d; idx2 = i; }
		}

		// Boundary proximity: 0 = centre of district, 1 = on the boundary line
		float r1 = Mathf.Sqrt(d1);
		float r2 = Mathf.Sqrt(d2);
		float boundaryProximity = r1 / (r1 + r2); // 0.5 exactly on the boundary

		// Neutral zone: slots near the boundary line get no texture override — lets
		// the underlying shader patterns show, which naturally reads as a street edge
		// or civic plaza separating the two districts.
		float threshold = 0.5f - this.BoundaryWidth * 0.5f;
		if (boundaryProximity > threshold) {
			return new SlotStyle { style = this.seeds[idx1].primary, texScale = 1f, applyTexture = false };
		}

		// Resolve style for this slot
		ArchStyle style;
		if (this.seeds[idx1].isMixed) {
			// Mixed district: Perlin noise anchored to seed position so the blend is spatially
			// coherent (patches of each style) rather than random per-slot checkerboard
			float n = Mathf.PerlinNoise(
				px * 0.22f + this.seeds[idx1].position.x * 0.1f,
				pz * 0.22f + this.seeds[idx1].position.y * 0.1f);
			style = n > 0.52f ? this.seeds[idx1].primary : this.seeds[idx1].secondary;
		} else {
			style = this.seeds[idx1].primary;
		}

		// Per-slot texture scale variation (±15%) — breaks up tiling repetition
		float scaleNoise = Mathf.PerlinNoise(px * 0.18f + 7.31f, pz * 0.18f + 3.17f);
		float texScale   = GetBaseTexScale(style) * (0.87f + scaleNoise * 0.26f);

		// Small chance to skip texture even inside a district — adds organic gaps,
		// reads like a repaired section or different material in an older building
		var slotRng = new System.Random(slotPos.GetHashCode() ^ 0x3A7F1C2B);
		bool applyTexture = slotRng.NextDouble() > 0.08;

		return new SlotStyle { style = style, texScale = texScale, applyTexture = applyTexture };
	}

	// ── Callback ──────────────────────────────────────────────────────────

	public void OnGenerateChunk4D(Vector4Int chunkAddress, GenerateMap4DNearPlayer source) {
		if (this.Mode == TextureSource.Shader) return;
		if (this.mapBehaviour?.Map == null) return;
		if (!this.urlsFetched) return;

		int chunkSize = source.ChunkSize;
		var slots = new List<Slot4D>();

		for (int dx = 0; dx < chunkSize; dx++) {
			for (int dy = 0; dy < this.mapBehaviour.MapHeight; dy++) {
				for (int dz = 0; dz < chunkSize; dz++) {
					for (int dw = 0; dw < chunkSize; dw++) {
						var pos = new Vector4Int(
							chunkAddress.x * chunkSize + dx, dy,
							chunkAddress.z * chunkSize + dz,
							chunkAddress.w * chunkSize + dw);
						var slot = this.mapBehaviour.Map.GetSlot(pos);
						if (slot != null && slot.Collapsed) slots.Add(slot);
					}
				}
			}
		}

		if (slots.Count > 0) {
			StartCoroutine(this.ApplyStyleDeferred(slots));
		}
	}

	private IEnumerator ApplyStyleDeferred(List<Slot4D> slots) {
		float deadline = Time.time + 4f;
		var block = new MaterialPropertyBlock();

		foreach (var slot in slots) {
			while (slot.GameObject == null && Time.time < deadline) yield return null;
			if (slot.GameObject == null) continue;

			var info = this.GetSlotStyle(slot.Position);
			if (!info.applyTexture) continue;

			Texture2D tex = this.ResolveTexture(info.style);
			if (tex == null) continue;

			foreach (var r in slot.GameObject.GetComponentsInChildren<Renderer>()) {
				r.GetPropertyBlock(block);
				block.SetTexture(SideTexID,   tex);
				block.SetTexture(TopTexID,    tex);
				block.SetTexture(BottomTexID, tex);
				block.SetFloat(BrickStrID,  0f);
				block.SetFloat(TexScaleID,  info.texScale);
				r.SetPropertyBlock(block);
			}
		}
	}

	private Texture2D ResolveTexture(ArchStyle style) {
		if (this.Mode == TextureSource.Downloaded && this.downloadedUrls.TryGetValue(style, out var url)) {
			if (this.downloadedTextures.TryGetValue(url, out var downloaded)) return downloaded;
		}
		this.proceduralTextures.TryGetValue(style, out var procedural);
		return procedural;
	}

	// ── Texture generation ────────────────────────────────────────────────

	private static float GetBaseTexScale(ArchStyle style) {
		switch (style) {
			case ArchStyle.Victorian:      return 0.50f;
			case ArchStyle.Modernist:      return 1.50f;
			case ArchStyle.Mediterranean:  return 0.80f;
			case ArchStyle.Industrial:     return 1.20f;
			case ArchStyle.Brutalist:      return 2.00f;
			case ArchStyle.Ornate:         return 0.40f;
			case ArchStyle.Park:           return 1.00f;
			case ArchStyle.Commercial:     return 1.20f;
			default:                       return 1.00f;
		}
	}

	private void GenerateAllProceduralTextures() {
		const int W = 256, H = 256;
		this.proceduralTextures[ArchStyle.Victorian]     = ProceduralTexGen.Brick(W, H, new Color(0.70f, 0.38f, 0.28f), new Color(0.58f, 0.52f, 0.48f));
		this.proceduralTextures[ArchStyle.Modernist]     = ProceduralTexGen.Concrete(W, H, new Color(0.72f, 0.72f, 0.70f));
		this.proceduralTextures[ArchStyle.Mediterranean] = ProceduralTexGen.Plaster(W, H, new Color(0.90f, 0.82f, 0.65f));
		this.proceduralTextures[ArchStyle.Industrial]    = ProceduralTexGen.Metal(W, H, new Color(0.55f, 0.50f, 0.48f));
		this.proceduralTextures[ArchStyle.Brutalist]     = ProceduralTexGen.Concrete(W, H, new Color(0.58f, 0.55f, 0.52f));
		this.proceduralTextures[ArchStyle.Ornate]        = ProceduralTexGen.Brick(W, H, new Color(0.88f, 0.72f, 0.50f), new Color(0.72f, 0.65f, 0.58f));
		this.proceduralTextures[ArchStyle.Park]          = ProceduralTexGen.Plaster(W, H, new Color(0.52f, 0.68f, 0.42f));  // mossy green
		this.proceduralTextures[ArchStyle.Commercial]    = ProceduralTexGen.Concrete(W, H, new Color(0.78f, 0.76f, 0.80f)); // pale lilac-grey
	}

	// ── Poly Haven API ────────────────────────────────────────────────────

	private IEnumerator FetchTextureUrls() {
		var fetched = new Dictionary<string, string>(); // category → url
		foreach (ArchStyle style in Enum.GetValues(typeof(ArchStyle))) {
			if (!polyHavenCategories.TryGetValue(style, out var category)) continue;
			if (fetched.TryGetValue(category, out var cachedUrl)) {
				this.downloadedUrls[style] = cachedUrl;
				continue;
			}

			string apiUrl = "https://api.polyhaven.com/assets?type=textures&categories=" + category + "&limit=3";
			var req = UnityWebRequest.Get(apiUrl);
			yield return req.SendWebRequest();

			if (!req.isNetworkError && !req.isHttpError) {
				string text = req.downloadHandler.text;
				int q1 = text.IndexOf('"');
				if (q1 >= 0) {
					int q2 = text.IndexOf('"', q1 + 1);
					if (q2 > q1) {
						string slug = text.Substring(q1 + 1, q2 - q1 - 1);
						if (!string.IsNullOrEmpty(slug) && !slug.StartsWith("{")) {
							string url = "https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/"
								+ slug + "/" + slug + "_diff_1k.jpg";
							this.downloadedUrls[style] = url;
							fetched[category] = url;
						}
					}
				}
			}
			req.Dispose();
		}

		// Pre-download all referenced textures so they're ready for first use
		foreach (var kv in this.downloadedUrls) {
			string url = kv.Value;
			if (!this.downloadedTextures.ContainsKey(url)) {
				yield return StartCoroutine(this.DownloadTexture(url, tex => {
					if (tex != null) this.downloadedTextures[url] = tex;
				}));
			}
		}

		this.urlsFetched = true;
	}

	private IEnumerator DownloadTexture(string url, Action<Texture2D> callback) {
		var req = UnityWebRequestTexture.GetTexture(url);
		yield return req.SendWebRequest();
		callback(!req.isNetworkError && !req.isHttpError ? DownloadHandlerTexture.GetContent(req) : null);
		req.Dispose();
	}
}
