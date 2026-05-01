using System.Collections.Generic;
using UnityEngine;

public class DecorationPlacer : MonoBehaviour, IMapGenerationCallbackReceiver {
	private MapBehaviour mapBehaviour;
	private MaterialWFC materialWFC;
	private System.Random random = new System.Random();

	public GameObject[] OutdoorProps;
	public GameObject[] IndoorProps;
	public Material[] GroundDecalMaterials;

	[Range(2, 8)]
	public int PropSpacing = 4;

	[Range(1, 4)]
	public int DecalSpacing = 2;

	[Range(0f, 1f)]
	public float PropProbability = 0.4f;

	[Range(0f, 1f)]
	public float DecalProbability = 0.6f;

	public int MaxPropsPerChunk = 6;
	public int MaxDecalsPerChunk = 12;

	public void Initialize(MaterialWFC wfc) {
		this.materialWFC = wfc;
	}

	public void OnEnable() {
		this.mapBehaviour = this.GetComponent<MapBehaviour>();
		var generator = this.GetComponent<GenerateMapNearPlayer>();
		if (generator != null) {
			generator.RegisterMapGenerationCallbackReceiver(this);
		}
	}

	public void OnDisable() {
		this.GetComponent<GenerateMapNearPlayer>().UnregisterMapGenerationCallbackReceiver(this);
	}

	public void OnGenerateChunk(Vector3Int chunkAddress, GenerateMapNearPlayer source) {
		if (this.materialWFC == null && this.mapBehaviour != null) {
			this.materialWFC = this.mapBehaviour.GetMaterialWFC();
		}

		int chunkSize = source.ChunkSize;
		int propsPlaced = 0;
		int decalsPlaced = 0;
		int slotIndex = 0;

		for (int x = chunkSize * chunkAddress.x; x < chunkSize * (chunkAddress.x + 1); x++) {
			for (int z = chunkSize * chunkAddress.z; z < chunkSize * (chunkAddress.z + 1); z++) {
				for (int y = 0; y < this.mapBehaviour.MapHeight; y++) {
					var pos = new Vector3Int(x, y, z);
					var slot = this.mapBehaviour.Map.GetSlot(pos);
					if (slot == null || !slot.Collapsed || slot.GameObject == null) {
						continue;
					}

					var module = slot.Module;
					bool isWalkableTop = module.GetFace(Orientations.UP).Walkable;

					if (!isWalkableTop) {
						continue;
					}

					bool isInterior = module.Prototype.IsInterior;

					if (propsPlaced < this.MaxPropsPerChunk
						&& slotIndex % this.PropSpacing == 0
						&& this.random.NextDouble() < this.PropProbability) {
						TryPlaceProp(slot, isInterior);
						propsPlaced++;
					}

					if (decalsPlaced < this.MaxDecalsPerChunk
						&& slotIndex % this.DecalSpacing == 0
						&& this.random.NextDouble() < this.DecalProbability) {
						TryPlaceDecal(slot);
						decalsPlaced++;
					}

					slotIndex++;
				}
			}
		}
	}

	private void TryPlaceProp(Slot slot, bool isInterior) {
		GameObject[] propSet = isInterior ? this.IndoorProps : this.OutdoorProps;
		if (propSet == null || propSet.Length == 0) {
			return;
		}

		var prefab = propSet[this.random.Next(propSet.Length)];
		if (prefab == null) {
			return;
		}

		var prop = GameObject.Instantiate(prefab);
		prop.transform.SetParent(slot.GameObject.transform);

		float offsetX = (float)(this.random.NextDouble() - 0.5) * AbstractMap.BLOCK_SIZE * 0.6f;
		float offsetZ = (float)(this.random.NextDouble() - 0.5) * AbstractMap.BLOCK_SIZE * 0.6f;
		prop.transform.localPosition = new Vector3(offsetX, 0f, offsetZ);

		float yRotation = (float)(this.random.NextDouble() * 360.0);
		prop.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
	}

	private void TryPlaceDecal(Slot slot) {
		if (this.GroundDecalMaterials == null || this.GroundDecalMaterials.Length == 0) {
			return;
		}

		var material = this.GroundDecalMaterials[this.random.Next(this.GroundDecalMaterials.Length)];
		if (material == null) {
			return;
		}

		var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
		decal.name = "GroundDecal";
		Object.Destroy(decal.GetComponent<Collider>());

		decal.transform.SetParent(slot.GameObject.transform);
		decal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

		float offsetX = (float)(this.random.NextDouble() - 0.5) * AbstractMap.BLOCK_SIZE * 0.7f;
		float offsetZ = (float)(this.random.NextDouble() - 0.5) * AbstractMap.BLOCK_SIZE * 0.7f;
		decal.transform.localPosition = new Vector3(offsetX, 0.01f, offsetZ);

		float scale = 0.3f + (float)(this.random.NextDouble() * 0.7f);
		decal.transform.localScale = new Vector3(scale, scale, 1f);

		float yRotation = (float)(this.random.NextDouble() * 360.0);
		decal.transform.localRotation = Quaternion.Euler(90f, yRotation, 0f);

		decal.GetComponent<Renderer>().material = material;
	}
}
