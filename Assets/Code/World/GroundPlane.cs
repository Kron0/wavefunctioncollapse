using UnityEngine;

// Spawns a large tiled ground plane at Y=0. Auto-follows the player to stay centred.
public class GroundPlane : MonoBehaviour {
	[SerializeField] public Transform playerTransform;
	[SerializeField] private float planeRadius = 300f;
	[SerializeField] private float tileSize    = 6f;

	private GameObject planeGO;
	private Material groundMat;

	void Start() {
		this.groundMat = this.BuildGroundMaterial();
		this.SpawnPlane();
	}

	void Update() {
		if (this.planeGO == null || this.playerTransform == null) return;
		// Keep plane centred under player on XZ, locked to Y=0
		var p = this.playerTransform.position;
		this.planeGO.transform.position = new Vector3(p.x, -0.08f, p.z);
	}

	private void SpawnPlane() {
		this.planeGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
		this.planeGO.name = "GroundPlane";
		this.planeGO.transform.SetParent(this.transform);
		this.planeGO.transform.position = new Vector3(0f, -0.08f, 0f);

		// Unity Plane is 10×10 units; scale to cover playable area
		float s = this.planeRadius / 5f;
		this.planeGO.transform.localScale = new Vector3(s, 1f, s);

		Object.Destroy(this.planeGO.GetComponent<Collider>());
		this.planeGO.GetComponent<Renderer>().sharedMaterial = this.groundMat;
	}

	private Material BuildGroundMaterial() {
		var tex = this.GenerateGroundTexture(512, 512);
		var mat = new Material(Shader.Find("Standard"));
		mat.mainTexture    = tex;
		mat.mainTextureScale = new Vector2(this.planeRadius / this.tileSize, this.planeRadius / this.tileSize);
		mat.SetFloat("_Metallic",   0f);
		mat.SetFloat("_Glossiness", 0.05f);
		return mat;
	}

	private Texture2D GenerateGroundTexture(int w, int h) {
		var tex = new Texture2D(w, h, TextureFormat.RGB24, true);
		var px  = new Color[w * h];

		// Layered ground: base dirt + grass patches + subtle stone paving
		for (int y = 0; y < h; y++) {
			for (int x = 0; x < w; x++) {
				float n1 = Mathf.PerlinNoise(x * 0.04f, y * 0.04f);
				float n2 = Mathf.PerlinNoise(x * 0.10f + 5f, y * 0.10f + 5f);
				float n3 = Mathf.PerlinNoise(x * 0.25f + 11f, y * 0.25f + 11f) * 0.3f;

				// Stone grid overlay (subtle paving cracks)
				int bx = x % 32, by = y % 32;
				bool stoneEdge = bx < 2 || by < 2;

				Color dirt  = new Color(0.42f + n3 * 0.06f, 0.33f + n3 * 0.04f, 0.22f);
				Color grass = new Color(0.32f + n3 * 0.08f, 0.52f + n1 * 0.12f, 0.24f);
				Color stone = new Color(0.55f, 0.52f, 0.48f);

				float grassMix = Mathf.Clamp01(n1 * 1.4f + n2 * 0.6f - 0.35f);
				Color base_ = Color.Lerp(dirt, grass, grassMix);
				if (stoneEdge) base_ = Color.Lerp(base_, stone, 0.28f);

				px[y * w + x] = base_;
			}
		}

		tex.SetPixels(px);
		tex.Apply();
		tex.filterMode = FilterMode.Bilinear;
		tex.wrapMode   = TextureWrapMode.Repeat;
		return tex;
	}
}
