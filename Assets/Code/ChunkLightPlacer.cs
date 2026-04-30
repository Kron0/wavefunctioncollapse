using System.Collections.Generic;
using UnityEngine;

public class ChunkLightPlacer : MonoBehaviour, IMapGenerationCallbackReceiver {
	private MapBehaviour mapBehaviour;
	private MaterialWFC materialWFC;

	[Range(2, 8)]
	public int StreetLightSpacing = 3;

	[Range(0.5f, 5f)]
	public float StreetLightRange = 3f;

	[Range(0.3f, 3f)]
	public float StreetLightIntensity = 1.2f;

	[Range(0.5f, 3f)]
	public float InteriorLightRange = 2f;

	[Range(0.2f, 2f)]
	public float InteriorLightIntensity = 0.8f;

	[Range(0.5f, 3f)]
	public float AccentLightRange = 1.5f;

	[Range(0.3f, 2f)]
	public float AccentLightIntensity = 0.6f;

	public void Initialize(MaterialWFC wfc) {
		this.materialWFC = wfc;
	}

	public void OnEnable() {
		this.mapBehaviour = this.GetComponent<MapBehaviour>();
		this.GetComponent<GenerateMapNearPlayer>().RegisterMapGenerationCallbackReceiver(this);
	}

	public void OnDisable() {
		this.GetComponent<GenerateMapNearPlayer>().UnregisterMapGenerationCallbackReceiver(this);
	}

	public void OnGenerateChunk(Vector3Int chunkAddress, GenerateMapNearPlayer source) {
		if (this.materialWFC == null && this.mapBehaviour != null) {
			this.materialWFC = this.mapBehaviour.GetMaterialWFC();
		}

		int chunkSize = source.ChunkSize;
		int lightCount = 0;

		for (int x = chunkSize * chunkAddress.x; x < chunkSize * (chunkAddress.x + 1); x++) {
			for (int z = chunkSize * chunkAddress.z; z < chunkSize * (chunkAddress.z + 1); z++) {
				for (int y = 0; y < this.mapBehaviour.MapHeight; y++) {
					var pos = new Vector3Int(x, y, z);
					var slot = this.mapBehaviour.Map.GetSlot(pos);
					if (slot == null || !slot.Collapsed) {
						continue;
					}

					var module = slot.Module;
					var palette = this.materialWFC != null ? this.materialWFC.GetPalette(pos) : null;

					if (module.Prototype.IsInterior) {
						TryPlaceInteriorLight(slot, palette);
					} else if (y == 0 || IsGroundLevel(slot)) {
						if (lightCount % this.StreetLightSpacing == 0) {
							TryPlaceStreetLight(slot, palette);
						}
					}

					if (HasPortalFace(module)) {
						TryPlaceAccentLight(slot, palette);
					}

					lightCount++;
				}
			}
		}
	}

	private void TryPlaceStreetLight(Slot slot, MaterialPalette palette) {
		if (slot.GameObject == null) {
			return;
		}

		bool hasWalkableFloor = slot.Module.GetFace(Orientations.UP).Walkable;
		if (!hasWalkableFloor) {
			return;
		}

		var lightGO = new GameObject("StreetLight");
		lightGO.transform.SetParent(slot.GameObject.transform);
		lightGO.transform.localPosition = Vector3.up * AbstractMap.BLOCK_SIZE * 0.9f;

		var light = lightGO.AddComponent<Light>();
		light.type = LightType.Point;
		light.range = this.StreetLightRange;
		light.intensity = this.StreetLightIntensity;
		light.color = palette != null ? palette.LightColor : new Color(1f, 0.9f, 0.7f);
		light.shadows = LightShadows.None;
		light.renderMode = LightRenderMode.Auto;
	}

	private void TryPlaceInteriorLight(Slot slot, MaterialPalette palette) {
		if (slot.GameObject == null) {
			return;
		}

		bool hasWalkableFloor = false;
		for (int d = 0; d < 6; d++) {
			if (slot.Module.GetFace(d).Walkable) {
				hasWalkableFloor = true;
				break;
			}
		}
		if (!hasWalkableFloor) {
			return;
		}

		var lightGO = new GameObject("InteriorLight");
		lightGO.transform.SetParent(slot.GameObject.transform);
		lightGO.transform.localPosition = Vector3.up * AbstractMap.BLOCK_SIZE * 0.7f;

		var light = lightGO.AddComponent<Light>();
		light.type = LightType.Point;
		light.range = this.InteriorLightRange;
		light.intensity = this.InteriorLightIntensity;
		light.color = palette != null ? palette.LightColor * 1.1f : new Color(1f, 0.92f, 0.8f);
		light.shadows = LightShadows.None;
		light.renderMode = LightRenderMode.Auto;
	}

	private void TryPlaceAccentLight(Slot slot, MaterialPalette palette) {
		if (slot.GameObject == null) {
			return;
		}

		var lightGO = new GameObject("AccentLight");
		lightGO.transform.SetParent(slot.GameObject.transform);
		lightGO.transform.localPosition = Vector3.up * AbstractMap.BLOCK_SIZE * 0.5f;

		var light = lightGO.AddComponent<Light>();
		light.type = LightType.Point;
		light.range = this.AccentLightRange;
		light.intensity = this.AccentLightIntensity;

		Color accent = palette != null ? palette.LightColor : new Color(1f, 0.85f, 0.65f);
		float hue, sat, val;
		Color.RGBToHSV(accent, out hue, out sat, out val);
		hue = (hue + 0.05f) % 1f;
		sat = Mathf.Clamp01(sat + 0.2f);
		light.color = Color.HSVToRGB(hue, sat, val);
		light.shadows = LightShadows.None;
		light.renderMode = LightRenderMode.Auto;
	}

	private bool IsGroundLevel(Slot slot) {
		var below = this.mapBehaviour.Map.GetSlot(slot.Position + Vector3Int.down);
		return below == null || (below.Collapsed && !below.Module.GetFace(Orientations.UP).Walkable);
	}

	private static bool HasPortalFace(Module module) {
		for (int d = 0; d < 6; d++) {
			if (module.GetFace(d).IsOcclusionPortal) {
				return true;
			}
		}
		return false;
	}
}
