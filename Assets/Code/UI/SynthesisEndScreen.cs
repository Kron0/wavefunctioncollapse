using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Full-screen end-game card shown after the synthesis moment.
// Subscribe to CollectiblePlacer.OnSynthesisComplete to trigger,
// or call CollectiblePlacer.TriggerSynthesis() directly (e.g. F9 in editor).
public class SynthesisEndScreen : MonoBehaviour {

	// ── References ────────────────────────────────────────────────────────────
	private CanvasGroup  rootGroup;
	private Image        accentBar;
	private RectTransform panelRT;
	private float        accentT;
	private bool         revealed;
	private bool         dismissing;

	// ── Lifecycle ─────────────────────────────────────────────────────────────

	void Awake() {
		this.BuildCanvas();
	}

	void Start() {
		CollectiblePlacer.OnSynthesisComplete += this.Reveal;
	}

	void OnDestroy() {
		CollectiblePlacer.OnSynthesisComplete -= this.Reveal;
	}

	void Update() {
		// Slowly cycle accent bar through all six W-layer colours
		if (this.rootGroup != null && this.rootGroup.alpha > 0.01f && this.accentBar != null) {
			this.accentT += Time.deltaTime * 0.12f;
			int   a    = Mathf.FloorToInt(this.accentT) % DimensionColors.LayerAccents.Length;
			int   b    = (a + 1) % DimensionColors.LayerAccents.Length;
			float frac = this.accentT - Mathf.Floor(this.accentT);
			this.accentBar.color = Color.Lerp(DimensionColors.ForLayer(a), DimensionColors.ForLayer(b), frac);
		}

		// Dismiss on any key once fully revealed
		if (this.revealed && !this.dismissing && this.rootGroup != null && this.rootGroup.alpha >= 1f) {
			if (Input.anyKeyDown) {
				this.dismissing = true;
				StartCoroutine(this.FadeOut());
			}
		}

#if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.F9) && !this.revealed) {
			CollectiblePlacer.TriggerSynthesis();
		}
#endif
	}

	// ── Trigger ───────────────────────────────────────────────────────────────

	private void Reveal() {
		if (this.revealed) return;
		this.revealed = true;
		StartCoroutine(this.FadeIn());
	}

	// ── Animations ────────────────────────────────────────────────────────────

	private IEnumerator FadeIn() {
		// Brief pause — let the synthesis city visuals settle first
		yield return new WaitForSeconds(2.5f);

		// Panel starts 40px below final position and rises as it fades in
		Vector2 hiddenPos  = new Vector2(0f, -40f);
		Vector2 visiblePos = Vector2.zero;
		float   duration   = 3.2f;

		for (float e = 0f; e < duration; e += Time.deltaTime) {
			float frac = e / duration;
			float ease = 1f - Mathf.Pow(1f - frac, 3f); // cubic ease-out
			this.rootGroup.alpha = Mathf.Lerp(0f, 1f, ease);
			if (this.panelRT != null)
				this.panelRT.anchoredPosition = Vector2.Lerp(hiddenPos, visiblePos, ease);
			yield return null;
		}

		this.rootGroup.alpha = 1f;
		if (this.panelRT != null) this.panelRT.anchoredPosition = visiblePos;
		this.rootGroup.blocksRaycasts = true;
	}

	private IEnumerator FadeOut() {
		float duration = 1.2f;
		for (float e = 0f; e < duration; e += Time.deltaTime) {
			this.rootGroup.alpha = Mathf.Lerp(1f, 0f, e / duration);
			yield return null;
		}
		this.rootGroup.alpha       = 0f;
		this.rootGroup.blocksRaycasts = false;
	}

	// ── Canvas construction ───────────────────────────────────────────────────

	private void BuildCanvas() {
		var cvGO = new GameObject("SynthesisCanvas");
		cvGO.transform.SetParent(this.transform, false);

		var cv = cvGO.AddComponent<Canvas>();
		cv.renderMode   = RenderMode.ScreenSpaceOverlay;
		cv.sortingOrder = 200; // above HUD (sortingOrder 50)

		var scaler = cvGO.AddComponent<CanvasScaler>();
		scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.matchWidthOrHeight  = 0.5f;
		cvGO.AddComponent<GraphicRaycaster>();

		this.rootGroup = cvGO.AddComponent<CanvasGroup>();
		this.rootGroup.alpha          = 0f;
		this.rootGroup.blocksRaycasts = false;

		// ── Full-screen vignette ──────────────────────────────────────────────
		var vig = MakeRect("Vignette", cvGO.transform);
		Stretch(vig);
		var vigImg = vig.AddComponent<Image>();
		vigImg.color          = new Color(0.01f, 0.01f, 0.04f, 0.68f);
		vigImg.raycastTarget  = false;

		// ── Central panel ─────────────────────────────────────────────────────
		var panelGO = MakeRect("Panel", cvGO.transform);
		this.panelRT            = panelGO.GetComponent<RectTransform>();
		this.panelRT.anchorMin  = this.panelRT.anchorMax = new Vector2(0.5f, 0.5f);
		this.panelRT.pivot      = new Vector2(0.5f, 0.5f);
		this.panelRT.sizeDelta  = new Vector2(660f, 540f);
		this.panelRT.anchoredPosition = new Vector2(0f, -40f);

		var panelImg = panelGO.AddComponent<Image>();
		panelImg.color         = new Color(0.03f, 0.03f, 0.06f, 0.96f);
		panelImg.raycastTarget = false;

		// Accent bar — top edge, colour-cycles through all W layers
		var abGO = MakeRect("AccentBar", panelGO.transform);
		var abRT = abGO.GetComponent<RectTransform>();
		abRT.anchorMin        = new Vector2(0f, 1f);
		abRT.anchorMax        = new Vector2(1f, 1f);
		abRT.pivot            = new Vector2(0.5f, 1f);
		abRT.anchoredPosition = Vector2.zero;
		abRT.sizeDelta        = new Vector2(0f, 4f);
		this.accentBar        = abGO.AddComponent<Image>();
		this.accentBar.raycastTarget = false;

		// Thin bottom bar
		var bbGO = MakeRect("BottomBar", panelGO.transform);
		var bbRT = bbGO.GetComponent<RectTransform>();
		bbRT.anchorMin        = new Vector2(0f, 0f);
		bbRT.anchorMax        = new Vector2(1f, 0f);
		bbRT.pivot            = new Vector2(0.5f, 0f);
		bbRT.anchoredPosition = Vector2.zero;
		bbRT.sizeDelta        = new Vector2(0f, 2f);
		bbGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);

		// Vertical layout group inside panel
		var vlg = panelGO.AddComponent<VerticalLayoutGroup>();
		vlg.padding              = new RectOffset(60, 60, 48, 40);
		vlg.spacing              = 8f;
		vlg.childControlWidth    = true;
		vlg.childControlHeight   = false;
		vlg.childForceExpandWidth  = true;
		vlg.childForceExpandHeight = false;

		// ── Content ───────────────────────────────────────────────────────────

		// Eyebrow
		var eyebrow = MakeTMP("Eyebrow", panelGO.transform, 9f);
		eyebrow.text             = "THE CITY REMEMBERS";
		eyebrow.color            = new Color(1f, 1f, 1f, 0.22f);
		eyebrow.fontStyle        = FontStyles.Bold;
		eyebrow.characterSpacing = 9f;
		eyebrow.alignment        = TextAlignmentOptions.Center;
		LE(eyebrow.gameObject, 14f);

		Spacer(panelGO.transform, 10f);

		// Main thank you — large, unhurried
		var thanks = MakeTMP("Thanks", panelGO.transform, 46f);
		thanks.text      = "Thank you\nfor playing.";
		thanks.fontStyle = FontStyles.Bold;
		thanks.alignment = TextAlignmentOptions.Center;
		thanks.color     = Color.white;
		thanks.lineSpacing = -4f;
		LE(thanks.gameObject, 116f);

		Spacer(panelGO.transform, 6f);

		// Subtext
		var sub = MakeTMP("Sub", panelGO.transform, 11f);
		sub.text             = "The city is whole now.  It will remain.";
		sub.color            = new Color(1f, 1f, 1f, 0.38f);
		sub.alignment        = TextAlignmentOptions.Center;
		sub.characterSpacing = 1.5f;
		LE(sub.gameObject, 16f);

		Spacer(panelGO.transform, 24f);

		// Divider
		MakeRect("Divider", panelGO.transform).AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
		LE(panelGO.transform.Find("Divider").gameObject, 1f);

		Spacer(panelGO.transform, 20f);

		// Credits header
		var credHdr = MakeTMP("CreditsHdr", panelGO.transform, 8f);
		credHdr.text             = "CREDITS";
		credHdr.color            = new Color(1f, 1f, 1f, 0.18f);
		credHdr.fontStyle        = FontStyles.Bold;
		credHdr.characterSpacing = 6f;
		credHdr.alignment        = TextAlignmentOptions.Center;
		LE(credHdr.gameObject, 12f);

		Spacer(panelGO.transform, 10f);

		// Credit rows — replace with real names before shipping
		this.CreditRow(panelGO.transform, "Design & Development", "Your Name Here");
		this.CreditRow(panelGO.transform, "Wave Function Collapse", "Your Name Here");
		this.CreditRow(panelGO.transform, "Music & Sound",         "Your Name Here");
		this.CreditRow(panelGO.transform, "Special Thanks",        "Everyone Who Played");

		Spacer(panelGO.transform, 22f);

		// Dismiss hint — very quiet
		var hint = MakeTMP("Hint", panelGO.transform, 8f);
		hint.text             = "press any key to continue exploring";
		hint.color            = new Color(1f, 1f, 1f, 0.15f);
		hint.alignment        = TextAlignmentOptions.Center;
		hint.characterSpacing = 2f;
		LE(hint.gameObject, 12f);
	}

	private void CreditRow(Transform parent, string role, string name) {
		var rowGO = MakeRect("CR_" + role, parent);
		var hlg   = rowGO.AddComponent<HorizontalLayoutGroup>();
		hlg.childControlWidth      = true;
		hlg.childControlHeight     = false;
		hlg.childForceExpandWidth  = false;
		hlg.childForceExpandHeight = false;
		LE(rowGO, 14f);

		var roleTMP = MakeTMP("Role", rowGO.transform, 9f);
		roleTMP.text             = role;
		roleTMP.color            = new Color(1f, 1f, 1f, 0.28f);
		roleTMP.characterSpacing = 0.5f;
		var rLE = roleTMP.gameObject.AddComponent<LayoutElement>();
		rLE.flexibleWidth   = 1f;
		rLE.preferredHeight = 14f;

		var nameTMP = MakeTMP("Name", rowGO.transform, 9f);
		nameTMP.text      = name;
		nameTMP.color     = new Color(1f, 1f, 1f, 0.60f);
		nameTMP.alignment = TextAlignmentOptions.Right;
		nameTMP.fontStyle = FontStyles.Bold;
		var nLE = nameTMP.gameObject.AddComponent<LayoutElement>();
		nLE.flexibleWidth   = 1f;
		nLE.preferredHeight = 14f;
	}

	// ── Helpers (mirrors WLayerHUD pattern) ───────────────────────────────────

	private static GameObject MakeRect(string name, Transform parent) {
		var go = new GameObject(name, typeof(RectTransform));
		go.transform.SetParent(parent, false);
		return go;
	}

	private static TMP_FontAsset cachedFont;

	private static TextMeshProUGUI MakeTMP(string name, Transform parent, float size) {
		var go = MakeRect(name, parent);
		var t  = go.AddComponent<TextMeshProUGUI>();
		t.fontSize      = size;
		t.raycastTarget = false;
		if (t.font == null) {
			if (cachedFont == null)
				cachedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
			if (cachedFont != null) t.font = cachedFont;
		}
		return t;
	}

	private static void Stretch(GameObject go) {
		var rt        = go.GetComponent<RectTransform>();
		rt.anchorMin  = Vector2.zero;
		rt.anchorMax  = Vector2.one;
		rt.offsetMin  = rt.offsetMax = Vector2.zero;
	}

	private static void LE(GameObject go, float height) {
		go.AddComponent<LayoutElement>().preferredHeight = height;
	}

	private static void Spacer(Transform parent, float height) {
		LE(MakeRect("Spacer", parent), height);
	}
}
