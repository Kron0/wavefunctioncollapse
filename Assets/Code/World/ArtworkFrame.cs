using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// Tracks a slot's world transform and downloads/applies a painting texture.
public class ArtworkFrame : MonoBehaviour {
	private Transform   slotTransform;
	private Vector3     wallWorldDir;   // unit vector toward the wall (away from the room)
	private float       blockSize;
	private Vector3     frameWorldSize; // natural size of the frame in world units
	private Renderer    canvasRenderer;
	private string      imageUrl;

	private static readonly int MainTexID  = Shader.PropertyToID("_MainTex");
	private static readonly int ColorID    = Shader.PropertyToID("_Color");

	public void Init(Transform slotTransform, Vector3 wallWorldDir, float blockSize,
		string imageUrl, Renderer canvasRenderer) {

		this.slotTransform  = slotTransform;
		this.wallWorldDir   = wallWorldDir.normalized;
		this.blockSize      = blockSize;
		this.frameWorldSize = new Vector3(blockSize * 0.36f, blockSize * 0.43f, blockSize * 0.04f);
		this.canvasRenderer = canvasRenderer;
		this.imageUrl       = imageUrl;
	}

	void Start() {
		if (!string.IsNullOrEmpty(this.imageUrl)) {
			StartCoroutine(this.DownloadTexture());
		}
	}

	void LateUpdate() {
		if (this.slotTransform == null) { Destroy(this.gameObject); return; }

		bool visible = this.slotTransform.gameObject.activeSelf;
		if (this.gameObject.activeSelf != visible) {
			this.gameObject.SetActive(visible);
		}
		if (!visible) return;

		// Sync world position and scale with the slot's current projection state
		float projScale = this.slotTransform.lossyScale.x;
		this.transform.position = this.slotTransform.position
			+ this.wallWorldDir * this.blockSize * 0.47f * projScale;
		this.transform.rotation = Quaternion.LookRotation(-this.wallWorldDir, Vector3.up);
		this.transform.localScale = this.frameWorldSize * projScale;
	}

	private IEnumerator DownloadTexture() {
		var req = UnityWebRequestTexture.GetTexture(this.imageUrl);
		yield return req.SendWebRequest();

		if (!req.isNetworkError && !req.isHttpError && this.canvasRenderer != null) {
			var tex = DownloadHandlerTexture.GetContent(req);
			tex.wrapMode = TextureWrapMode.Clamp;
			var mat = this.canvasRenderer.material;
			mat.SetTexture(MainTexID, tex);
			mat.SetColor(ColorID, Color.white);
		}
		req.Dispose();
	}
}
