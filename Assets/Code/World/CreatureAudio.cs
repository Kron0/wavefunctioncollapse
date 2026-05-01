using System;
using UnityEngine;

// Procedural audio clips for city wildlife.
// All sounds are synthesised at runtime — no audio assets required.
// Clips are generated once and cached for the session.
public static class CreatureAudio {

	// ── Public API ────────────────────────────────────────────────────────────

	// Bird call. pitchMul: 1.0=songbird, 0.65=crow, 0.80=pigeon, 1.30=gull.
	// varietyIndex rotates through a small set of clip variations.
	public static AudioClip GetBird(float pitchMul, int varietyIndex = 0) {
		var key = new BirdKey { pitch = Mathf.Round(pitchMul * 10f) / 10f, variety = varietyIndex % 4 };
		if (!birdCache.TryGetValue(key, out var clip)) {
			clip = MakeBird(pitchMul, varietyIndex % 4);
			birdCache[key] = clip;
		}
		return clip;
	}

	public static AudioClip GetCat(int varietyIndex = 0) {
		int idx = varietyIndex % catCache.Length;
		return catCache[idx] ?? (catCache[idx] = MakeCat(idx));
	}

	public static AudioClip GetDog(int varietyIndex = 0) {
		int idx = varietyIndex % dogCache.Length;
		return dogCache[idx] ?? (dogCache[idx] = MakeDog(idx));
	}

	public static AudioClip GetMouse(bool squeak = false) {
		if (squeak) return mouseSqueak ?? (mouseSqueak = MakeMouseSqueak());
		return mouseRustle ?? (mouseRustle = MakeMouseRustle());
	}

	// ── Caches ────────────────────────────────────────────────────────────────

	private struct BirdKey : IEquatable<BirdKey> {
		public float pitch;
		public int   variety;
		public bool Equals(BirdKey o) => pitch == o.pitch && variety == o.variety;
		public override int GetHashCode() => pitch.GetHashCode() * 397 ^ variety;
	}

	private static readonly System.Collections.Generic.Dictionary<BirdKey, AudioClip> birdCache
		= new System.Collections.Generic.Dictionary<BirdKey, AudioClip>();

	private static readonly AudioClip[] catCache = new AudioClip[3];
	private static readonly AudioClip[] dogCache = new AudioClip[3];
	private static AudioClip mouseRustle;
	private static AudioClip mouseSqueak;

	// ── Bird ──────────────────────────────────────────────────────────────────
	// FM synthesis: carrier modulated by sub-oscillator for natural warble.
	// pitchMul shifts the register; variety changes chirp pattern and cadence.

	private static AudioClip MakeBird(float pitchMul, int variety) {
		const int sr = 44100;

		// Carrier base and modulation differ by bird type
		float carrier, modRatio, modDepth, chirpDur;
		int   chirpCount;

		if (pitchMul >= 1.20f) {
			// Gull / high bird — sharp, piercing, single sweeping call
			carrier    = 2800f * pitchMul;
			modRatio   = 0.5f;
			modDepth   = carrier * 0.55f;
			chirpDur   = 0.25f;
			chirpCount = 1 + (variety % 2);
		} else if (pitchMul <= 0.70f) {
			// Crow / raven — rough, lower, irregular
			carrier    = 1400f * pitchMul;
			modRatio   = 1.3f;
			modDepth   = carrier * 0.90f;
			chirpDur   = 0.18f;
			chirpCount = 2 + (variety % 3);
		} else if (pitchMul <= 0.85f) {
			// Pigeon / dove — slow, mellow coo
			carrier    = 900f * pitchMul;
			modRatio   = 0.25f;
			modDepth   = carrier * 0.30f;
			chirpDur   = 0.40f;
			chirpCount = 1 + (variety % 2);
		} else {
			// Songbird — melodic, rapid chirp burst
			carrier    = 2200f * pitchMul;
			modRatio   = 0.75f;
			modDepth   = carrier * 0.70f;
			chirpDur   = 0.12f;
			chirpCount = 2 + (variety % 4);
		}

		float gap       = chirpDur * 0.55f;
		float totalDur  = chirpCount * (chirpDur + gap) + 0.05f;
		int   n         = Mathf.RoundToInt(sr * totalDur);
		var   buf       = new float[n];
		var   rng       = new Random(variety * 7331 + (int)(pitchMul * 100f));

		float mod = carrier * modRatio;
		for (int c = 0; c < chirpCount; c++) {
			float startT = c * (chirpDur + gap);
			// Small pitch variation per chirp for realism
			float pitchJit = 1f + (float)(rng.NextDouble() - 0.5) * 0.08f;
			float cf = carrier * pitchJit;
			float mf = mod    * pitchJit;

			for (int i = 0; i < Mathf.RoundToInt(sr * chirpDur); i++) {
				int   si  = Mathf.RoundToInt(sr * startT) + i;
				if (si >= n) break;
				float t   = (float)i / sr;
				float frac = t / chirpDur;

				// Envelope: quick attack, brief sustain, gentle tail
				float env = BirdEnv(frac);

				// FM: sample = sin(2π·cf·t + modDepth/mf·sin(2π·mf·t))
				double phaseM  = 2.0 * Math.PI * mf * t;
				double phaseC  = 2.0 * Math.PI * cf * t + (modDepth / Mathf.Max(1f, mf)) * Math.Sin(phaseM);
				buf[si] += (float)(Math.Sin(phaseC) * env * 0.55f);
			}
		}

		Normalize(buf, 0.80f);
		var clip = AudioClip.Create($"bird_{pitchMul:F1}_{variety}", n, 1, sr, false);
		clip.SetData(buf, 0);
		return clip;
	}

