using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Places trees, plants, window boxes, and path stones in 4D map chunks.
// Combines: parks (#39), trees (#37), plants (#46), walkways (#41).
public class DecorationPlacer4D : MonoBehaviour, IMapGenerationCallbackReceiver4D {
	private GenerateMap4DNearPlayer generator;
	private MapBehaviour4D mapBehaviour;

	// Shared material cache — avoids per-object material allocation
	private readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

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

		int  style         = DistrictBiasProvider.GetChunkStyle(chunkAddress.x, chunkAddress.z);
		bool isPark        = style == (int)ArchStyle.Park;
		bool isResidential = style == (int)ArchStyle.Victorian
		                  || style == (int)ArchStyle.Ornate
		                  || style == (int)ArchStyle.Mediterranean;

		int  chunkSize = source.ChunkSize;
		var  candidates = new List<(Slot4D slot, PlacementType type)>();
		var  rng        = new System.Random(chunkAddress.GetHashCode() ^ unchecked((int)0x9B3C1F7A));

		for (int dx = 0; dx < chunkSize; dx++) {
			for (int dz = 0; dz < chunkSize; dz++) {
				for (int dw = 0; dw < chunkSize; dw++) {
					// ── Ground-level slots: trees and path stones ──────────────
					var groundPos = new Vector4Int(
						chunkAddress.x * chunkSize + dx, 0,
						chunkAddress.z * chunkSize + dz,
						chunkAddress.w * chunkSize + dw);
					var groundSlot = this.mapBehaviour.Map.GetSlot(groundPos);
					if (groundSlot != null && groundSlot.Collapsed) {
						bool isWalkable = groundSlot.Module?.Name?.ToLower().Contains("walkablearea") == true;
						if (isWalkable) {
							if (isPark && rng.NextDouble() < 0.45) {
								candidates.Add((groundSlot, PlacementType.Tree));
							} else if (isPark && rng.NextDouble() < 0.22) {
								candidates.Add((groundSlot, PlacementType.PathStone));
							} else if (!isPark && rng.NextDouble() < 0.07) {
								candidates.Add((groundSlot, PlacementType.Tree));
							}
						}
					}

					// ── Upper floors: window boxes ─────────────────────────────
					for (int dy = 1; dy <= 2; dy++) {
						var upperPos = new Vector4Int(
							chunkAddress.x * chunkSize + dx, dy,
							chunkAddress.z * chunkSize + dz,
							chunkAddress.w * chunkSize + dw);
						var upperSlot = this.mapBehaviour.Map.GetSlot(upperPos);
						if (upperSlot == null || !upperSlot.Collapsed) continue;
						bool hasWindow = upperSlot.Module?.Name?.ToLower().Contains("window") == true
						             || upperSlot.Module?.Name?.ToLower().Contains("balcony") == true;
						if (hasWindow && isResidential && rng.NextDouble() < 0.30) {
							candidates.Add((upperSlot, PlacementType.WindowBox));
						}
					}
				}
			}
		}

