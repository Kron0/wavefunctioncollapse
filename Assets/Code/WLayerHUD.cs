using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WLayerHUD : MonoBehaviour {
	public FourDController player;

	public static bool StartupComplete { get; private set; }

	// ─── Canvas elements ──────────────────────────────────────────────────────
	private CanvasGroup startupFadeGroup;
	private Image transitionFlashImage;
	private Image accentBar;
	private TextMeshProUGUI bigNumber;
	private TextMeshProUGUI subLabel;
	private Image[] layerDots;
	private Image progressFill;
	private TextMeshProUGUI artifactText;
	private Image artifactFill;
	private TextMeshProUGUI landmarkText;
	private CanvasGroup landmarkGroup;

	// ─── State ───────────────────────────────────────────────────────────────
	private GenerateMap4DNearPlayer generator;
	private bool fadeStarted;
	private float fadeAlpha = 1f;
	private int previousWLayer;
	private float flashAlpha;
	private Color flashColor = new Color(0f, 0.8f, 1f);
	private int previousArtifactCount;

	// =========================================================================
	// LIFECYCLE
	// =========================================================================

	void Awake() {
		StartupComplete = false;
		this.BuildCanvas();
	}

	void Start() {
		this.generator = Object.FindObjectOfType<GenerateMap4DNearPlayer>();
		if (this.player == null) {
			var go = GameObject.FindGameObjectWithTag("Player");
			if (go != null) this.player = go.GetComponent<FourDController>();
		}
		this.previousWLayer = MapBehaviour4D.ActiveWLayer;
		MapBehaviour4D.OnWLayerChanged += this.HandleWLayerChanged;
	}

	void OnDestroy() {
		MapBehaviour4D.OnWLayerChanged -= this.HandleWLayerChanged;
	}

	private void HandleWLayerChanged(int newLayer) {
		bool goingUp = newLayer > this.previousWLayer;
		this.flashColor = goingUp ? new Color(0f, 0.8f, 1f) : new Color(1f, 0.2f, 0.8f);
		this.flashAlpha = 0.45f;

		// Gate passage sound — one sweep per transition, heard at camera
		var gateClip = CollectibleAudio.GetGateSound(goingUp ? 1 : -1);
		var listenPos = Camera.main != null ? Camera.main.transform.position : this.transform.position;
		AudioSource.PlayClipAtPoint(gateClip, listenPos, 0.55f);

		this.previousWLayer = newLayer;
		if (this.bigNumber != null) {
			StartCoroutine(this.PunchTransform(this.bigNumber.transform, 1.35f, 0.18f));
		}
	}

	// =========================================================================
	// UPDATE
	// =========================================================================

	void Update() {
		// Startup fade — wait for first chunk then fade over 1.2s
		if (this.fadeAlpha > 0f) {
			if (!this.fadeStarted) {
				if ((this.generator != null && this.generator.GeneratedChunks.Length > 0) || Time.time > 6f) {
					this.fadeStarted = true;
				}
			} else {
				this.fadeAlpha = Mathf.Max(0f, this.fadeAlpha - Time.deltaTime / 1.2f);
				if (this.startupFadeGroup != null) this.startupFadeGroup.alpha = this.fadeAlpha;
				if (this.fadeAlpha <= 0f) StartupComplete = true;
			}
		}

		// W-layer transition flash
		if (this.flashAlpha > 0f) {
			this.flashAlpha = Mathf.Max(0f, this.flashAlpha - Time.deltaTime / 0.3f);
		}
		if (this.transitionFlashImage != null) {
			this.transitionFlashImage.color = new Color(
				this.flashColor.r, this.flashColor.g, this.flashColor.b, this.flashAlpha);
		}

		if (this.player == null) return;

		float wPos  = this.player.WPosition;
		int   layer = MapBehaviour4D.ActiveWLayer;
		float frac  = wPos - Mathf.Floor(wPos);
		Color accent = DimensionColors.ForLayer(layer);
		int numLayers = DimensionColors.LayerAccents.Length;

		if (this.accentBar != null) this.accentBar.color = accent;

		if (this.bigNumber != null) this.bigNumber.text = $"W{layer}";

		if (this.subLabel != null) this.subLabel.text = wPos.ToString("+0.0;-0.0;0.0");

		// Layer dots
		if (this.layerDots != null) {
			int activeDot = ((layer % numLayers) + numLayers) % numLayers;
			for (int i = 0; i < this.layerDots.Length; i++) {
				if (this.layerDots[i] == null) continue;
				Color dc = DimensionColors.ForLayer(i);
				this.layerDots[i].color = (i == activeDot)
					? new Color(dc.r, dc.g, dc.b, 1f)
					: new Color(dc.r * 0.25f, dc.g * 0.25f, dc.b * 0.25f, 0.45f);
			}
		}

		// W-fraction progress bar
		if (this.progressFill != null) {
			this.progressFill.fillAmount = frac;
			this.progressFill.color = new Color(accent.r, accent.g, accent.b, 0.85f);
		}

		// Artifacts
		int found = CollectiblePlacer.TotalCollected;
		int total = CollectiblePlacer.TotalPlaced;
		if (this.artifactText != null) this.artifactText.text = $"{found} / {total}";
		if (this.artifactFill != null) {
			this.artifactFill.fillAmount = total > 0 ? (float)found / total : 0f;
		}
		if (found > this.previousArtifactCount) {
			this.previousArtifactCount = found;
			if (this.artifactText != null) {
				StartCoroutine(this.PunchTransform(this.artifactText.transform, 1.3f, 0.15f));
			}
		}

		// Landmark indicator
		if (this.landmarkGroup != null) {
			bool hasLandmarks = LandmarkPlacer.LandmarkCount > 0 && Camera.main != null;
			this.landmarkGroup.alpha = Mathf.MoveTowards(
				this.landmarkGroup.alpha, hasLandmarks ? 1f : 0f, Time.deltaTime * 3f);

			if (hasLandmarks && this.landmarkText != null) {
				Vector3 nearest = NearestLandmark(this.player.transform.position, out float dist);
				Vector3 vp = Camera.main.WorldToViewportPoint(nearest);
				bool onScreen = vp.z > 0f && vp.x > 0.05f && vp.x < 0.95f && vp.y > 0.05f && vp.y < 0.95f;
				string bearing = onScreen ? "●" : ViewportArrow(vp);
				this.landmarkText.text = $"BEACON  {bearing}  {dist:F0}m";
				this.landmarkText.color = new Color(accent.r, accent.g, accent.b, 0.85f);
			}
		}
	}

	// =========================================================================
	// ANIMATIONS
	// =========================================================================

	private IEnumerator PunchTransform(Transform t, float targetScale, float duration) {
		float half = duration * 0.5f;
		for (float e = 0f; e < half; e += Time.deltaTime) {
			t.localScale = Vector3.one * Mathf.Lerp(1f, targetScale, e / half);
			yield return null;
		}
		for (float e = 0f; e < half; e += Time.deltaTime) {
			t.localScale = Vector3.one * Mathf.Lerp(targetScale, 1f, e / half);
			yield return null;
		}
		t.localScale = Vector3.one;
	}

	// =========================================================================
	// CANVAS CONSTRUCTION
	// =========================================================================

	private void BuildCanvas() {
		var cvGO = new GameObject("HUD_Canvas");
		cvGO.transform.SetParent(this.transform, false);
		var cv = cvGO.AddComponent<Canvas>();
		cv.renderMode = RenderMode.ScreenSpaceOverlay;
		cv.sortingOrder = 50;
		var scaler = cvGO.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.matchWidthOrHeight = 0.5f;
		cvGO.AddComponent<GraphicRaycaster>();

		// Startup fade (fullscreen black, fades out)
		var fadeGO = Rect("StartupFade", cvGO.transform);
		Stretch(fadeGO);
		this.startupFadeGroup = fadeGO.AddComponent<CanvasGroup>();
		this.startupFadeGroup.blocksRaycasts = false;
		var fadeImg = fadeGO.AddComponent<Image>();
		fadeImg.color = Color.black;
		fadeImg.raycastTarget = false;

		// W-layer transition flash (fullscreen, tinted)
		var flashGO = Rect("TransitionFlash", cvGO.transform);
		Stretch(flashGO);
		this.transitionFlashImage = flashGO.AddComponent<Image>();
		this.transitionFlashImage.color = Color.clear;
		this.transitionFlashImage.raycastTarget = false;

		this.BuildWPanel(cvGO.transform);
	}

	private void BuildWPanel(Transform canvasT) {
		// ── Panel root ────────────────────────────────────────────────────────
		var panelGO = Rect("WPanel", canvasT);
		var panelRT = panelGO.GetComponent<RectTransform>();
		panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(1f, 0f);
		panelRT.anchoredPosition = new Vector2(-20f, 20f);
		panelRT.sizeDelta = new Vector2(260f, 234f);
		var panelBG = panelGO.AddComponent<Image>();
		panelBG.color = new Color(0.04f, 0.04f, 0.07f, 0.90f);
		panelBG.raycastTarget = false;

		// Thin accent bar across the top edge
		var abGO = Rect("AccentBar", panelGO.transform);
		var abRT = abGO.GetComponent<RectTransform>();
		abRT.anchorMin = new Vector2(0f, 1f);
		abRT.anchorMax = new Vector2(1f, 1f);
		abRT.pivot = new Vector2(0.5f, 1f);
		abRT.anchoredPosition = Vector2.zero;
		abRT.sizeDelta = new Vector2(0f, 3f);
		this.accentBar = abGO.AddComponent<Image>();

		// Vertical layout group stacks children top-to-bottom
		var vlg = panelGO.AddComponent<VerticalLayoutGroup>();
		vlg.padding = new RectOffset(18, 18, 14, 14);
		vlg.spacing = 5f;
		vlg.childControlWidth = true;
		vlg.childControlHeight = false;
		vlg.childForceExpandWidth = true;
		vlg.childForceExpandHeight = false;

		// Header label
		var hdr = TMP("Header", panelGO.transform, 9f);
		hdr.text = "PERSONAL DIMENSION";
		hdr.color = new Color(1f, 1f, 1f, 0.35f);
		hdr.fontStyle = FontStyles.Bold;
		hdr.characterSpacing = 3f;
		LE(hdr.gameObject, 14f);

		// Big W layer number — punches on layer change
		this.bigNumber = TMP("WNumber", panelGO.transform, 58f);
		this.bigNumber.text = "W0";
		this.bigNumber.fontStyle = FontStyles.Bold;
		LE(this.bigNumber.gameObject, 72f);

		// Decimal W position sub-label
		this.subLabel = TMP("WSubLabel", panelGO.transform, 12f);
		this.subLabel.color = new Color(1f, 1f, 1f, 0.5f);
		LE(this.subLabel.gameObject, 16f);

		// Layer dots (one per W layer, horizontal row)
		var dotsGO = Rect("LayerDots", panelGO.transform);
		var hlg = dotsGO.AddComponent<HorizontalLayoutGroup>();
		hlg.spacing = 5f;
		hlg.childControlWidth = false;
		hlg.childControlHeight = false;
		hlg.childForceExpandWidth = false;
		hlg.childForceExpandHeight = false;
		LE(dotsGO, 10f);

		this.layerDots = new Image[DimensionColors.LayerAccents.Length];
		for (int i = 0; i < this.layerDots.Length; i++) {
			var dGO = Rect($"Dot{i}", dotsGO.transform);
			dGO.GetComponent<RectTransform>().sizeDelta = new Vector2(16f, 10f);
			this.layerDots[i] = dGO.AddComponent<Image>();
		}

		// W-fraction progress bar (how far between integer layers)
		var pbGO = Rect("ProgressBG", panelGO.transform);
		pbGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
		LE(pbGO, 4f);
		var pfGO = Rect("ProgressFill", pbGO.transform);
		Stretch(pfGO);
		this.progressFill = pfGO.AddComponent<Image>();
		this.progressFill.type = Image.Type.Filled;
		this.progressFill.fillMethod = Image.FillMethod.Horizontal;
		this.progressFill.fillOrigin = 0;

		// Divider
		var divGO = Rect("Divider", panelGO.transform);
		divGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.10f);
		LE(divGO, 1f);

		// Artifact row: label on left, count on right
		var arGO = Rect("ArtifactRow", panelGO.transform);
		var arHLG = arGO.AddComponent<HorizontalLayoutGroup>();
		arHLG.childControlWidth = true;
		arHLG.childControlHeight = false;
		arHLG.childForceExpandWidth = false;
		arHLG.childForceExpandHeight = false;
		LE(arGO, 16f);

		var arLbl = TMP("ArtLabel", arGO.transform, 9f);
		arLbl.text = "ARTIFACTS";
		arLbl.color = new Color(1f, 1f, 1f, 0.38f);
		arLbl.fontStyle = FontStyles.Bold;
		arLbl.characterSpacing = 2f;
		var arLblLE = arLbl.gameObject.AddComponent<LayoutElement>();
		arLblLE.flexibleWidth = 1f;
		arLblLE.preferredHeight = 16f;

		this.artifactText = TMP("ArtCount", arGO.transform, 11f);
		this.artifactText.alignment = TextAlignmentOptions.Right;
		this.artifactText.fontStyle = FontStyles.Bold;
		this.artifactText.color = DimensionColors.ArtifactGold;
		var artCountLE = this.artifactText.gameObject.AddComponent<LayoutElement>();
		artCountLE.preferredWidth = 55f;
		artCountLE.preferredHeight = 16f;

		// Artifact fill bar
		var abBarGO = Rect("ArtifactBG", panelGO.transform);
		abBarGO.AddComponent<Image>().color = new Color(1f, 0.85f, 0.25f, 0.12f);
		LE(abBarGO, 3f);
		var afGO = Rect("ArtifactFill", abBarGO.transform);
		Stretch(afGO);
		this.artifactFill = afGO.AddComponent<Image>();
		this.artifactFill.type = Image.Type.Filled;
		this.artifactFill.fillMethod = Image.FillMethod.Horizontal;
		this.artifactFill.fillOrigin = 0;
		this.artifactFill.color = DimensionColors.ArtifactGold;

		// Landmark row (fades in when beacons exist)
		var lmGO = Rect("LandmarkRow", panelGO.transform);
		LE(lmGO, 16f);
		this.landmarkGroup = lmGO.AddComponent<CanvasGroup>();
		this.landmarkGroup.alpha = 0f;
		this.landmarkGroup.blocksRaycasts = false;
		this.landmarkText = TMP("LandmarkText", lmGO.transform, 9f);
		this.landmarkText.color = new Color(1f, 1f, 1f, 0.75f);
		Stretch(this.landmarkText.gameObject);
	}

	// ─── Construction helpers ─────────────────────────────────────────────────

	private static GameObject Rect(string name, Transform parent) {
		var go = new GameObject(name, typeof(RectTransform));
		go.transform.SetParent(parent, false);
		return go;
	}

	private static TextMeshProUGUI TMP(string name, Transform parent, float size) {
		var go = Rect(name, parent);
		var t = go.AddComponent<TextMeshProUGUI>();
		t.fontSize = size;
		t.raycastTarget = false;
		return t;
	}

	private static void Stretch(GameObject go) {
		var rt = go.GetComponent<RectTransform>();
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.offsetMin = rt.offsetMax = Vector2.zero;
	}

	private static void LE(GameObject go, float height) {
		go.AddComponent<LayoutElement>().preferredHeight = height;
	}

	// ─── Runtime helpers ──────────────────────────────────────────────────────

	private static Vector3 NearestLandmark(Vector3 from, out float dist) {
		var positions = LandmarkPlacer.LandmarkWorldPositions;
		Vector3 nearest = positions[0];
		dist = float.MaxValue;
		foreach (var p in positions) {
			float d = Vector3.Distance(from, p);
			if (d < dist) { dist = d; nearest = p; }
		}
		return nearest;
	}

	private static string ViewportArrow(Vector3 viewport) {
		Vector2 dir = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
		if (viewport.z <= 0f) dir = -dir;
		float angle = (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 360f) % 360f;
		int sector = Mathf.RoundToInt(angle / 45f) % 8;
		return new[] { "→", "↗", "↑", "↖", "←", "↙", "↓", "↘" }[sector];
	}
}