	private static float BirdEnv(float frac) {
		if (frac < 0.08f) return frac / 0.08f;
		if (frac < 0.50f) return 1f;
		return (1f - frac) / 0.50f;
	}

	// ── Cat ───────────────────────────────────────────────────────────────────
	// Formant synthesis with a pitch arc: rises then falls back (meow contour).

	private static readonly float[] CatBasePitch = { 285f, 320f, 260f };
	private static readonly float[] CatF1        = { 600f, 680f, 550f };
	private static readonly float[] CatF2        = { 1400f, 1600f, 1200f };

	private static AudioClip MakeCat(int idx) {
		const int sr = 44100;
		float p0  = CatBasePitch[idx];
		float f1  = CatF1[idx], f2 = CatF2[idx], f3 = 2600f;
		float dur = 0.55f;
		int   n   = Mathf.RoundToInt(sr * dur);
		var   buf = new float[n];
		var   rng = new Random(idx * 2311);
		var   ph  = new float[6];

		for (int i = 0; i < n; i++) {
			float t    = (float)i / sr;
			float frac = t / dur;

			// Pitch arc: rise to peak at 40%, decay back (meow contour)
			float glide  = frac < 0.40f ? frac / 0.40f : 1f - (frac - 0.40f) / 0.60f;
			float curP   = p0 * (1f + 0.55f * glide);
			float vibAmt = Mathf.Clamp01((t - 0.08f) / 0.05f) * 0.015f;
			curP *= 1f + vibAmt * Mathf.Sin(2f * Mathf.PI * 5.5f * t);
			float dt = curP / sr;

			float voiced = 0f;
			float[] glottal = { 1.00f, 0.50f, 0.33f, 0.25f, 0.20f, 0.17f };
			for (int h = 0; h < 6; h++) {
				ph[h] = (ph[h] + dt * (h + 1)) % 1f;
				float hFreq = curP * (h + 1);
				float amp   = VoiceFormant(hFreq, f1, f2, f3) * glottal[h];
				voiced += Mathf.Sin(ph[h] * 2f * Mathf.PI) * amp;
			}

			// Slight nasality from noise
			float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.06f;
			float env   = CatEnv(frac);
			buf[i] = (voiced * Mathf.Clamp01(t / 0.03f) + noise) * env * 0.45f;
		}

		Normalize(buf, 0.88f);
		var clip = AudioClip.Create($"cat_{idx}", n, 1, sr, false);
		clip.SetData(buf, 0);
		return clip;
	}

	private static float CatEnv(float frac) {
		if (frac < 0.06f) return frac / 0.06f;
		if (frac < 0.75f) return 1f;
		return (1f - frac) / 0.25f;
	}

	// ── Dog ───────────────────────────────────────────────────────────────────
	// Voiced burst with a noisy plosive onset — bark character.

	private static readonly float[] DogBasePitch = { 380f, 440f, 310f };

