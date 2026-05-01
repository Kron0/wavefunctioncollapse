using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Populates the city with life: benches, street lamps, welcome mats, hanging signs, planters.
// Dreamy/cartoonish scale — slightly oversized, saturated colours.
public class HumanDetailPlacer : MonoBehaviour, IMapGenerationCallbackReceiver4D {
	private GenerateMap4DNearPlayer generator;
	private MapBehaviour4D mapBehaviour;

	private readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

	private static readonly Color[] BenchColors = {
		new Color(0.75f, 0.42f, 0.22f),
		new Color(0.30f, 0.48f, 0.72f),
		new Color(0.65f, 0.30f, 0.55f),
		new Color(0.28f, 0.58f, 0.40f),
	};

	private static readonly Color[] MatColors = {
		new Color(0.80f, 0.20f, 0.20f),
		new Color(0.20f, 0.35f, 0.75f),
		new Color(0.65f, 0.55f, 0.18f),
		new Color(0.22f, 0.55f, 0.42f),
		new Color(0.55f, 0.22f, 0.60f),
	};

	void OnEnable() {
		this.generator    = this.GetComponent<GenerateMap4DNearPlayer>();
		this.mapBehaviour = this.GetComponent<MapBehaviour4D>();
		if (this.generator != null) this.generator.RegisterCallback4D(this);
	}

	void OnDisable() {
		if (this.generator != null) this.generator.UnregisterCallback4D(this);
	}

	public void OnGenerateChunk4D(Vector4Int chunkAddress, GenerateMap4DNearPlayer source) {
		if (this.mapBehaviour?.Map == null) return;

		int chunkSize = source.ChunkSize;
		var rng = new System.Random(chunkAddress.GetHashCode() ^ unchecked((int)0xDEADC0DE));

		var groundItems = new List<(Slot4D slot, DetailType type)>();
		var upperItems  = new List<(Slot4D slot, DetailType type)>();

		for (int dx = 0; dx < chunkSize; dx++) {
			for (int dz = 0; dz < chunkSize; dz++) {
				for (int dw = 0; dw < chunkSize; dw++) {
					// ── Ground level ───────────────────────────────────────
					var gPos = new Vector4Int(
						chunkAddress.x * chunkSize + dx, 0,
						chunkAddress.z * chunkSize + dz,
						chunkAddress.w * chunkSize + dw);
					var gSlot = this.mapBehaviour.Map.GetSlot(gPos);
					if (gSlot != null && gSlot.Collapsed) {
						bool walkable = gSlot.Module?.Name?.ToLower().Contains("walkablearea") == true;
						if (walkable) {
							double r = rng.NextDouble();
							if      (r < 0.06f) groundItems.Add((gSlot, DetailType.Bench));
							else if (r < 0.13f) groundItems.Add((gSlot, DetailType.LampPost));
							else if (r < 0.20f) groundItems.Add((gSlot, DetailType.Planter));
							else if (r < 0.28f) groundItems.Add((gSlot, DetailType.WelcomeMat));
						}
					}

					// ── Floor 1-2: hanging signs ───────────────────────────
					for (int dy = 1; dy <= 2; dy++) {
						var uPos = new Vector4Int(
							chunkAddress.x * chunkSize + dx, dy,
							chunkAddress.z * chunkSize + dz,
							chunkAddress.w * chunkSize + dw);
						var uSlot = this.mapBehaviour.Map.GetSlot(uPos);
						if (uSlot == null || !uSlot.Collapsed) continue;
						bool hasExteriorWall = uSlot.Module?.Name?.ToLower().Contains("window") == true
						                    || uSlot.Module?.Name?.ToLower().Contains("wall") == true;
						if (hasExteriorWall && rng.NextDouble() < 0.10f) {
							upperItems.Add((uSlot, DetailType.HangingSign));
						}
					}
				}
			}
		}

		var all = new List<(Slot4D slot, DetailType type)>(groundItems);
		all.AddRange(upperItems);

		if (all.Count > 0) {
			StartCoroutine(this.PlaceDeferred(all, rng.Next()));
		}
	}

