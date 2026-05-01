using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class PauseMenu : MonoBehaviour {

	public static bool IsPaused { get; private set; }

	private static readonly string[] Sections = { "GAME", "DISPLAY", "AUDIO", "CONTROLS", "GAMEPLAY" };
	private static readonly Color    Accent    = new Color(0.00f, 0.78f, 1.00f);
	private const float DIM_ALPHA = 0.78f;

	// ─── Canvas references ────────────────────────────────────────────────────
	private Canvas         pauseCanvas;
	private CanvasGroup    dimGroup;
	private RectTransform  panelRT;
	private CanvasGroup    panelGroup;

	// ─── Nav ──────────────────────────────────────────────────────────────────
	private RectTransform[]      navRTs;
	private Image[]              navBGs;
	private Image[]              navBars;
	private Image[]              navDots;
	private TextMeshProUGUI[]    navLabels;
	private float[]              navHovers;
	private int                  selectedSection;

	private RectTransform resumeRT;
	private Image         resumeBG;
	private float         resumeHoverT;

	private RectTransform quitRT;
	private Image         quitBG;
	private float         quitHoverT;

	// ─── Content ──────────────────────────────────────────────────────────────
	private CanvasGroup[] sectionGroups;
	private Coroutine     sectionAnim;

	// ─── Game section ─────────────────────────────────────────────────────────
	private Slider          sensSlider;
	private TextMeshProUGUI sensValueLabel;
	private float           mouseSensitivity = 3f;

	// ─── State ───────────────────────────────────────────────────────────────
	private FourDController    player;
	private Coroutine          activeAnim;
	private static TMP_FontAsset tmpFont;

	// =========================================================================
	// LIFECYCLE
	// =========================================================================

	void Awake() {
		IsPaused = false;
		GameState.IsPaused = false;
		this.BuildCanvas();
	}

	void Start() {
		this.player = this.GetComponent<FourDController>();
		if (this.player == null) this.player = Object.FindObjectOfType<FourDController>();
		if (this.player != null) this.mouseSensitivity = this.player.MouseSensitivity;
		if (this.sensSlider     != null) this.sensSlider.value   = this.mouseSensitivity;
		if (this.sensValueLabel != null) this.sensValueLabel.text = $"{this.mouseSensitivity:F1}";
	}

	void Update() {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			if (IsPaused) this.DoResume();
			else          this.DoPause();
		}
		if (!IsPaused) return;

		this.UpdateHovers();
	}

	// =========================================================================
	// PAUSE / RESUME
	// =========================================================================

	private void DoPause() {
		IsPaused = true;
		GameState.IsPaused = true;
		Time.timeScale = 0f;
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible   = true;
		this.pauseCanvas.enabled = true;
		if (this.activeAnim != null) StopCoroutine(this.activeAnim);
		this.activeAnim = StartCoroutine(this.AnimateIn());
	}

	private void DoResume() {
		if (this.activeAnim != null) StopCoroutine(this.activeAnim);
		this.activeAnim = StartCoroutine(this.AnimateOut());
	}

	// =========================================================================
	// ANIMATIONS  (all use Time.unscaledDeltaTime — timeScale is 0 when paused)
	// =========================================================================

	private IEnumerator AnimateIn() {
		float dur = 0.22f;
		for (float e = 0f; e < dur; e += Time.unscaledDeltaTime) {
			float t    = e / dur;
			float ease = 1f - (1f - t) * (1f - t) * (1f - t); // cubic ease-out
			this.dimGroup.alpha   = ease * DIM_ALPHA;
			this.panelGroup.alpha = ease;
			float s = Mathf.Lerp(0.93f, 1f, ease);
			this.panelRT.localScale = new Vector3(s, s, 1f);
			yield return null;
		}
		this.dimGroup.alpha     = DIM_ALPHA;
		this.panelGroup.alpha   = 1f;
		this.panelRT.localScale = Vector3.one;
		this.activeAnim = null;
	}

	private IEnumerator AnimateOut() {
		float dur      = 0.15f;
		float startDim = this.dimGroup.alpha;
		for (float e = 0f; e < dur; e += Time.unscaledDeltaTime) {
			float t    = e / dur;
			float ease = t * t; // ease-in
			this.dimGroup.alpha   = Mathf.Lerp(startDim, 0f, ease);
			this.panelGroup.alpha = 1f - t;
			float s = Mathf.Lerp(1f, 0.95f, ease);
			this.panelRT.localScale = new Vector3(s, s, 1f);
			yield return null;
		}
		this.dimGroup.alpha      = 0f;
		this.panelGroup.alpha    = 0f;
		this.pauseCanvas.enabled = false;
		IsPaused = false;
		GameState.IsPaused = false;
		Time.timeScale       = 1f;
		Cursor.lockState     = CursorLockMode.Locked;
		Cursor.visible       = false;
		this.activeAnim = null;
	}

	private IEnumerator SwitchSectionAnim(int newIndex) {
		var outG = this.sectionGroups[this.selectedSection];
		float half = 0.08f;

		// Fade out current
		for (float e = 0f; e < half; e += Time.unscaledDeltaTime) {
			outG.alpha = 1f - e / half;
			yield return null;
		}
		outG.alpha          = 0f;
		outG.interactable   = false;
		outG.blocksRaycasts = false;

		this.selectedSection = newIndex;

		// Fade in new
		var inG = this.sectionGroups[newIndex];
		inG.interactable   = true;
		inG.blocksRaycasts = true;
		for (float e = 0f; e < half; e += Time.unscaledDeltaTime) {
			inG.alpha = e / half;
			yield return null;
		}
		inG.alpha       = 1f;
		this.sectionAnim = null;
	}

	// =========================================================================
	// HOVER / CLICK
	// =========================================================================

	private void UpdateHovers() {
		float  dt  = Time.unscaledDeltaTime;
		Vector2 mp  = Input.mousePosition;
		Camera  cam = null; // null = screen-space coords for ScreenSpaceOverlay

		// ── Nav section buttons ───────────────────────────────────────────────
		for (int i = 0; i < Sections.Length; i++) {
			bool over   = this.navRTs != null && RectTransformUtility.RectangleContainsScreenPoint(this.navRTs[i], mp, cam);
			bool active = i == this.selectedSection;

			this.navHovers[i] = Mathf.MoveTowards(this.navHovers[i], over ? 1f : 0f, dt * 12f);

			if (this.navBGs != null) {
				Color activeTint = new Color(Accent.r, Accent.g, Accent.b, 0.09f);
				Color hoverTint  = new Color(1f, 1f, 1f, 0.045f * this.navHovers[i]);
				this.navBGs[i].color = active ? activeTint : hoverTint;
			}
			if (this.navBars != null) {
				this.navBars[i].color = new Color(Accent.r, Accent.g, Accent.b, active ? 1f : this.navHovers[i] * 0.25f);
			}
			if (this.navLabels != null) {
				float a = active ? 1f : Mathf.Lerp(0.45f, 0.75f, this.navHovers[i]);
				this.navLabels[i].color = new Color(1f, 1f, 1f, a);
			}
			if (this.navDots != null) {
				this.navDots[i].color = active ? Accent : new Color(1f, 1f, 1f, 0.2f);
			}

			if (over && !active && Input.GetMouseButtonDown(0)) {
				if (this.sectionAnim != null) StopCoroutine(this.sectionAnim);
				this.sectionAnim = StartCoroutine(this.SwitchSectionAnim(i));
			}
		}

		// ── Resume button ─────────────────────────────────────────────────────
		bool overResume = this.resumeRT != null && RectTransformUtility.RectangleContainsScreenPoint(this.resumeRT, mp, cam);
		this.resumeHoverT = Mathf.MoveTowards(this.resumeHoverT, overResume ? 1f : 0f, dt * 12f);
		if (this.resumeBG != null) {
			this.resumeBG.color = Color.Lerp(
				new Color(0.05f, 0.12f, 0.18f, 0.65f),
				new Color(0.08f, 0.20f, 0.28f, 0.95f), this.resumeHoverT);
		}
		if (overResume && Input.GetMouseButtonDown(0)) this.DoResume();

		// ── Quit button ───────────────────────────────────────────────────────
		bool overQuit = this.quitRT != null && RectTransformUtility.RectangleContainsScreenPoint(this.quitRT, mp, cam);
		this.quitHoverT = Mathf.MoveTowards(this.quitHoverT, overQuit ? 1f : 0f, dt * 12f);
		if (this.quitBG != null) {
			this.quitBG.color = Color.Lerp(
				new Color(0.35f, 0.05f, 0.05f, 0.65f),
				new Color(0.55f, 0.08f, 0.08f, 0.95f), this.quitHoverT);
		}
		if (overQuit && Input.GetMouseButtonDown(0)) {
			Application.Quit();
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#endif
		}
	}

	// =========================================================================
	// CANVAS CONSTRUCTION
	// =========================================================================

	private void BuildCanvas() {
		// Ensure EventSystem exists for Slider interaction
		if (Object.FindObjectOfType<EventSystem>() == null) {
			var esGO = new GameObject("EventSystem");
			esGO.AddComponent<EventSystem>();
			try { esGO.AddComponent<StandaloneInputModule>(); }
			catch (System.Exception) { /* Submit/Cancel axes not configured — EventSystem works without an input module for mouse */ }
		}

		var cvGO = new GameObject("PauseCanvas");
		cvGO.transform.SetParent(this.transform, false);
		this.pauseCanvas = cvGO.AddComponent<Canvas>();
		this.pauseCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
		this.pauseCanvas.sortingOrder = 60;
		var scaler = cvGO.AddComponent<CanvasScaler>();
		scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.matchWidthOrHeight  = 0.5f;
		cvGO.AddComponent<GraphicRaycaster>();

		// ── Fullscreen dim ────────────────────────────────────────────────────
		var dimGO = R("Dim", cvGO.transform);
		Stretch(dimGO);
		this.dimGroup = dimGO.AddComponent<CanvasGroup>();
		this.dimGroup.alpha          = 0f;
		this.dimGroup.blocksRaycasts = true;
		var dimImg = dimGO.AddComponent<Image>();
		dimImg.color = new Color(0f, 0f, 0.04f, 0.78f);

		// ── Panel ─────────────────────────────────────────────────────────────
		var panelGO = R("Panel", cvGO.transform);
		this.panelRT               = panelGO.GetComponent<RectTransform>();
		this.panelRT.anchorMin     = this.panelRT.anchorMax = this.panelRT.pivot = new Vector2(0.5f, 0.5f);
		this.panelRT.sizeDelta     = new Vector2(760f, 580f);
		this.panelRT.localScale    = new Vector3(0.93f, 0.93f, 1f);
		this.panelGroup            = panelGO.AddComponent<CanvasGroup>();
		this.panelGroup.alpha      = 0f;
		var panelBG                = panelGO.AddComponent<Image>();
		panelBG.color              = new Color(0.03f, 0.03f, 0.06f, 0.97f);
		panelBG.raycastTarget      = false;

		this.AddBorders(panelGO.transform);
		this.AddCornerBrackets(panelGO.transform);

		// ── Header ────────────────────────────────────────────────────────────
		const float headerH = 72f;
		var hdrGO = R("Header", panelGO.transform);
		var hdrRT = hdrGO.GetComponent<RectTransform>();
		hdrRT.anchorMin        = new Vector2(0f, 1f); hdrRT.anchorMax = new Vector2(1f, 1f);
		hdrRT.pivot            = new Vector2(0.5f, 1f);
		hdrRT.anchoredPosition = Vector2.zero;
		hdrRT.sizeDelta        = new Vector2(0f, headerH);

		// Accent pip
		var pip = R("Pip", hdrGO.transform);
		var pipRT = pip.GetComponent<RectTransform>();
		pipRT.anchorMin = pipRT.anchorMax = new Vector2(0f, 0.5f);
		pipRT.pivot = new Vector2(0f, 0.5f);
		pipRT.anchoredPosition = new Vector2(28f, 0f);
		pipRT.sizeDelta = new Vector2(4f, 16f);
		pip.AddComponent<Image>().color = Accent;

		// Small secondary pip
		var pip2 = R("Pip2", hdrGO.transform);
		var pip2RT = pip2.GetComponent<RectTransform>();
		pip2RT.anchorMin = pip2RT.anchorMax = new Vector2(0f, 0.5f);
		pip2RT.pivot = new Vector2(0f, 0.5f);
		pip2RT.anchoredPosition = new Vector2(34f, 0f);
		pip2RT.sizeDelta = new Vector2(3f, 10f);
		pip2.AddComponent<Image>().color = new Color(Accent.r, Accent.g, Accent.b, 0.5f);

		// Title
		var title = TMP("Title", hdrGO.transform, 30f);
		title.text      = "PAUSED";
		title.fontStyle = FontStyles.Bold;
		title.color     = Color.white;
		title.alignment = TextAlignmentOptions.BottomLeft;
		var titleRT = title.GetComponent<RectTransform>();
		titleRT.anchorMin = Vector2.zero; titleRT.anchorMax = Vector2.one;
		titleRT.offsetMin = new Vector2(46f, headerH * 0.46f); titleRT.offsetMax = new Vector2(-28f, -8f);

		// Subtitle
		var sub = TMP("Sub", hdrGO.transform, 10f);
		sub.text             = "PERSONAL DIMENSION";
		sub.fontStyle        = FontStyles.Bold;
		sub.color            = new Color(Accent.r, Accent.g, Accent.b, 0.65f);
		sub.characterSpacing = 2f;
		sub.alignment        = TextAlignmentOptions.TopLeft;
		var subRT = sub.GetComponent<RectTransform>();
		subRT.anchorMin = Vector2.zero; subRT.anchorMax = Vector2.one;
		subRT.offsetMin = new Vector2(46f, 4f); subRT.offsetMax = new Vector2(-28f, -headerH * 0.54f);

		// Header bottom border
		var hLine = R("HdrLine", hdrGO.transform);
		var hLineRT = hLine.GetComponent<RectTransform>();
		hLineRT.anchorMin = new Vector2(0f, 0f); hLineRT.anchorMax = new Vector2(1f, 0f);
		hLineRT.pivot = new Vector2(0.5f, 0f); hLineRT.anchoredPosition = Vector2.zero;
		hLineRT.sizeDelta = new Vector2(0f, 1f);
		hLine.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

		// ── Body ──────────────────────────────────────────────────────────────
		const float navW = 200f;

		var bodyGO = R("Body", panelGO.transform);
		var bodyRT = bodyGO.GetComponent<RectTransform>();
		bodyRT.anchorMin = Vector2.zero; bodyRT.anchorMax = Vector2.one;
		bodyRT.offsetMin = Vector2.zero; bodyRT.offsetMax = new Vector2(0f, -headerH);

		this.BuildNavColumn(bodyGO.transform, navW);

		var divGO = R("Div", bodyGO.transform);
		var divRT = divGO.GetComponent<RectTransform>();
		divRT.anchorMin = new Vector2(0f, 0f); divRT.anchorMax = new Vector2(0f, 1f);
		divRT.pivot = new Vector2(0f, 0.5f);
		divRT.anchoredPosition = new Vector2(navW, 0f);
		divRT.sizeDelta = new Vector2(1f, 0f);
		divGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);

		this.BuildContentColumn(bodyGO.transform, navW + 1f);

		this.pauseCanvas.enabled = false;
	}

	// ─── Nav column ──────────────────────────────────────────────────────────

	private void BuildNavColumn(Transform parent, float width) {
		var col = R("NavCol", parent);
		var colRT = col.GetComponent<RectTransform>();
		colRT.anchorMin = new Vector2(0f, 0f); colRT.anchorMax = new Vector2(0f, 1f);
		colRT.pivot     = new Vector2(0f, 0.5f);
		colRT.anchoredPosition = Vector2.zero;
		colRT.sizeDelta = new Vector2(width, 0f);

		const float itemH  = 44f;
		const float itemGap = 4f;
		const float padTop = 22f;
		const float padBot = 22f;
		const float btnH   = 44f;
		const float btnGap = 8f;

		this.navRTs    = new RectTransform[Sections.Length];
		this.navBGs    = new Image[Sections.Length];
		this.navBars   = new Image[Sections.Length];
		this.navDots   = new Image[Sections.Length];
		this.navLabels = new TextMeshProUGUI[Sections.Length];
		this.navHovers = new float[Sections.Length];

		float y = -padTop;
		for (int i = 0; i < Sections.Length; i++) {
			var item = R("NavItem_" + i, col.transform);
			var rt = item.GetComponent<RectTransform>();
			rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
			rt.pivot = new Vector2(0.5f, 1f);
			rt.anchoredPosition = new Vector2(0f, y);
			rt.sizeDelta = new Vector2(0f, itemH);
			this.navRTs[i] = rt;

			this.navBGs[i] = item.AddComponent<Image>();
			this.navBGs[i].color = Color.clear;

			var bar = R("Bar", item.transform);
			var barRT = bar.GetComponent<RectTransform>();
			barRT.anchorMin = new Vector2(0f, 0f); barRT.anchorMax = new Vector2(0f, 1f);
			barRT.pivot = new Vector2(0f, 0.5f);
			barRT.anchoredPosition = Vector2.zero; barRT.sizeDelta = new Vector2(3f, 0f);
			this.navBars[i] = bar.AddComponent<Image>();
			this.navBars[i].color = Color.clear;
			this.navBars[i].raycastTarget = false;

			var dot = R("Dot", item.transform);
			var dotRT = dot.GetComponent<RectTransform>();
			dotRT.anchorMin = dotRT.anchorMax = new Vector2(0f, 0.5f);
			dotRT.pivot = new Vector2(0f, 0.5f);
			dotRT.anchoredPosition = new Vector2(18f, 0f); dotRT.sizeDelta = new Vector2(6f, 6f);
			this.navDots[i] = dot.AddComponent<Image>();
			this.navDots[i].color = new Color(1f, 1f, 1f, 0.2f);
			this.navDots[i].raycastTarget = false;

			this.navLabels[i] = TMP("Lbl", item.transform, 10f);
			this.navLabels[i].text      = Sections[i];
			this.navLabels[i].fontStyle = FontStyles.Bold;
			this.navLabels[i].color     = new Color(1f, 1f, 1f, 0.45f);
			this.navLabels[i].alignment = TextAlignmentOptions.Left;
			this.navLabels[i].raycastTarget = false;
			var lblRT = this.navLabels[i].GetComponent<RectTransform>();
			lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
			lblRT.offsetMin = new Vector2(32f, 0f); lblRT.offsetMax = Vector2.zero;

			y -= itemH + itemGap;
		}

		// Bottom action buttons
		float resumeY = padBot + btnH + btnGap;
		float quitY   = padBot;
		this.resumeRT = this.NavActionButton(col.transform, "RESUME", false, resumeY, btnH, ref this.resumeBG);
		this.quitRT   = this.NavActionButton(col.transform, "QUIT",   true,  quitY,   btnH, ref this.quitBG);
	}

	private RectTransform NavActionButton(Transform parent, string label, bool danger,
		float bottomY, float height, ref Image bgOut) {

		var go = R("Btn_" + label, parent);
		var rt = go.GetComponent<RectTransform>();
		rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
		rt.pivot = new Vector2(0.5f, 0f);
		rt.anchoredPosition = new Vector2(0f, bottomY); rt.sizeDelta = new Vector2(0f, height);

		bgOut = go.AddComponent<Image>();
		bgOut.color = danger ? new Color(0.35f, 0.05f, 0.05f, 0.65f) : new Color(0.05f, 0.12f, 0.18f, 0.65f);

		// Top accent line
		var topLine = R("Line", go.transform);
		var lineRT  = topLine.GetComponent<RectTransform>();
		lineRT.anchorMin = new Vector2(0f, 1f); lineRT.anchorMax = new Vector2(1f, 1f);
		lineRT.pivot = new Vector2(0.5f, 1f); lineRT.anchoredPosition = Vector2.zero;
		lineRT.sizeDelta = new Vector2(0f, 1f);
		topLine.AddComponent<Image>().color = danger
			? new Color(1f, 0.28f, 0.28f, 0.45f)
			: new Color(Accent.r, Accent.g, Accent.b, 0.4f);

		var lbl = TMP("Lbl", go.transform, 10f);
		lbl.text      = label;
		lbl.fontStyle = FontStyles.Bold;
		lbl.color     = Color.white;
		lbl.alignment = TextAlignmentOptions.Center;
		Stretch(lbl.gameObject);

		return rt;
	}

	// ─── Content column ───────────────────────────────────────────────────────

	private void BuildContentColumn(Transform parent, float leftX) {
		var col = R("Content", parent);
		var colRT = col.GetComponent<RectTransform>();
		colRT.anchorMin = new Vector2(0f, 0f); colRT.anchorMax = new Vector2(1f, 1f);
		colRT.offsetMin = new Vector2(leftX, 0f); colRT.offsetMax = Vector2.zero;

		this.sectionGroups = new CanvasGroup[Sections.Length];
		for (int i = 0; i < Sections.Length; i++) {
			var sec   = R("Sec_" + i, col.transform);
			Stretch(sec);
			var cg    = sec.AddComponent<CanvasGroup>();
			cg.alpha          = i == 0 ? 1f : 0f;
			cg.interactable   = i == 0;
			cg.blocksRaycasts = i == 0;
			this.sectionGroups[i] = cg;

			if (i == 0) this.BuildGameSection(sec.transform);
			else        this.BuildPlaceholderSection(sec.transform, Sections[i]);
		}
	}

	private void BuildGameSection(Transform parent) {
		const float pad = 30f;

		// Section header
		var hdr = TMP("Hdr", parent, 11f);
		hdr.text             = "GAME";
		hdr.fontStyle        = FontStyles.Bold;
		hdr.color            = new Color(Accent.r, Accent.g, Accent.b, 0.7f);
		hdr.characterSpacing = 3f;
		hdr.alignment        = TextAlignmentOptions.Left;
		var hdrRT = hdr.GetComponent<RectTransform>();
		hdrRT.anchorMin = new Vector2(0f, 1f); hdrRT.anchorMax = new Vector2(1f, 1f);
		hdrRT.pivot = new Vector2(0.5f, 1f);
		hdrRT.anchoredPosition = new Vector2(0f, -pad); hdrRT.sizeDelta = new Vector2(-pad * 2f, 16f);

		// Divider under header
		var divGO = R("Div", parent);
		var divRT = divGO.GetComponent<RectTransform>();
		divRT.anchorMin = new Vector2(0f, 1f); divRT.anchorMax = new Vector2(1f, 1f);
		divRT.pivot = new Vector2(0.5f, 1f);
		divRT.anchoredPosition = new Vector2(0f, -pad - 20f); divRT.sizeDelta = new Vector2(-pad * 2f, 1f);
		divGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

		float y = -pad - 56f;

		// Sensitivity slider
		y = this.AddSliderRow(parent, "LOOK SENSITIVITY", y, pad, 0.5f, 10f, this.mouseSensitivity,
			out this.sensValueLabel, out this.sensSlider);
		this.sensSlider.onValueChanged.AddListener(v => {
			this.mouseSensitivity = v;
			if (this.sensValueLabel != null) this.sensValueLabel.text = $"{v:F1}";
			if (this.player != null)         this.player.MouseSensitivity = v;
		});

		y -= 24f;
		this.AddDimRow(parent, "FIELD OF VIEW", "90", y, pad);
		y -= 60f;
		this.AddDimRow(parent, "INVERT Y AXIS", "OFF", y, pad);
	}

	private float AddSliderRow(Transform parent, string label, float y, float pad,
		float min, float max, float initial,
		out TextMeshProUGUI valueOut, out Slider sliderOut) {

		// Label
		var lbl = TMP("SensLbl", parent, 9f);
		lbl.text      = label;
		lbl.fontStyle = FontStyles.Bold;
		lbl.color     = new Color(1f, 1f, 1f, 0.75f);
		lbl.alignment = TextAlignmentOptions.Left;
		var lblRT = lbl.GetComponent<RectTransform>();
		lblRT.anchorMin = new Vector2(0f, 1f); lblRT.anchorMax = new Vector2(0.65f, 1f);
		lblRT.pivot = new Vector2(0f, 1f);
		lblRT.anchoredPosition = new Vector2(pad, y); lblRT.sizeDelta = new Vector2(0f, 20f);

		// Value text
		var val = TMP("SensVal", parent, 9f);
		val.text      = $"{initial:F1}";
		val.fontStyle = FontStyles.Bold;
		val.color     = Color.white;
		val.alignment = TextAlignmentOptions.Right;
		var valRT = val.GetComponent<RectTransform>();
		valRT.anchorMin = new Vector2(0.65f, 1f); valRT.anchorMax = new Vector2(1f, 1f);
		valRT.pivot = new Vector2(1f, 1f);
		valRT.anchoredPosition = new Vector2(-pad, y); valRT.sizeDelta = new Vector2(0f, 20f);
		valueOut = val;

		y -= 24f;

		// Slider container
		var sGO = R("SensSlider", parent);
		var sRT = sGO.GetComponent<RectTransform>();
		sRT.anchorMin = new Vector2(0f, 1f); sRT.anchorMax = new Vector2(1f, 1f);
		sRT.pivot = new Vector2(0.5f, 1f);
		sRT.anchoredPosition = new Vector2(0f, y); sRT.sizeDelta = new Vector2(-pad * 2f, 16f);

		// Track
		var trackGO = R("Track", sGO.transform);
		Stretch(trackGO);
		trackGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

		// Fill area
		var fillArea = R("FillArea", sGO.transform);
		var faRT = fillArea.GetComponent<RectTransform>();
		faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
		faRT.offsetMin = Vector2.zero; faRT.offsetMax = new Vector2(-8f, 0f);

		var fillGO = R("Fill", fillArea.transform);
		Stretch(fillGO);
		var fillImg = fillGO.AddComponent<Image>();
		fillImg.color = new Color(Accent.r, Accent.g, Accent.b, 0.75f);

		// Handle slide area
		var handleArea = R("HandleArea", sGO.transform);
		Stretch(handleArea);

		var handleGO = R("Handle", handleArea.transform);
		var handleRT = handleGO.GetComponent<RectTransform>();
		handleRT.sizeDelta = new Vector2(10f, 24f);
		var handleImg = handleGO.AddComponent<Image>();
		handleImg.color = Accent;

		// Slider component
		var slider = sGO.AddComponent<Slider>();
		slider.minValue     = min;
		slider.maxValue     = max;
		slider.value        = initial;
		slider.fillRect     = fillGO.GetComponent<RectTransform>();
		slider.handleRect   = handleGO.GetComponent<RectTransform>();
		slider.targetGraphic = handleImg;
		slider.direction    = Slider.Direction.LeftToRight;
		slider.transition   = Selectable.Transition.None;
		sliderOut = slider;

		y -= 16f;
		return y;
	}

	private void AddDimRow(Transform parent, string label, string value, float y, float pad) {
		float alpha = 0.25f;

		var lbl = TMP("DimLbl_" + label, parent, 9f);
		lbl.text      = label;
		lbl.fontStyle = FontStyles.Bold;
		lbl.color     = new Color(1f, 1f, 1f, alpha);
		lbl.alignment = TextAlignmentOptions.Left;
		var lblRT = lbl.GetComponent<RectTransform>();
		lblRT.anchorMin = new Vector2(0f, 1f); lblRT.anchorMax = new Vector2(0.65f, 1f);
		lblRT.pivot = new Vector2(0f, 1f);
		lblRT.anchoredPosition = new Vector2(pad, y); lblRT.sizeDelta = new Vector2(0f, 20f);

		var val = TMP("DimVal_" + label, parent, 9f);
		val.text      = value;
		val.fontStyle = FontStyles.Bold;
		val.color     = new Color(1f, 1f, 1f, alpha);
		val.alignment = TextAlignmentOptions.Right;
		var valRT = val.GetComponent<RectTransform>();
		valRT.anchorMin = new Vector2(0.65f, 1f); valRT.anchorMax = new Vector2(1f, 1f);
		valRT.pivot = new Vector2(1f, 1f);
		valRT.anchoredPosition = new Vector2(-pad, y); valRT.sizeDelta = new Vector2(0f, 20f);

		var track = R("DimTrack_" + label, parent);
		var trackRT = track.GetComponent<RectTransform>();
		trackRT.anchorMin = new Vector2(0f, 1f); trackRT.anchorMax = new Vector2(1f, 1f);
		trackRT.pivot = new Vector2(0.5f, 1f);
		trackRT.anchoredPosition = new Vector2(0f, y - 24f); trackRT.sizeDelta = new Vector2(-pad * 2f, 2f);
		track.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
	}

	private void BuildPlaceholderSection(Transform parent, string name) {
		const float pad = 30f;

		var hdr = TMP("Hdr", parent, 11f);
		hdr.text             = name;
		hdr.fontStyle        = FontStyles.Bold;
		hdr.color            = new Color(Accent.r, Accent.g, Accent.b, 0.7f);
		hdr.characterSpacing = 3f;
		hdr.alignment        = TextAlignmentOptions.Left;
		var hdrRT = hdr.GetComponent<RectTransform>();
		hdrRT.anchorMin = new Vector2(0f, 1f); hdrRT.anchorMax = new Vector2(1f, 1f);
		hdrRT.pivot = new Vector2(0.5f, 1f);
		hdrRT.anchoredPosition = new Vector2(0f, -pad); hdrRT.sizeDelta = new Vector2(-pad * 2f, 16f);

		var divGO = R("Div", parent);
		var divRT = divGO.GetComponent<RectTransform>();
		divRT.anchorMin = new Vector2(0f, 1f); divRT.anchorMax = new Vector2(1f, 1f);
		divRT.pivot = new Vector2(0.5f, 1f);
		divRT.anchoredPosition = new Vector2(0f, -pad - 20f); divRT.sizeDelta = new Vector2(-pad * 2f, 1f);
		divGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

		var msg = TMP("Msg", parent, 10f);
		msg.text      = "Options coming soon.";
		msg.color     = new Color(1f, 1f, 1f, 0.25f);
		msg.alignment = TextAlignmentOptions.Left;
		var msgRT = msg.GetComponent<RectTransform>();
		msgRT.anchorMin = new Vector2(0f, 1f); msgRT.anchorMax = new Vector2(1f, 1f);
		msgRT.pivot = new Vector2(0f, 1f);
		msgRT.anchoredPosition = new Vector2(pad, -pad - 80f); msgRT.sizeDelta = new Vector2(-pad * 2f, 20f);
	}

	// ─── Panel decoration ─────────────────────────────────────────────────────

	private void AddBorders(Transform parent) {
		Edge(parent, "BrdTop",   new Vector2(0f,1f),new Vector2(1f,1f),new Vector2(0.5f,1f),new Vector2(0f,2f),   new Color(Accent.r,Accent.g,Accent.b,0.85f));
		Edge(parent, "BrdBot",   new Vector2(0f,0f),new Vector2(1f,0f),new Vector2(0.5f,0f),new Vector2(0f,2f),   new Color(1f,1f,1f,0.06f));
		Edge(parent, "BrdLeft",  new Vector2(0f,0f),new Vector2(0f,1f),new Vector2(0f,0.5f),new Vector2(2f,0f),   new Color(Accent.r,Accent.g,Accent.b,0.22f));
		Edge(parent, "BrdRight", new Vector2(1f,0f),new Vector2(1f,1f),new Vector2(1f,0.5f),new Vector2(2f,0f),   new Color(Accent.r,Accent.g,Accent.b,0.07f));
	}

	private static void Edge(Transform parent, string name,
		Vector2 ancMin, Vector2 ancMax, Vector2 pivot, Vector2 size, Color color) {
		var go = R(name, parent);
		var rt = go.GetComponent<RectTransform>();
		rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.pivot = pivot;
		rt.anchoredPosition = Vector2.zero; rt.sizeDelta = size;
		go.AddComponent<Image>().color = color;
	}

	private void AddCornerBrackets(Transform parent) {
		Color c = new Color(Accent.r, Accent.g, Accent.b, 0.55f);
		const float sz = 16f, t = 2f;
		Bracket(parent, "BrkTL", new Vector2(0f,1f), sz, t, c, false, false);
		Bracket(parent, "BrkTR", new Vector2(1f,1f), sz, t, c, true,  false);
		Bracket(parent, "BrkBL", new Vector2(0f,0f), sz, t, c, false, true);
		Bracket(parent, "BrkBR", new Vector2(1f,0f), sz, t, c, true,  true);
	}

	private static void Bracket(Transform parent, string name, Vector2 corner,
		float sz, float t, Color c, bool flipX, bool flipY) {

		var root = R(name, parent);
		var rt   = root.GetComponent<RectTransform>();
		rt.anchorMin = rt.anchorMax = rt.pivot = corner;
		rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;

		// Horizontal arm
		var h = R("H", root.transform);
		var hRT = h.GetComponent<RectTransform>();
		hRT.anchorMin = hRT.anchorMax = hRT.pivot = corner;
		hRT.sizeDelta = new Vector2(sz, t);
		hRT.anchoredPosition = Vector2.zero;
		h.AddComponent<Image>().color = c;

		// Vertical arm
		var v = R("V", root.transform);
		var vRT = v.GetComponent<RectTransform>();
		vRT.anchorMin = vRT.anchorMax = vRT.pivot = corner;
		vRT.sizeDelta = new Vector2(t, sz);
		vRT.anchoredPosition = Vector2.zero;
		v.AddComponent<Image>().color = c;
	}

	// =========================================================================
	// HELPERS
	// =========================================================================

	private static GameObject R(string name, Transform parent) {
		var go = new GameObject(name, typeof(RectTransform));
		go.transform.SetParent(parent, false);
		return go;
	}

	private static void Stretch(GameObject go) {
		var rt = go.GetComponent<RectTransform>();
		rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
		rt.offsetMin = rt.offsetMax = Vector2.zero;
	}

	private static TextMeshProUGUI TMP(string name, Transform parent, float size) {
		var go = R(name, parent);
		var t  = go.AddComponent<TextMeshProUGUI>();
		t.fontSize       = size;
		t.raycastTarget  = false;
		if (t.font == null) {
			if (tmpFont == null)
				tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
			if (tmpFont != null) t.font = tmpFont;
		}
		return t;
	}
}