	private static AudioClip MakeDog(int idx) {
		const int sr = 44100;
		float p0  = DogBasePitch[idx];
		float dur = 0.20f;
		int   n   = Mathf.RoundToInt(sr * dur);
		var   buf = new float[n];
		var   rng = new Random(idx * 1777 + 99);

		for (int i = 0; i < n; i++) {
			float t    = (float)i / sr;
			float frac = t / dur;

			// Voiced harmonic burst
			float voiced = 0f;
			for (int h = 1; h <= 5; h++) {
				voiced += Mathf.Sin(2f * Mathf.PI * p0 * h * t) * (1f / h);
			}

			// Plosive noise onset (first 30ms)
			float noiseFade = Mathf.Clamp01(1f - t / 0.03f);
			float noise = ((float)rng.NextDouble() * 2f - 1f) * noiseFade;

			// Bark envelope: very fast attack, exponential decay
			float env = Mathf.Clamp01(t / 0.005f) * Mathf.Exp(-frac * 6f);
			buf[i] = (voiced * 0.55f + noise * 0.45f) * env * 0.70f;
		}

		Normalize(buf, 0.88f);
		var clip = AudioClip.Create($"dog_{idx}", n, 1, sr, false);
		clip.SetData(buf, 0);
		return clip;
	}

	// ── Mouse ─────────────────────────────────────────────────────────────────

	private static AudioClip MakeMouseRustle() {
		const int sr = 44100;
		const float dur = 0.14f;
		int n = Mathf.RoundToInt(sr * dur);
		var buf = new float[n];
		var rng = new Random(42);

		// Highpass-filtered noise burst — sounds like small movement in dry material
		float prev = 0f;
		for (int i = 0; i < n; i++) {
			float t    = (float)i / sr;
			float frac = t / dur;
			float raw  = (float)rng.NextDouble() * 2f - 1f;
			// Simple 1-pole highpass (fc ≈ 4500 Hz)
			float hp   = raw - prev * 0.36f;
			prev = raw;
			float env  = Mathf.Clamp01(t / 0.015f) * (1f - frac * frac);
			buf[i] = hp * env * 0.60f;
		}

		Normalize(buf, 0.75f);
		var clip = AudioClip.Create("mouse_rustle", n, 1, sr, false);
		clip.SetData(buf, 0);
		return clip;
	}

	private static AudioClip MakeMouseSqueak() {
		const int sr = 44100;
		const float dur = 0.09f;
		int n = Mathf.RoundToInt(sr * dur);
		var buf = new float[n];

		// Brief FM chirp, very high pitch
		float carrier = 4200f;
		float modF    = carrier * 0.6f;
		float modD    = carrier * 1.2f;

		for (int i = 0; i < n; i++) {
			float t    = (float)i / sr;
			float frac = t / dur;
			// Pitch glide upward
			float cf   = carrier * (1f + frac * 0.3f);
			float env  = Mathf.Clamp01(t / 0.005f) * Mathf.Exp(-frac * 8f);
			double ph  = 2.0 * Math.PI * cf * t + (modD / modF) * Math.Sin(2.0 * Math.PI * modF * t);
			buf[i] = (float)(Math.Sin(ph) * env * 0.55f);
		}

		Normalize(buf, 0.80f);
		var clip = AudioClip.Create("mouse_squeak", n, 1, sr, false);
		clip.SetData(buf, 0);
		return clip;
	}

	// ── Shared helpers ────────────────────────────────────────────────────────

	// Gaussian formant filter — identical to the voice system in CollectibleAudio.
	private static float VoiceFormant(float freq, float f1, float f2, float f3) {
		const float bw1 = 120f, bw2 = 150f, bw3 = 200f;
		float g1 = Mathf.Exp(-(freq - f1) * (freq - f1) / (2f * bw1 * bw1));
		float g2 = Mathf.Exp(-(freq - f2) * (freq - f2) / (2f * bw2 * bw2)) * 0.85f;
		float g3 = Mathf.Exp(-(freq - f3) * (freq - f3) / (2f * bw3 * bw3)) * 0.50f;
		return Mathf.Clamp01(g1 + g2 + g3);
	}

	private static void Normalize(float[] buf, float target) {
		float peak = 0f;
		for (int i = 0; i < buf.Length; i++) if (Mathf.Abs(buf[i]) > peak) peak = Mathf.Abs(buf[i]);
		if (peak > 0.01f) { float s = target / peak; for (int i = 0; i < buf.Length; i++) buf[i] *= s; }
	}
}
