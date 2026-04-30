using UnityEngine;

public class WGateBlocker : MonoBehaviour {
	public int wGateLayer;

	void OnEnable() {
		MapBehaviour4D.OnWLayerChanged += this.HandleWLayerChanged;
		this.UpdateState(MapBehaviour4D.ActiveWLayer);
	}

	void OnDisable() {
		MapBehaviour4D.OnWLayerChanged -= this.HandleWLayerChanged;
	}

	private void HandleWLayerChanged(int newLayer) {
		this.UpdateState(newLayer);
	}

	private void UpdateState(int activeLayer) {
		bool open = activeLayer == this.wGateLayer;
		foreach (var col in this.GetComponentsInChildren<Collider>()) {
			col.enabled = !open;
		}
		foreach (var ren in this.GetComponentsInChildren<Renderer>()) {
			ren.enabled = !open;
		}
	}
}