	// ── Placement coroutine ───────────────────────────────────────────────

	private enum DetailType { Bench, LampPost, WelcomeMat, Planter, HangingSign }

	private IEnumerator PlaceDeferred(List<(Slot4D slot, DetailType type)> items, int seed) {
		float deadline = Time.time + 4f;
		var rng = new System.Random(seed);

		foreach (var (slot, type) in items) {
			while (slot.GameObject == null && Time.time < deadline) yield return null;
			if (slot.GameObject == null) continue;

			float bs  = AbstractMap4D.BLOCK_SIZE;
			var   pos = slot.GameObject.transform.position;

			switch (type) {
				case DetailType.Bench:       this.SpawnBench(pos, bs, rng);       break;
				case DetailType.LampPost:    this.SpawnLampPost(pos, bs, rng);    break;
				case DetailType.WelcomeMat:  this.SpawnWelcomeMat(pos, bs, rng);  break;
				case DetailType.Planter:     this.SpawnPlanter(pos, bs, rng);     break;
				case DetailType.HangingSign: this.SpawnHangingSign(slot, bs, rng); break;
			}
		}
	}

	// ── Bench ─────────────────────────────────────────────────────────────

	private void SpawnBench(Vector3 slotPos, float bs, System.Random rng) {
		float jx = ((float)rng.NextDouble() - 0.5f) * bs * 0.50f;
		float jz = ((float)rng.NextDouble() - 0.5f) * bs * 0.50f;
		float rot = (float)rng.NextDouble() * 360f;

		Color col  = BenchColors[rng.Next(BenchColors.Length)];
		Color dark = col * 0.55f;

		var root = new GameObject("Bench");
		root.transform.SetParent(this.transform);
		root.transform.position = new Vector3(slotPos.x + jx, slotPos.y, slotPos.z + jz);
		root.transform.rotation = Quaternion.Euler(0f, rot, 0f);

		float seatH = 0.35f * bs;
		float seatL = 0.80f * bs;
		float seatW = 0.28f * bs;
		float legH  = seatH;
		float legSz = 0.06f * bs;

		// Seat plank
		this.AddPrim(root, PrimitiveType.Cube, "Seat",
			new Vector3(0f, seatH + 0.02f * bs, 0f),
			new Vector3(seatL, 0.06f * bs, seatW), col);

		// Backrest
		this.AddPrim(root, PrimitiveType.Cube, "Back",
			new Vector3(0f, seatH + 0.22f * bs, seatW * -0.42f),
			new Vector3(seatL, 0.22f * bs, 0.055f * bs), col);

		// Four legs
		for (int sx = -1; sx <= 1; sx += 2) {
			for (int sz = -1; sz <= 1; sz += 2) {
				this.AddPrim(root, PrimitiveType.Cylinder, "Leg",
					new Vector3(sx * seatL * 0.36f, legH * 0.5f, sz * seatW * 0.38f),
					new Vector3(legSz, legH, legSz), dark);
			}
		}
	}

	// ── Lamp post ─────────────────────────────────────────────────────────

