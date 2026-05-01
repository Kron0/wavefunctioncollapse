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
	private IWPositionProvider player;
	private int activeWLayer = 0;
	private MaterialWFC materialWFC;

	// Per-W-layer collider lists for O(layer size) updates instead of O(all slots)
	private readonly Dictionary<int, List<Collider>> collidersByLayer = new Dictionary<int, List<Collider>>();

	// All built slots (main-thread only) — avoids per-frame GetAllSlots().ToArray() snapshot
	private readonly List<Slot4D> builtSlots = new List<Slot4D>();

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

	public void SetPlayer(IWPositionProvider controller) {
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
		this.builtSlots.Clear();
		this.collidersByLayer.Clear();
	}

	public bool Initialized {
		get {
			return this.Map != null;
		}
	}

	// Max milliseconds per frame devoted to building slot GameObjects.
	// Keeps frame-time predictable regardless of how many cheap vs expensive slots dequeue.
	private const float BuildBudgetMs = 3.5f;

	[Header("Logging")]
	public bool VerboseLogging = false;

	public void Update() {
		if (this.Map == null) {
			return;
		}

		float budgetEnd = Time.realtimeSinceStartup + BuildBudgetMs * 0.001f;
		while (Time.realtimeSinceStartup < budgetEnd && this.Map.BuildQueue.TryDequeue(out var slot)) {
			if (slot == null) break;
			if (this.BuildSlot(slot)) GameState.BuiltSlotCount++;
		}

		if (this.player != null) {
			int newWLayer = Mathf.RoundToInt(this.player.WPosition);
			newWLayer = Mathf.Clamp(newWLayer, 0, GameState.NumWLayers - 1);
			if (newWLayer != this.activeWLayer) {
				bool hasGround = this.HasSolidGround(newWLayer, this.player.transform.position);
				if (this.VerboseLogging) Debug.Log($"[Map4D] W layer change {this.activeWLayer}→{newWLayer}: HasSolidGround={hasGround}");
				if (hasGround) {
					int previousWLayer = this.activeWLayer;
					this.activeWLayer = newWLayer;
					this.UpdateColliders();
					if (!this.player.SnapToGround()) {
						if (this.VerboseLogging) Debug.Log($"[Map4D] SnapToGround failed for W={newWLayer}, reverting to W={previousWLayer}");
						this.activeWLayer = previousWLayer;
						this.UpdateColliders();
						this.player.ClampWPosition(previousWLayer);
					} else {
						if (this.VerboseLogging) Debug.Log($"[Map4D] W layer confirmed: {this.activeWLayer}");
						OnWLayerChanged?.Invoke(this.activeWLayer);
					}
				} else {
					if (this.VerboseLogging) Debug.Log($"[Map4D] W={newWLayer} has no solid ground, clamping to W={this.activeWLayer}");
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

		bool enabled = slot.Position.w == this.activeWLayer;
		SetCollidersEnabled(gameObject, enabled);

		// Register colliders in per-layer index for fast UpdateColliders
		var cols = gameObject.GetComponentsInChildren<Collider>();
		if (cols.Length > 0) {
			int wLayer = slot.Position.w;
			if (!this.collidersByLayer.TryGetValue(wLayer, out var colList)) {
				colList = new List<Collider>(cols.Length);
				this.collidersByLayer[wLayer] = colList;
			}
			colList.AddRange(cols);
		}

		this.builtSlots.Add(slot);
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
		foreach (var kv in this.collidersByLayer) {
			bool enabled = kv.Key == this.activeWLayer;
			var list = kv.Value;
			for (int i = list.Count - 1; i >= 0; i--) {
				if (list[i] == null) { list.RemoveAt(i); continue; }
				list[i].enabled = enabled;
			}
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

		for (int i = this.builtSlots.Count - 1; i >= 0; i--) {
			var slot = this.builtSlots[i];
			if (slot.GameObject == null) { this.builtSlots.RemoveAt(i); continue; }

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
