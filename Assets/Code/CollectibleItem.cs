using UnityEngine;

public class CollectibleItem : MonoBehaviour {
	public int requiredWLayer;

	private bool collected;
	private Transform player;

	void Start() {
		var playerGO = GameObject.FindGameObjectWithTag("Player");
		if (playerGO != null) {
			this.player = playerGO.transform;
		}
	}

	void Update() {
		if (this.collected || this.player == null) {
			return;
		}
		if (MapBehaviour4D.ActiveWLayer != this.requiredWLayer) {
			return;
		}
		if (Vector3.Distance(this.transform.position, this.player.position) > 1.5f) {
			return;
		}
		this.collected = true;
		CollectiblePlacer.TotalCollected++;
		Destroy(this.gameObject);
	}
}
