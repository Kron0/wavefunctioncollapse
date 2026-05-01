using System.Collections.Generic;
using UnityEngine;

public class LandmarkPlacer : MonoBehaviour, IMapGenerationCallbackReceiver4D {
	private GenerateMap4DNearPlayer generator;
	private MapBehaviour4D mapBehaviour;

	public static readonly List<Vector3> LandmarkWorldPositions = new List<Vector3>();
	public static int LandmarkCount => LandmarkWorldPositions.Count;

	void OnEnable() {
		LandmarkWorldPositions.Clear();
		this.generator    = this.GetComponent<GenerateMap4DNearPlayer>();
		this.mapBehaviour = this.GetComponent<MapBehaviour4D>();
		if (this.generator != null) this.generator.RegisterCallback4D(this);
	}

	void OnDisable() {
		if (this.generator != null) this.generator.UnregisterCallback4D(this);
	}

	public void OnGenerateChunk4D(Vector4Int chunkAddress, GenerateMap4DNearPlayer source) {
		var rng = new System.Random(chunkAddress.GetHashCode() ^ 0x5EEDFACE);
		if (rng.NextDouble() > 0.125f) return;
		if (this.mapBehaviour == null || this.mapBehaviour.Map == null) return;

		int   chunkSize = source.ChunkSize;
		int   wBase     = chunkAddress.w * chunkSize;
		int   wLayer    = wBase;
		Slot4D topSlot  = null;
		int   maxY      = -1;

		for (int dx = 0; dx < chunkSize; dx++) {
			for (int dz = 0; dz < chunkSize; dz++) {
				for (int y = this.mapBehaviour.MapHeight - 1; y >= 0; y--) {
					var slot = this.mapBehaviour.Map.GetSlot(
						new Vector4Int(chunkAddress.x * chunkSize + dx, y,
							chunkAddress.z * chunkSize + dz, wBase));
					if (slot != null && slot.Collapsed && slot.GameObject != null && y > maxY) {
						maxY    = y;
						topSlot = slot;
					}
				}
			}
		}

		if (topSlot == null) return;

		float scale = 0.5f + (float)rng.NextDouble() * 1.5f;
		Vector3 beaconBase = topSlot.GameObject.transform.position + Vector3.up * AbstractMap4D.BLOCK_SIZE;
		this.SpawnBeacon(beaconBase, wLayer, scale);
		LandmarkWorldPositions.Add(beaconBase + Vector3.up * 4.5f * scale);
	}

	private void SpawnBeacon(Vector3 basePos, int wLayer, float scale = 1f) {
		Color accent  = DimensionColors.ForLayer(wLayer);
		Color emDim   = accent * 1.8f;
		Color emBright = accent * 5f;

		var beacon = new GameObject("Landmark_Beacon");
		beacon.transform.position = basePos;
		beacon.transform.localScale = Vector3.one * scale;
		beacon.transform.SetParent(this.transform);

		var sharedMat = new Material(Shader.Find("Standard"));
		sharedMat.color = accent * 0.15f;
		sharedMat.SetColor("_EmissionColor", emDim);
		sharedMat.EnableKeyword("_EMISSION");
		sharedMat.SetFloat("_Metallic", 0.8f);
		sharedMat.SetFloat("_Glossiness", 0.6f);

		// Base ring — thin flat disk at ground
		this.MakePart(beacon, PrimitiveType.Cylinder, "Base",
			new Vector3(0f, 0.05f, 0f), new Vector3(1.0f, 0.04f, 1.0f), sharedMat);

		// Slim column
		this.MakePart(beacon, PrimitiveType.Cylinder, "Column",
			new Vector3(0f, 2.0f, 0f), new Vector3(0.06f, 2.0f, 0.06f), sharedMat);

		// Decorative ring at 1/3 height
		this.MakePart(beacon, PrimitiveType.Cylinder, "RingLow",
			new Vector3(0f, 1.2f, 0f), new Vector3(0.55f, 0.035f, 0.55f), sharedMat);

		// Decorative ring at 2/3 height
		this.MakePart(beacon, PrimitiveType.Cylinder, "RingHigh",
			new Vector3(0f, 2.8f, 0f), new Vector3(0.40f, 0.035f, 0.40f), sharedMat);

		// Rotating top pivot (crosshairs + orb)
		var topPivot = new GameObject("TopPivot");
		topPivot.transform.SetParent(beacon.transform);
		topPivot.transform.localPosition = new Vector3(0f, 4.0f, 0f);

		// Cross-bar H
		this.MakePart(topPivot, PrimitiveType.Cube, "CrossH",
			new Vector3(0f, 0f, 0f), new Vector3(0.70f, 0.04f, 0.04f), sharedMat);

		// Cross-bar depth
		this.MakePart(topPivot, PrimitiveType.Cube, "CrossD",
			new Vector3(0f, 0f, 0f), new Vector3(0.04f, 0.04f, 0.70f), sharedMat);

		// Orb
		var orbMat = new Material(Shader.Find("Standard"));
		orbMat.color = accent;
		orbMat.SetColor("_EmissionColor", emBright);
		orbMat.EnableKeyword("_EMISSION");
		orbMat.SetFloat("_Metallic", 0.0f);
		orbMat.SetFloat("_Glossiness", 1.0f);

		var orbGO = this.MakePart(topPivot, PrimitiveType.Sphere, "Orb",
			new Vector3(0f, 0.25f, 0f), new Vector3(0.32f, 0.32f, 0.32f), orbMat);

		// Point light
		var lightGO = new GameObject("Beacon_Light");
		lightGO.transform.SetParent(beacon.transform);
		lightGO.transform.localPosition = new Vector3(0f, 4.2f, 0f);
		var light = lightGO.AddComponent<Light>();
		light.type      = LightType.Point;
		light.color     = accent;
		light.intensity = 3f;
		light.range     = 40f;

		// Animation driver
		var anim = beacon.AddComponent<LandmarkBeacon>();
		anim.Init(topPivot.transform, light, orbGO.GetComponent<Renderer>(), accent);
	}

	// Creates a primitive child; returns it. Collider is removed.
	private GameObject MakePart(GameObject parent, PrimitiveType shape, string name,
		Vector3 localPos, Vector3 localScale, Material mat) {

		var go = GameObject.CreatePrimitive(shape);
		go.name = name;
		go.transform.SetParent(parent.transform);
		go.transform.localPosition = localPos;
		go.transform.localScale    = localScale;
		Object.Destroy(go.GetComponent<Collider>());
		go.GetComponent<Renderer>().material = mat;
		return go;
	}
}