	private void SpawnLampPost(Vector3 slotPos, float bs, System.Random rng) {
		float jx = ((float)rng.NextDouble() - 0.5f) * bs * 0.55f;
		float jz = ((float)rng.NextDouble() - 0.5f) * bs * 0.55f;

		var root = new GameObject("LampPost");
		root.transform.SetParent(this.transform);
		root.transform.position = new Vector3(slotPos.x + jx, slotPos.y, slotPos.z + jz);

		Color poleCol    = new Color(0.22f, 0.22f, 0.24f);
		Color lanternCol = new Color(0.98f, 0.95f, 0.65f);

		float poleH   = 2.0f * bs;
		float poleR   = 0.045f * bs;
		float headOff = 0.30f * bs;

		// Pole
		this.AddPrim(root, PrimitiveType.Cylinder, "Pole",
			new Vector3(0f, poleH * 0.5f, 0f),
			new Vector3(poleR, poleH, poleR), poleCol);

		// Arm
		this.AddPrim(root, PrimitiveType.Cylinder, "Arm",
			new Vector3(headOff * 0.5f, poleH + 0.03f * bs, 0f),
			new Vector3(headOff, poleR, poleR),
			poleCol);

		// Lantern sphere
		this.AddPrim(root, PrimitiveType.Sphere, "Lantern",
			new Vector3(headOff, poleH - 0.10f * bs, 0f),
			Vector3.one * 0.16f * bs, lanternCol);

		// Actual point light
		var lightGO = new GameObject("LampLight");
		lightGO.transform.SetParent(root.transform);
		lightGO.transform.localPosition = new Vector3(headOff, poleH - 0.10f * bs, 0f);
		var light = lightGO.AddComponent<Light>();
		light.type      = LightType.Point;
		light.color     = new Color(1.00f, 0.92f, 0.65f);
		light.intensity = 0.8f;
		light.range     = bs * 2.0f;
	}

	// ── Welcome mat ───────────────────────────────────────────────────────

	private void SpawnWelcomeMat(Vector3 slotPos, float bs, System.Random rng) {
		Color col = MatColors[rng.Next(MatColors.Length)];
		float jx  = ((float)rng.NextDouble() - 0.5f) * bs * 0.30f;
		float jz  = ((float)rng.NextDouble() - 0.5f) * bs * 0.30f;
		float rot = Mathf.Round((float)rng.NextDouble() * 3f) * 90f;

		var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
		go.name = "WelcomeMat";
		go.transform.SetParent(this.transform);
		go.transform.position   = new Vector3(slotPos.x + jx, slotPos.y + 0.025f * bs, slotPos.z + jz);
		go.transform.rotation   = Quaternion.Euler(0f, rot, 0f);
		go.transform.localScale = new Vector3(bs * 0.35f, bs * 0.022f, bs * 0.22f);
		Object.Destroy(go.GetComponent<Collider>());

		var mat = this.GetMat("mat_" + col.r.ToString("F2"), col, 0f, 0.08f);
		go.GetComponent<Renderer>().sharedMaterial = mat;

		// Thin border stripe — slightly darker
		var border = GameObject.CreatePrimitive(PrimitiveType.Cube);
		border.name = "MatBorder";
		border.transform.SetParent(go.transform);
		border.transform.localPosition = new Vector3(0f, 0.8f, 0f);
		border.transform.localScale    = new Vector3(1.08f, 0.08f, 1.08f);
		Object.Destroy(border.GetComponent<Collider>());
		Color borderCol = col * 0.6f;
		border.GetComponent<Renderer>().sharedMaterial = this.GetMat("border_" + borderCol.r.ToString("F2"), borderCol, 0f, 0.05f);
	}

	// ── Street planter ────────────────────────────────────────────────────

	private void SpawnPlanter(Vector3 slotPos, float bs, System.Random rng) {
		float jx = ((float)rng.NextDouble() - 0.5f) * bs * 0.50f;
		float jz = ((float)rng.NextDouble() - 0.5f) * bs * 0.50f;

		Color potCol  = new Color(0.65f + (float)rng.NextDouble() * 0.20f,
		                          0.45f + (float)rng.NextDouble() * 0.15f,
		                          0.28f + (float)rng.NextDouble() * 0.10f);
		Color leafCol = new Color(0.22f, 0.58f + (float)rng.NextDouble() * 0.18f, 0.28f);

		var root = new GameObject("Planter");
		root.transform.SetParent(this.transform);
		root.transform.position = new Vector3(slotPos.x + jx, slotPos.y, slotPos.z + jz);

		float potSz = 0.28f * bs;
		float potH  = 0.22f * bs;

		// Pot (tapered cylinder approximated by cube)
		this.AddPrim(root, PrimitiveType.Cube, "Pot",
			new Vector3(0f, potH * 0.5f, 0f),
			new Vector3(potSz, potH, potSz), potCol);

		// Foliage ball
		float leafR = 0.24f * bs + (float)rng.NextDouble() * 0.10f * bs;
		this.AddPrim(root, PrimitiveType.Sphere, "Leaves",
			new Vector3(0f, potH + leafR * 0.7f, 0f),
			Vector3.one * leafR, leafCol);
	}

