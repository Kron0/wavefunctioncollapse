using System.Collections.Generic;
using UnityEngine;

public class WGatePlacer : MonoBehaviour, IMapGenerationCallbackReceiver4D {
	private GenerateMap4DNearPlayer generator;
	private MapBehaviour4D mapBehaviour;

	void OnEnable() {
		this.generator    = this.GetComponent<GenerateMap4DNearPlayer>();
		this.mapBehaviour = this.GetComponent<MapBehaviour4D>();
		if (this.generator != null) this.generator.RegisterCallback4D(this);
	}

	void OnDisable() {
		if (this.generator != null) this.generator.UnregisterCallback4D(this);
	}

	public void OnGenerateChunk4D(Vector4Int chunkAddress, GenerateMap4DNearPlayer source) {
		var rng = new System.Random(chunkAddress.GetHashCode() ^ 0x7A7E7A7E);
		if (rng.NextDouble() > 0.25f) return;
		if (this.mapBehaviour == null || this.mapBehaviour.Map == null) return;

		int chunkSize = source.ChunkSize;
		var candidates = new List<Slot4D>();

		for (int dx = 0; dx < chunkSize; dx++) {
			for (int y = 1; y < this.mapBehaviour.MapHeight - 1; y++) {
				for (int dz = 0; dz < chunkSize; dz++) {
					for (int dw = 0; dw < chunkSize; dw++) {
						var pos = new Vector4Int(
							chunkAddress.x * chunkSize + dx, y,
							chunkAddress.z * chunkSize + dz,
							chunkAddress.w * chunkSize + dw);
						var slot = this.mapBehaviour.Map.GetSlot(pos);
						if (slot == null || !slot.Collapsed || slot.GameObject == null) continue;
						var proto = slot.Module.Prototype;
						if (proto.Left.Walkable || proto.Right.Walkable
								|| proto.Forward.Walkable || proto.Back.Walkable) {
							candidates.Add(slot);
						}
					}
				}
			}
		}

		int gateCount = Mathf.Clamp(candidates.Count / 8, 1, 2);
		for (int i = 0; i < gateCount && candidates.Count > 0; i++) {
			int idx = rng.Next(candidates.Count);
			this.PlaceGate(candidates[idx]);
			candidates.RemoveAt(idx);
		}
	}

	private void PlaceGate(Slot4D slot) {
		float bs = AbstractMap4D.BLOCK_SIZE;
		// Gate color = accent of the layer where it OPENS (so player knows which layer unlocks it)
		int   gateLayer = slot.Position.w + 1;
		Color accent    = DimensionColors.ForLayer(gateLayer);
		Color emDim     = accent * 1.2f;

		Vector3 worldPos = slot.GameObject.transform.position;

		// Main barrier wall (carries WGateBlocker for collision toggling)
		var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
		wall.name = "WGate_Wall";
		wall.transform.position   = worldPos;
		wall.transform.localScale = new Vector3(bs * 0.92f, bs * 0.85f, bs * 0.08f);
		wall.transform.SetParent(this.transform);

		var wallMat = new Material(Shader.Find("Standard"));
		wallMat.color = accent * 0.12f;
		wallMat.SetColor("_EmissionColor", emDim * 0.4f);
		wallMat.EnableKeyword("_EMISSION");
		wallMat.SetFloat("_Metallic", 0.9f);
		wallMat.SetFloat("_Glossiness", 0.5f);
		wall.GetComponent<Renderer>().material = wallMat;

		var blocker = wall.AddComponent<WGateBlocker>();
		blocker.wGateLayer = gateLayer;

		// Edge frame: 4 thin posts at corners of the wall
		float halfW  = bs * 0.46f;
		float halfH  = bs * 0.425f;
		float postSz = bs * 0.07f;
		Vector3[] corners = {
			new Vector3(-halfW, -halfH, 0),
			new Vector3( halfW, -halfH, 0),
			new Vector3(-halfW,  halfH, 0),
			new Vector3( halfW,  halfH, 0),
		};

		var frameMat = new Material(Shader.Find("Standard"));
		frameMat.color = accent * 0.4f;
		frameMat.SetColor("_EmissionColor", accent * 2f);
		frameMat.EnableKeyword("_EMISSION");
		frameMat.SetFloat("_Metallic", 0.85f);
		frameMat.SetFloat("_Glossiness", 0.7f);

		foreach (var corner in corners) {
			var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
			post.name = "GatePost";
			post.transform.SetParent(wall.transform);
			post.transform.localPosition = corner / bs;  // in wall's local space
			post.transform.localScale    = new Vector3(postSz / (bs * 0.92f),
				postSz / (bs * 0.85f), 1.4f);
			Object.Destroy(post.GetComponent<Collider>());
			post.GetComponent<Renderer>().material = frameMat;
		}

		// Horizontal top/bottom bars
		float[] barYs = { -halfH, halfH };
		foreach (float by in barYs) {
			var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
			bar.name = "GateBar";
			bar.transform.SetParent(wall.transform);
			bar.transform.localPosition = new Vector3(0, by / (bs * 0.85f), 0);
			bar.transform.localScale    = new Vector3(1.0f, postSz / (bs * 0.85f), 1.4f);
			Object.Destroy(bar.GetComponent<Collider>());
			bar.GetComponent<Renderer>().material = frameMat;
		}

		// Persistent glow marker — always visible (no WGateBlocker), no collider
		var marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
		marker.name = "WGate_Marker";
		marker.transform.position   = worldPos;
		marker.transform.localScale = new Vector3(bs * 0.35f, bs * 0.35f, 1f);
		marker.transform.SetParent(this.transform);
		Object.Destroy(marker.GetComponent<MeshCollider>());

		var markerMat = new Material(Shader.Find("Standard"));
		markerMat.SetFloat("_Mode", 3f);
		markerMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		markerMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		markerMat.SetInt("_ZWrite", 0);
		markerMat.EnableKeyword("_ALPHABLEND_ON");
		markerMat.renderQueue = 3000;
		markerMat.color = new Color(accent.r, accent.g, accent.b, 0.22f);
		markerMat.SetColor("_EmissionColor", accent * 0.8f);
		markerMat.EnableKeyword("_EMISSION");
		marker.GetComponent<Renderer>().material = markerMat;
	}
}
