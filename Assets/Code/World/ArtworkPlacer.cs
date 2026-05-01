using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ArtworkPlacer : MonoBehaviour, IMapGenerationCallbackReceiver4D {
	private GenerateMap4DNearPlayer generator;
	private MapBehaviour4D          mapBehaviour;
	private readonly List<string>   imageUrls = new List<string>();
	private bool urlsFetched = false;

	// World-space vectors matching Orientations index order
	private static readonly Vector3[] FaceWorldDir = {
		Vector3.left,    // 0 LEFT
		Vector3.down,    // 1 DOWN
		Vector3.back,    // 2 BACK
		Vector3.right,   // 3 RIGHT
		Vector3.up,      // 4 UP
		Vector3.forward  // 5 FORWARD
	};

	void OnEnable() {
		this.generator    = this.GetComponent<GenerateMap4DNearPlayer>();
		this.mapBehaviour = this.GetComponent<MapBehaviour4D>();
		if (this.generator != null) this.generator.RegisterCallback4D(this);
		this.imageUrls.Clear();
		this.urlsFetched = false;
		StartCoroutine(this.FetchArtworkUrls());
	}

	void OnDisable() {
		if (this.generator != null) this.generator.UnregisterCallback4D(this);
	}

	public void OnGenerateChunk4D(Vector4Int chunkAddress, GenerateMap4DNearPlayer source) {
		if (this.mapBehaviour?.Map == null) return;
		if (!this.urlsFetched || this.imageUrls.Count == 0) return;

		int  chunkSize = source.ChunkSize;
		int  mapHeight = this.mapBehaviour.MapHeight;
		var  rng       = new System.Random(chunkAddress.GetHashCode() ^ 0xA17F4A1);

		var candidates = new List<(Slot4D slot, int faceDir, string url)>();

		for (int dx = 0; dx < chunkSize; dx++) {
			for (int dz = 0; dz < chunkSize; dz++) {
				for (int dw = 0; dw < chunkSize; dw++) {
					for (int dy = 1; dy <= Mathf.Min(2, mapHeight - 2); dy++) {
						var pos = new Vector4Int(
							chunkAddress.x * chunkSize + dx,
							dy,
							chunkAddress.z * chunkSize + dz,
							chunkAddress.w * chunkSize + dw);

						var slot = this.mapBehaviour.Map.GetSlot(pos);
						if (slot == null || !slot.Collapsed) continue;

						// Only paint on accessible slots (at least one walkable horizontal face)
						bool hasRoomAccess = false;
						foreach (int hd in Orientations.HorizontalDirections) {
							var hf = slot.Module.GetFace(hd) as ModulePrototype.HorizontalFaceDetails;
							if (hf?.Walkable == true) { hasRoomAccess = true; break; }
						}
						if (!hasRoomAccess) continue;

						// Paint each solid wall face with 18% probability (one per slot)
						foreach (int fd in Orientations.HorizontalDirections) {
							var face = slot.Module.GetFace(fd) as ModulePrototype.HorizontalFaceDetails;
							if (face == null || face.Walkable) continue;
							if (rng.NextDouble() > 0.18f) continue;

							candidates.Add((slot, fd, this.imageUrls[rng.Next(this.imageUrls.Count)]));
							break;
						}
					}
				}
			}
		}

		if (candidates.Count > 0) {
			StartCoroutine(this.PlaceArtworkDeferred(candidates));
		}
	}

	private IEnumerator PlaceArtworkDeferred(List<(Slot4D slot, int faceDir, string url)> candidates) {
		float deadline = Time.time + 4f;
		foreach (var (slot, faceDir, url) in candidates) {
			while (slot.GameObject == null && Time.time < deadline) {
				yield return null;
			}
			if (slot.GameObject == null) continue;
			this.SpawnFrame(slot, FaceWorldDir[faceDir], url);
		}
	}

	private void SpawnFrame(Slot4D slot, Vector3 wallWorldDir, string imageUrl) {
		float bs     = AbstractMap4D.BLOCK_SIZE;
		Color accent = DimensionColors.ForLayer(slot.Position.w);

		// Frame proportions (all as fractions of the root's local space where 1 unit = frame outer size)
		// Root's world scale is set by ArtworkFrame: (bs*0.36, bs*0.43, bs*0.04)

		var root = new GameObject("ArtworkFrame");
		root.transform.SetParent(this.transform);

		// ── Outer backing plate (dark, absorbs light like a recessed shadow box)
		var backMat = new Material(Shader.Find("Standard")) {
			color = new Color(0.06f, 0.05f, 0.04f)
		};
		backMat.SetFloat("_Metallic",    0.0f);
		backMat.SetFloat("_Glossiness",  0.0f);
		this.MakePart(root, "Backing", Vector3.zero,
			new Vector3(1.0f, 1.0f, 0.60f), backMat);

		// ── Outer frame moulding (four bars, proud of backing plate, warm gold)
		var frameMat = new Material(Shader.Find("Standard")) {
			color = new Color(0.62f, 0.50f, 0.28f)
		};
		frameMat.SetFloat("_Metallic",   0.85f);
		frameMat.SetFloat("_Glossiness", 0.55f);
		frameMat.SetColor("_EmissionColor", new Color(0.10f, 0.08f, 0.04f));
		frameMat.EnableKeyword("_EMISSION");

		float bw = 0.11f;   // border fraction of width
		float bh = 0.09f;   // border fraction of height
		float fz = 0.38f;   // frame bar local-Z center (proud of backing)
		float fd = 0.50f;   // frame bar local-Z depth

		// Top & bottom bars span full width
		this.MakePart(root, "FrameTop",    new Vector3(0f,  0.5f - bh * 0.5f, fz), new Vector3(1.0f, bh, fd), frameMat);
		this.MakePart(root, "FrameBottom", new Vector3(0f, -0.5f + bh * 0.5f, fz), new Vector3(1.0f, bh, fd), frameMat);
		// Left & right bars fit between top/bottom
		this.MakePart(root, "FrameLeft",   new Vector3(-0.5f + bw * 0.5f, 0f, fz), new Vector3(bw, 1f - bh * 2f, fd), frameMat);
		this.MakePart(root, "FrameRight",  new Vector3( 0.5f - bw * 0.5f, 0f, fz), new Vector3(bw, 1f - bh * 2f, fd), frameMat);

		// ── Inner bevel strips (darker, create depth illusion inside the moulding)
		var bevelMat = new Material(Shader.Find("Standard")) {
			color = new Color(0.18f, 0.14f, 0.08f)
		};
		bevelMat.SetFloat("_Metallic",   0.5f);
		bevelMat.SetFloat("_Glossiness", 0.3f);

		float ibw = bw * 0.30f;
		float ibh = bh * 0.35f;
		float ibz = 0.08f;
		this.MakePart(root, "BevelTop",    new Vector3(0f,  0.5f - bh + ibh * 0.5f, ibz), new Vector3(1f - bw * 2f, ibh, 0.25f), bevelMat);
		this.MakePart(root, "BevelBottom", new Vector3(0f, -0.5f + bh - ibh * 0.5f, ibz), new Vector3(1f - bw * 2f, ibh, 0.25f), bevelMat);
		this.MakePart(root, "BevelLeft",   new Vector3(-0.5f + bw - ibw * 0.5f, 0f, ibz), new Vector3(ibw, 1f - bh * 2f, 0.25f), bevelMat);
		this.MakePart(root, "BevelRight",  new Vector3( 0.5f - bw + ibw * 0.5f, 0f, ibz), new Vector3(ibw, 1f - bh * 2f, 0.25f), bevelMat);

		// ── Accent corner squares (tiny, glowing, W-layer color)
		var cornerMat = new Material(Shader.Find("Standard")) {
			color = accent * 0.4f
		};
		cornerMat.SetColor("_EmissionColor", accent * 0.6f);
		cornerMat.EnableKeyword("_EMISSION");
		cornerMat.SetFloat("_Metallic",   0.2f);
		cornerMat.SetFloat("_Glossiness", 0.9f);

		float cw = bw * 0.85f;
		float ch = bh * 0.85f;
		float cz = fz + 0.02f;
		this.MakePart(root, "CornTL", new Vector3(-0.5f + bw * 0.5f,  0.5f - bh * 0.5f, cz), new Vector3(cw, ch, 0.06f), cornerMat);
		this.MakePart(root, "CornTR", new Vector3( 0.5f - bw * 0.5f,  0.5f - bh * 0.5f, cz), new Vector3(cw, ch, 0.06f), cornerMat);
		this.MakePart(root, "CornBL", new Vector3(-0.5f + bw * 0.5f, -0.5f + bh * 0.5f, cz), new Vector3(cw, ch, 0.06f), cornerMat);
		this.MakePart(root, "CornBR", new Vector3( 0.5f - bw * 0.5f, -0.5f + bh * 0.5f, cz), new Vector3(cw, ch, 0.06f), cornerMat);

		// ── Mat board (off-white linen border between frame and canvas)
		var matBoardMat = new Material(Shader.Find("Standard")) {
			color = new Color(0.91f, 0.88f, 0.82f)
		};
		matBoardMat.SetFloat("_Metallic",   0.0f);
		matBoardMat.SetFloat("_Glossiness", 0.05f);

		float mw = 1f - bw * 2f;
		float mh = 1f - bh * 2f;
		float mz = 0.18f;
		this.MakePart(root, "MatBoard", new Vector3(0f, 0f, mz), new Vector3(mw, mh, 0.06f), matBoardMat);

		// ── Canvas quad (receives the downloaded painting)
		float cvw = mw - 0.08f;
		float cvh = mh - 0.08f;
		float cvz = 0.30f;

		var canvasGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
		canvasGO.name = "Canvas";
		canvasGO.transform.SetParent(root.transform);
		canvasGO.transform.localPosition = new Vector3(0f, 0f, cvz);
		canvasGO.transform.localScale    = new Vector3(cvw, cvh, 1f);
		UnityEngine.Object.Destroy(canvasGO.GetComponent<Collider>());

		var canvasMat = new Material(Shader.Find("Standard")) {
			color = new Color(0.90f, 0.88f, 0.84f)
		};
		canvasMat.SetFloat("_Metallic",   0.0f);
		canvasMat.SetFloat("_Glossiness", 0.02f);
		canvasGO.GetComponent<Renderer>().material = canvasMat;

		// ── Gallery spot light (warm, above and angled down)
		var lightGO = new GameObject("SpotLight");
		lightGO.transform.SetParent(root.transform);
		lightGO.transform.localPosition = new Vector3(0f, 0.55f, 0.60f);
		lightGO.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
		var spotLight = lightGO.AddComponent<Light>();
		spotLight.type      = LightType.Spot;
		spotLight.spotAngle = 40f;
		spotLight.color     = new Color(1.0f, 0.92f, 0.78f);
		spotLight.intensity = 0.9f;
		spotLight.range     = 4.5f;
		spotLight.shadows   = LightShadows.None;

		// ── Wire ArtworkFrame to drive world position / visibility
		var frame = root.AddComponent<ArtworkFrame>();
		frame.Init(slot.GameObject.transform, wallWorldDir, bs, imageUrl, canvasGO.GetComponent<Renderer>());
	}

	// Creates a primitive child; collider removed.
	private void MakePart(GameObject parent, string name,
		Vector3 localPos, Vector3 localScale, Material mat) {

		var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
		go.name = name;
		go.transform.SetParent(parent.transform);
		go.transform.localPosition = localPos;
		go.transform.localScale    = localScale;
		UnityEngine.Object.Destroy(go.GetComponent<Collider>());
		go.GetComponent<Renderer>().material = mat;
	}

	// ── AIC API ─────────────────────────────────────────────────────────────

	[Serializable] private class AICResp { public AICItem[] data; }
	[Serializable] private class AICItem  { public string image_id; public string title; }

	private IEnumerator FetchArtworkUrls() {
		const string Api = "https://api.artic.edu/api/v1/artworks"
			+ "?fields=id,title,image_id&limit=100&is_public_domain=true";

		var req = UnityWebRequest.Get(Api);
		req.SetRequestHeader("Accept", "application/json");
		yield return req.SendWebRequest();

		if (!req.isNetworkError && !req.isHttpError) {
			var resp = JsonUtility.FromJson<AICResp>(req.downloadHandler.text);
			if (resp?.data != null) {
				foreach (var item in resp.data) {
					if (!string.IsNullOrEmpty(item.image_id)) {
						this.imageUrls.Add(
							$"https://www.artic.edu/iiif/2/{item.image_id}/full/400,/0/default.jpg");
					}
				}
			}
		}
		req.Dispose();
		this.urlsFetched = true;
	}
}