		if (candidates.Count > 0) {
			StartCoroutine(this.PlaceDeferred(candidates, rng.Next()));
		}
	}

	// ── Placement coroutine ───────────────────────────────────────────────

	private enum PlacementType { Tree, PathStone, WindowBox }

	private IEnumerator PlaceDeferred(List<(Slot4D slot, PlacementType type)> items, int seed) {
		float deadline = Time.time + 4f;
		var   rng      = new System.Random(seed);

		foreach (var (slot, type) in items) {
			while (slot.GameObject == null && Time.time < deadline) yield return null;
			if (slot.GameObject == null) continue;

			float bs     = AbstractMap4D.BLOCK_SIZE;
			var   pos    = slot.GameObject.transform.position;
			int   wLayer = slot.Position.w;

			switch (type) {
				case PlacementType.Tree:      this.SpawnTree(pos, bs, wLayer, rng);     break;
				case PlacementType.PathStone: this.SpawnPathStones(pos, bs, rng);       break;
				case PlacementType.WindowBox: this.SpawnWindowBox(slot, bs, rng);       break;
			}
		}
	}

	// ── Trees ─────────────────────────────────────────────────────────────

	private void SpawnTree(Vector3 slotPos, float bs, int wLayer, System.Random rng) {
		int   variant = rng.Next(3);
		float jx = ((float)rng.NextDouble() - 0.5f) * bs * 0.55f;
		float jz = ((float)rng.NextDouble() - 0.5f) * bs * 0.55f;
		float scale  = 0.8f + (float)rng.NextDouble() * 0.45f;

		var root = new GameObject("Tree");
		root.transform.SetParent(this.transform);
		root.transform.position = new Vector3(slotPos.x + jx, slotPos.y, slotPos.z + jz);

		// Subtle W-layer tint keeps trees grounded in the dimension they're in
		Color tint = DimensionColors.ForLayer(wLayer) * 0.12f + Color.white * 0.88f;

		switch (variant) {
			case 0: this.BuildOak(root, scale, tint, rng);    break;
			case 1: this.BuildPine(root, scale, tint, rng);   break;
			case 2: this.BuildWillow(root, scale, tint, rng); break;
		}
	}

	// Exaggerated oak: very wide round canopy, stocky trunk
	private void BuildOak(GameObject root, float scale, Color tint, System.Random rng) {
		float trunkH  = 1.6f * scale;
		float trunkR  = 0.28f * scale;
		float canopyR = 1.55f * scale;

		Color trunkCol  = new Color(0.28f, 0.18f, 0.10f);
		Color canopyCol = new Color(0.20f + (float)rng.NextDouble() * 0.10f,
		                            0.60f + (float)rng.NextDouble() * 0.18f, 0.16f) * tint;

		this.AddCylinder(root, "Trunk",  new Vector3(0, trunkH * 0.5f, 0),
		                 new Vector3(trunkR, trunkH, trunkR), trunkCol);
		this.AddSphere(root, "Canopy",   new Vector3(0, trunkH + canopyR * 0.65f, 0),
		               new Vector3(canopyR * 1.2f, canopyR, canopyR * 1.2f), canopyCol);
		// Second lobe for visual richness
		float ox = ((float)rng.NextDouble() - 0.5f) * canopyR * 0.8f;
		float oz = ((float)rng.NextDouble() - 0.5f) * canopyR * 0.8f;
		this.AddSphere(root, "CanopyB", new Vector3(ox, trunkH + canopyR * 0.38f, oz),
		               Vector3.one * canopyR * 0.68f, canopyCol * 0.88f);
	}

	// Exaggerated pine: very tall, stacked flat disc layers
	private void BuildPine(GameObject root, float scale, Color tint, System.Random rng) {
		float trunkH = 2.8f * scale;
		Color trunkCol = new Color(0.30f, 0.20f, 0.12f);
		Color coneCol  = new Color(0.14f, 0.45f + (float)rng.NextDouble() * 0.12f, 0.20f) * tint;

		this.AddCylinder(root, "Trunk", new Vector3(0, trunkH * 0.5f, 0),
		                 new Vector3(0.18f * scale, trunkH, 0.18f * scale), trunkCol);

		float[] yFracs  = { 0.28f, 0.52f, 0.74f };
		float[] widths  = { 1.35f, 0.85f, 0.42f };
		for (int i = 0; i < 3; i++) {
			this.AddSphere(root, "Ring" + i, new Vector3(0, trunkH * yFracs[i], 0),
			               new Vector3(widths[i] * scale, widths[i] * 0.42f * scale, widths[i] * scale),
			               coneCol * (0.88f + i * 0.05f));
		}
	}

	// Weeping willow: thin tall trunk, drooping foliage arms
	private void BuildWillow(GameObject root, float scale, Color tint, System.Random rng) {
		float trunkH = 2.4f * scale;
		Color trunkCol    = new Color(0.33f, 0.23f, 0.14f);
		Color foliageCol  = new Color(0.26f, 0.54f + (float)rng.NextDouble() * 0.12f, 0.22f) * tint;

		this.AddCylinder(root, "Trunk", new Vector3(0, trunkH * 0.5f, 0),
		                 new Vector3(0.14f * scale, trunkH, 0.14f * scale), trunkCol);

		int arms = 5 + rng.Next(4);
		for (int i = 0; i < arms; i++) {
			float angle = i * (360f / arms) + (float)rng.NextDouble() * 18f;
			float rad   = 0.78f * scale;
			float cy    = trunkH - (0.25f + (float)rng.NextDouble() * 0.40f) * scale;
			this.AddSphere(root, "Arm" + i,
			               new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * rad, cy,
			                           Mathf.Sin(angle * Mathf.Deg2Rad) * rad),
			               new Vector3(0.48f * scale, 0.85f * scale, 0.48f * scale),
			               foliageCol * (0.84f + (float)rng.NextDouble() * 0.16f));
		}
	}

	// ── Path stones ───────────────────────────────────────────────────────

	private void SpawnPathStones(Vector3 slotPos, float bs, System.Random rng) {
		int   count      = 2 + rng.Next(3);
		Color stoneColor = new Color(0.58f, 0.54f, 0.50f);

		for (int i = 0; i < count; i++) {
			float jx = ((float)rng.NextDouble() - 0.5f) * bs * 0.72f;
			float jz = ((float)rng.NextDouble() - 0.5f) * bs * 0.72f;
			float sw = 0.32f + (float)rng.NextDouble() * 0.28f;
			float sh = 0.06f + (float)rng.NextDouble() * 0.06f;
			float sl = sw * (0.65f + (float)rng.NextDouble() * 0.55f);

			var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
			go.name = "PathStone";
			go.transform.SetParent(this.transform);
			go.transform.position = new Vector3(slotPos.x + jx, slotPos.y + sh * 0.5f, slotPos.z + jz);
			go.transform.localScale = new Vector3(sw * bs * 0.18f, sh * bs * 0.18f, sl * bs * 0.18f);
			go.transform.rotation   = Quaternion.Euler(
				((float)rng.NextDouble() - 0.5f) * 5f,
				(float)rng.NextDouble() * 360f,
				((float)rng.NextDouble() - 0.5f) * 5f);
			Object.Destroy(go.GetComponent<Collider>());
			go.GetComponent<Renderer>().sharedMaterial = this.GetMat("PathStone", stoneColor, 0f, 0.08f);
		}
	}

	// ── Window boxes ──────────────────────────────────────────────────────

	private void SpawnWindowBox(Slot4D slot, float bs, System.Random rng) {
		if (slot.GameObject == null) return;

		foreach (int hd in Orientations.HorizontalDirections) {
			var face = slot.Module?.GetFace(hd) as ModulePrototype.HorizontalFaceDetails;
			if (face == null || face.Walkable) continue;

			var rawDir = Orientations.Direction[hd];
			Vector3 wallDir = new Vector3(rawDir.x, rawDir.y, rawDir.z);
			Vector3 attach  = slot.GameObject.transform.position + wallDir * (bs * 0.48f) + Vector3.up * 0.05f;

			// Planter trough
			var trough = GameObject.CreatePrimitive(PrimitiveType.Cube);
			trough.name = "WBox_Trough";
			trough.transform.SetParent(this.transform);
			trough.transform.position   = attach;
			trough.transform.localScale = new Vector3(bs * 0.50f, bs * 0.09f, bs * 0.09f);
			trough.transform.rotation   = Quaternion.LookRotation(wallDir) * Quaternion.Euler(0, 90, 0);
			Object.Destroy(trough.GetComponent<Collider>());
			trough.GetComponent<Renderer>().sharedMaterial = this.GetMat("WBoxTrough", new Color(0.36f, 0.24f, 0.14f), 0f, 0.05f);

			// Three plant puffs inside the trough
			Color pc     = new Color(0.20f, 0.58f + (float)rng.NextDouble() * 0.18f, 0.20f);
			var   pcMat  = this.GetMat("WBoxPlant_" + pc.GetHashCode(), pc, 0f, 0.18f);
			for (int k = -1; k <= 1; k++) {
				float h = 0.10f + (float)rng.NextDouble() * 0.12f;
				var   p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				p.name  = "WBox_Plant";
				p.transform.SetParent(this.transform);
				p.transform.position   = attach + trough.transform.right * (k * bs * 0.15f) + Vector3.up * (bs * 0.06f + h);
				p.transform.localScale = Vector3.one * (0.07f + (float)rng.NextDouble() * 0.06f) * bs;
				Object.Destroy(p.GetComponent<Collider>());
				p.GetComponent<Renderer>().sharedMaterial = pcMat;
			}
			break;
		}
	}

	// ── Primitive helpers ─────────────────────────────────────────────────

	private void AddCylinder(GameObject parent, string name, Vector3 localPos, Vector3 localScale, Color color) {
		var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		go.name = name;
		go.transform.SetParent(parent.transform);
		go.transform.localPosition = localPos;
		go.transform.localScale    = localScale;
		Object.Destroy(go.GetComponent<Collider>());
		go.GetComponent<Renderer>().sharedMaterial = this.GetMat(name + color.r, color, 0f, 0.08f);
	}

	private void AddSphere(GameObject parent, string name, Vector3 localPos, Vector3 localScale, Color color) {
		var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		go.name = name;
		go.transform.SetParent(parent.transform);
		go.transform.localPosition = localPos;
		go.transform.localScale    = localScale;
		Object.Destroy(go.GetComponent<Collider>());
		go.GetComponent<Renderer>().sharedMaterial = this.GetMat("sphere_" + color.GetHashCode(), color, 0f, 0.15f);
	}

	private static Shader standardShader;

	private Material GetMat(string key, Color color, float metallic, float gloss) {
		if (this.matCache.TryGetValue(key, out var m)) return m;
		if (standardShader == null) standardShader = Shader.Find("Standard");
		var mat = new Material(standardShader) { color = color };
		mat.SetFloat("_Metallic",   metallic);
		mat.SetFloat("_Glossiness", gloss);
		this.matCache[key] = mat;
		return mat;
	}
}