	// ── Hanging sign ──────────────────────────────────────────────────────

	private void SpawnHangingSign(Slot4D slot, float bs, System.Random rng) {
		if (slot.GameObject == null) return;

		foreach (int hd in Orientations.HorizontalDirections) {
			var face = slot.Module?.GetFace(hd) as ModulePrototype.HorizontalFaceDetails;
			if (face == null || face.Walkable) continue;

			var rawDir = Orientations.Direction[hd];
			Vector3 wallDir = new Vector3(rawDir.x, rawDir.y, rawDir.z);
			Vector3 attach  = slot.GameObject.transform.position
				+ wallDir * (bs * 0.42f) + Vector3.up * (bs * 0.08f);

			// Bracket arm (thin horizontal cylinder pointing out from wall)
			var bracket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			bracket.name = "SignBracket";
			bracket.transform.SetParent(this.transform);
			bracket.transform.position = attach + wallDir * (bs * 0.12f);
			bracket.transform.localScale = new Vector3(0.04f * bs, 0.22f * bs, 0.04f * bs);
			bracket.transform.rotation = Quaternion.LookRotation(Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
			Object.Destroy(bracket.GetComponent<Collider>());
			bracket.GetComponent<Renderer>().sharedMaterial = this.GetMat("SignBracket", new Color(0.20f, 0.20f, 0.22f), 0.6f, 0.5f);

			// Sign board (flat quad hanging from bracket end)
			Color signBg  = new Color(
				0.80f + (float)rng.NextDouble() * 0.15f,
				0.72f + (float)rng.NextDouble() * 0.18f,
				0.40f + (float)rng.NextDouble() * 0.20f);
			var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
			sign.name = "SignBoard";
			sign.transform.SetParent(this.transform);
			sign.transform.position   = attach + wallDir * (bs * 0.24f) - Vector3.up * (bs * 0.08f);
			sign.transform.localScale = new Vector3(bs * 0.30f, bs * 0.13f, bs * 0.03f);
			sign.transform.rotation   = Quaternion.LookRotation(wallDir);
			Object.Destroy(sign.GetComponent<Collider>());

			var signMat = new Material(Shader.Find("Standard"));
			signMat.color = signBg;
			signMat.SetFloat("_Metallic", 0f);
			signMat.SetFloat("_Glossiness", 0.12f);
			sign.GetComponent<Renderer>().material = signMat;
			break;
		}
	}

	// ── Primitive helpers ─────────────────────────────────────────────────

	private void AddPrim(GameObject parent, PrimitiveType shape, string name,
		Vector3 localPos, Vector3 localScale, Color color) {

		var go = GameObject.CreatePrimitive(shape);
		go.name = name;
		go.transform.SetParent(parent.transform);
		go.transform.localPosition = localPos;
		go.transform.localScale    = localScale;
		Object.Destroy(go.GetComponent<Collider>());
		go.GetComponent<Renderer>().sharedMaterial = this.GetMat(name + color.GetHashCode(), color, 0f, 0.12f);
	}

	private Material GetMat(string key, Color color, float metallic, float gloss) {
		if (this.matCache.TryGetValue(key, out var m)) return m;
		var mat = new Material(Shader.Find("Standard")) { color = color };
		mat.SetFloat("_Metallic",   metallic);
		mat.SetFloat("_Glossiness", gloss);
		this.matCache[key] = mat;
		return mat;
	}
}
