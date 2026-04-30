using UnityEngine;

public class WTransitionEffect : MonoBehaviour {
	private int previousWLayer;
	private float flashAlpha;
	private Color flashColor = Color.cyan;
	private Texture2D flashTex;

	void OnEnable() {
		MapBehaviour4D.OnWLayerChanged += this.OnWLayerChanged;
	}

	void OnDisable() {
		MapBehaviour4D.OnWLayerChanged -= this.OnWLayerChanged;
	}

	void Start() {
		this.flashTex = new Texture2D(1, 1);
		this.flashTex.SetPixel(0, 0, Color.white);
		this.flashTex.Apply();
		this.previousWLayer = MapBehaviour4D.ActiveWLayer;
	}

	private void OnWLayerChanged(int newLayer) {
		bool goingUp = newLayer > this.previousWLayer;
		this.flashColor = goingUp
			? new Color(0f, 0.8f, 1f)
			: new Color(1f, 0.2f, 0.8f);
		this.flashAlpha = 0.45f;
		this.previousWLayer = newLayer;
	}

	void Update() {
		if (this.flashAlpha > 0f) {
			this.flashAlpha -= Time.deltaTime * (1f / 0.3f);
			if (this.flashAlpha < 0f) {
				this.flashAlpha = 0f;
			}
		}
	}

	void OnGUI() {
		if (this.flashAlpha <= 0f || this.flashTex == null) {
			return;
		}
		GUI.color = new Color(this.flashColor.r, this.flashColor.g, this.flashColor.b, this.flashAlpha);
		GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), this.flashTex);
		GUI.color = Color.white;
	}
}
