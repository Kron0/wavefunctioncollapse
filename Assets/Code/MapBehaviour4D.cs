using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MapBehaviour4D : MonoBehaviour {
	public InfiniteMap4D Map;

	public int MapHeight = 6;

	public BoundaryConstraint[] BoundaryConstraints;

	public bool ApplyBoundaryConstraints = true;

	public ModuleData ModuleData;

	public static event Action<int> OnWLayerChanged;
	public static int ActiveWLayer { get; private set; }

	private TesseractProjection projection;
	private FourDController player;
	private int activeWLayer = 0;
	private MaterialWFC materialWFC;

	public void Initialize() {
		ModuleData.Current = this.ModuleData.Modules;
		Module.Initialize4DNeighbors(ModuleData.Current);
		this.Clear();
		this.Map = new InfiniteMap4D(this.MapHeight);
		if (this.ApplyBoundaryConstraints && this.BoundaryConstraints != null && this.BoundaryConstraints.Any()) {
			this.Map.ApplyBoundaryConstraints(this.BoundaryConstraints);
		}
		this.projection = this.GetComponent<TesseractProjection>();
		SlotMaterializer.EnsureInitialized();
		this.materialWFC = new MaterialWFC();
	}

	public void SetPlayer(FourDController controller) {
		this.player = controller;
	}

	public void Clear() {
		var children = new List<Transform>();
		foreach (Transform child in this.transform) {
			children.Add(child);
		}
		foreach (var child in children) {
			GameObject.DestroyImmediate(child.gameObject);
		}
		this.Map = null;
	}

	public bool Initialized {
		get {
			return this.Map != null;
		}
	}

	public void Update() {
		if (this.Map == null) {
			return;
		}

		int itemsLeft = 150;

		while (itemsLeft > 0 && this.Map.BuildQueue.TryDequeue(out var slot)) {
			if (slot == null) break;
			if (this.BuildSlot(slot)) {
				itemsLeft--;
			}
		}

		if (this.player != null) {
			int newWLayer = Mathf.RoundToInt(this.player.WPosition);
			if (newWLayer != this.activeWLayer) {
				if (this.HasSolidGround(newWLayer, this.player.transform.position)) {
					int previousWLayer = this.activeWLayer;
					this.activeWLayer = newWLayer;
					this.UpdateColliders();
					if (!this.player.SnapToGround()) {
						// No floor found in new layer — revert
						this.activeWLayer = previousWLayer;
						this.UpdateColliders();
						this.player.ClampWPosition(previousWLayer);
					} else {
						OnWLayerChanged?.Invoke(this.activeWLayer);
					}
				} else {
					this.player.ClampWPosition(this.activeWLayer);
				}
			}
			ActiveWLayer = this.activeWLayer;
		}
	}

	public bool BuildSlot(Slot4D slot) {
		if (slot.GameObject != null) {
#if UNITY_EDITOR
			GameObject.DestroyImmediate(slot.GameObject);
#else
			GameObject.Destroy(slot.GameObject);
#endif
		}

		if (!slot.Collapsed || slot.Module.Prototype.Spawn == false) {
			return false;
		}
		var module = slot.Module;
		if (module == null) {
			return false;
		}

		if (this.projection != null && !this.projection.IsInRenderRange(slot.Position.w)) {
			return false;
		}

		var gameObject = GameObject.Instantiate(module.Prototype.gameObject);
		gameObject.name = module.Prototype.gameObject.name + " " + slot.Position;
		GameObject.DestroyImmediate(gameObject.GetComponent<ModulePrototype>());
		gameObject.transform.parent = this.transform;

		float blockScale = AbstractMap4D.BLOCK_SIZE / 2f;
		if (this.projection != null) {
			gameObject.transform.position = this.projection.ProjectPosition(slot.Position);
			float scale = this.projection.GetScale(slot.Position.w);
			gameObject.transform.localScale = Vector3.one * scale * blockScale;
		} else {
			gameObject.transform.position = this.transform.position
				+ Vector3.up * AbstractMap4D.BLOCK_SIZE / 2f
				+ slot.Position.ToVector3() * AbstractMap4D.BLOCK_SIZE;
			gameObject.transform.localScale = Vector3.one * blockScale;
		}

		gameObject.transform.rotation = Quaternion.Euler(Vector3.up * 90f * module.Rotation);
		slot.GameObject = gameObject;

		if (this.materialWFC != null) {
			var palette = this.materialWFC.GetPalette(slot.Position.ToVector3Int(), slot.Position.w);
			float slotAlpha = this.projection != null ? this.projection.GetAlpha(slot.Position.w) : 1f;
			SlotMaterializer.Apply(slot, gameObject, palette, slotAlpha);
		}

		SetCollidersEnabled(gameObject, slot.Position.w == this.activeWLayer);

		return true;
	}

	private bool HasSolidGround(int wLayer, Vector3 playerWorldPos) {
		if (this.Map == null) {
			return false;
		}
		// Convert player world position back to map coords
		int mapX = Mathf.RoundToInt(playerWorldPos.x / AbstractMap4D.BLOCK_SIZE);
		int mapZ = Mathf.RoundToInt(playerWorldPos.z / AbstractMap4D.BLOCK_SIZE);

		// Check if any slot at ground level in the new W layer is collapsed and built nearby
		for (int dx = -1; dx <= 1; dx++) {
			for (int dz = -1; dz <= 1; dz++) {
				for (int y = 0; y < this.MapHeight; y++) {
					var pos = new Vector4Int(mapX + dx, y, mapZ + dz, wLayer);
					var slot = this.Map.GetSlot(pos);
					if (slot != null && slot.Collapsed && slot.ConstructionComplete) {
						return true;
					}
				}
			}
		}
		return false;
	}

	private void UpdateColliders() {
		foreach (var slot in this.Map.GetAllSlots().ToArray()) {
			if (slot.GameObject == null) continue;
			SetCollidersEnabled(slot.GameObject, slot.Position.w == this.activeWLayer);
		}
	}

	private static void SetCollidersEnabled(GameObject go, bool enabled) {
		foreach (var col in go.GetComponentsInChildren<Collider>()) {
			col.enabled = enabled;
		}
	}

	public void BuildAllSlots() {
		while (this.Map.BuildQueue.TryDequeue(out var slot)) {
			this.BuildSlot(slot);
		}
	}

	public void UpdateSlotPositions() {
		if (this.Map == null || this.projection == null) {
			return;
		}

		foreach (var slot in this.Map.GetAllSlots().ToArray()) {
			if (slot.GameObject == null) continue;

			if (!this.projection.IsInRenderRange(slot.Position.w)) {
				slot.GameObject.SetActive(false);
				continue;
			}

			slot.GameObject.SetActive(true);
			slot.GameObject.transform.position = this.projection.ProjectPosition(slot.Position);
			float scale = this.projection.GetScale(slot.Position.w);
			slot.GameObject.transform.localScale = Vector3.one * scale * (AbstractMap4D.BLOCK_SIZE / 2f);

			float alpha = this.projection.GetAlpha(slot.Position.w);
			SlotMaterializer.UpdateAlpha(slot.GameObject, alpha);
		}
	}
}
