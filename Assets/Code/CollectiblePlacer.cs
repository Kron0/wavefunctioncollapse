using UnityEngine;

public class CollectiblePlacer : MonoBehaviour, IMapGenerationCallbackReceiver4D {
	private GenerateMap4DNearPlayer generator;
	private MapBehaviour4D mapBehaviour;

	public static int TotalCollected = 0;
	public static int TotalPlaced    = 0;

	void OnEnable() {
		TotalCollected = 0;
		TotalPlaced    = 0;
		this.generator    = this.GetComponent<GenerateMap4DNearPlayer>();
		this.mapBehaviour = this.GetComponent<MapBehaviour4D>();
		if (this.generator != null) this.generator.RegisterCallback4D(this);
	}

	void OnDisable() {
		if (this.generator != null) this.generator.UnregisterCallback4D(this);
	}

	public void OnGenerateChunk4D(Vector4Int chunkAddress, GenerateMap4DNearPlayer source) {
		if (this.mapBehaviour == null || this.mapBehaviour.Map == null) return;

		var rng       = new System.Random(chunkAddress.GetHashCode() ^ 0xC011EC7);
		int chunkSize = source.ChunkSize;
		int topY      = this.mapBehaviour.MapHeight - 1;

		for (int dx = 0; dx < chunkSize; dx++) {
			for (int dz = 0; dz < chunkSize; dz++) {
				for (int dw = 0; dw < chunkSize; dw++) {
					var pos = new Vector4Int(
						chunkAddress.x * chunkSize + dx,
						topY,
						chunkAddress.z * chunkSize + dz,
						chunkAddress.w * chunkSize + dw);
					var slot = this.mapBehaviour.Map.GetSlot(pos);
					if (slot == null || !slot.Collapsed || slot.GameObject == null) continue;
					if (!slot.Module.Prototype.Up.Walkable) continue;
					if (rng.NextDouble() > 0.12f) continue;

					Vector3 worldPos = slot.GameObject.transform.position
						+ Vector3.up * AbstractMap4D.BLOCK_SIZE * 0.85f;
					this.SpawnCollectible(worldPos, pos.w);
				}
			}
		}
	}

	private void SpawnCollectible(Vector3 worldPos, int wLayer) {
		var root = new GameObject("Collectible");
		root.transform.position = worldPos;
		root.transform.SetParent(this.transform);

		Color gold     = DimensionColors.ArtifactGold;
		Color goldDim  = gold * 0.6f;
		Color goldBright = gold * 6f;

		// Shared gold material
		var mat = new Material(Shader.Find("Standard"));
		mat.color = gold * 0.5f;
		mat.SetColor("_EmissionColor", goldDim);
		mat.EnableKeyword("_EMISSION");
		mat.SetFloat("_Metallic", 0.7f);
		mat.SetFloat("_Glossiness", 0.8f);

		// Core: cube pre-rotated to diamond shape
		var core = GameObject.CreatePrimitive(PrimitiveType.Cube);
		core.name = "Core";
		core.transform.SetParent(root.transform);
		core.transform.localPosition  = Vector3.zero;
		core.transform.localScale     = new Vector3(0.28f, 0.28f, 0.28f);
		core.transform.localRotation  = Quaternion.Euler(35f, 45f, 35f);
		Object.Destroy(core.GetComponent<Collider>());
		core.GetComponent<Renderer>().material = mat;

		// Thin orbit ring (very flat cylinder)
		var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		ring.name = "Ring";
		ring.transform.SetParent(root.transform);
		ring.transform.localPosition = Vector3.zero;
		ring.transform.localScale    = new Vector3(0.60f, 0.012f, 0.60f);
		ring.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);  // slight tilt
		Object.Destroy(ring.GetComponent<Collider>());
		ring.GetComponent<Renderer>().material = mat;

		// Inner glow sphere (brighter emission, smaller)
		var glowMat = new Material(Shader.Find("Standard"));
		glowMat.color = gold;
		glowMat.SetColor("_EmissionColor", goldBright);
		glowMat.EnableKeyword("_EMISSION");

		var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		glow.name = "Glow";
		glow.transform.SetParent(root.transform);
		glow.transform.localPosition = Vector3.zero;
		glow.transform.localScale    = new Vector3(0.16f, 0.16f, 0.16f);
		Object.Destroy(glow.GetComponent<Collider>());
		glow.GetComponent<Renderer>().material = glowMat;

		// Point light
		var lightGO = new GameObject("Light");
		lightGO.transform.SetParent(root.transform);
		lightGO.transform.localPosition = Vector3.zero;
		var light = lightGO.AddComponent<Light>();
		light.type      = LightType.Point;
		light.color     = gold;
		light.intensity = 0.8f;
		light.range     = 6f;

		// Sphere collider on root for collection trigger
		var col = root.AddComponent<SphereCollider>();
		col.radius    = 0.5f;
		col.isTrigger = true;

		var item = root.AddComponent<CollectibleItem>();
		item.requiredWLayer = wLayer;

		TotalPlaced++;
	}
}
