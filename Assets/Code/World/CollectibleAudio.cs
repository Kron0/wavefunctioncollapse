using UnityEngine;

// Procedural audio clips for collectible artifacts.
// Clips are generated once and cached for the lifetime of the session.
public static class CollectibleAudio {
	// One ambient frequency per W-layer — a minor pentatonic root set
	private static readonly float[] AmbientFreqs = {
		110.00f,  // W0  A2
		130.81f,  // W1  C3
		146.83f,  // W2  D3
		164.81f,  // W3  E3
		196.00f,  // W4  G3
		220.00f,  // W5  A3
	};

	// Pentatonic chime scale (C5 major pentatonic, two octaves)
	private static readonly float[] ChimeFreqs = {
		523.25f, 587.33f, 659.25f, 783.99f, 880.00f,
		1046.5f, 1174.7f, 1318.5f, 1568.0f, 1760.0f,
	};

	private static AudioClip[] ambientCache;
	private static AudioClip[] chimeCache;

	// Gentle looping hum, spatialised — played by each collectible while it exists.
	public static AudioClip GetAmbient(int wLayer) {
		if (ambientCache == null) ambientCache = new AudioClip[AmbientFreqs.Length];
		int idx = ((wLayer % AmbientFreqs.Length) + AmbientFreqs.Length) % AmbientFreqs.Length;
		if (ambientCache[idx] == null) ambientCache[idx] = MakeAmbient(AmbientFreqs[idx]);
		return ambientCache[idx];
	}

	// Short bright chime, rising through the pentatonic scale with each pickup.
	public static AudioClip GetChime(int pickupIndex) {
		if (chimeCache == null) chimeCache = new AudioClip[ChimeFreqs.Length];
		int idx = pickupIndex % ChimeFreqs.Length;
		if (chimeCache[idx] == null) chimeCache[idx] = MakeChime(ChimeFreqs[idx]);
		return chimeCache[idx];
	}

	// Deep resonant drone with slow vibrato: 2s loop at 22050 Hz
	private static AudioClip MakeAmbient(float freq) {
		const int sampleRate = 22050;
		const float duration = 2f;
		int n = Mathf.RoundToInt(sampleRate * duration);
		var clip = AudioClip.Create($"amb_{freq:F0}", n, 1, sampleRate, false);
		var buf = new float[n];
		for (int i = 0; i < n; i++) {
			float t   = (float)i / sampleRate;
			// Slow LFO (0.8 Hz) gives gentle shimmer
			float lfo = 1f + 0.04f * Mathf.Sin(2f * Mathf.PI * 0.8f * t);
			float env = Mathf.Sin(Mathf.PI * t / duration); // fade in+out for seamless loop
			buf[i] = (Mathf.Sin(2f * Mathf.PI * freq * lfo * t) * 0.55f
			        + Mathf.Sin(2f * Mathf.PI * freq * 2f * t)  * 0.18f
			        + Mathf.Sin(2f * Mathf.PI * freq * 3f * t)  * 0.06f) * env;
		}
		clip.SetData(buf, 0);
		return clip;
	}

	// Bell-like chime: fast attack, exponential decay, 0.5s
	private static AudioClip MakeChime(float freq) {
		const int sampleRate = 44100;
		const float duration = 0.5f;
		int n = Mathf.RoundToInt(sampleRate * duration);
		var clip = AudioClip.Create($"chime_{freq:F0}", n, 1, sampleRate, false);
		var buf = new float[n];
		for (int i = 0; i < n; i++) {
			float t   = (float)i / sampleRate;
			float env = Mathf.Clamp01(t / 0.008f) * Mathf.Exp(-t * 7f);
			buf[i] = (Mathf.Sin(2f * Mathf.PI * freq       * t) * 0.70f
			        + Mathf.Sin(2f * Mathf.PI * freq * 2f   * t) * 0.20f
			        + Mathf.Sin(2f * Mathf.PI * freq * 3.1f * t) * 0.08f) * env;
		}
		clip.SetData(buf, 0);
		return clip;
	}

	// ── Voice approval (one per W-layer) ─────────────────────────────────────
	// Formant-synthesised vocals: harmonic source + vowel resonance filter,
	// pitch glide, vibrato, and breathiness.  Each layer sounds distinct.

	private static AudioClip[] voiceCache;

	public static AudioClip GetVoice(int wLayer) {
		if (voiceCache == null) voiceCache = new AudioClip[6];
		int idx = ((wLayer % 6) + 6) % 6;
		if (voiceCache[idx] == null) voiceCache[idx] = MakeVoice(idx);
		return voiceCache[idx];
	}

	// Fundamental pitch (Hz), pitch-glide ratio, vowel formants F1/F2/F3,
	// clip duration (s), and breathiness per layer.
	// Layer character: 0=warm OOH, 1=deep MMM, 2=open AHH,
	//                  3=bright EEE, 4=bold YAH, 5=soft OHH
	private static readonly float[] VoicePitch  = { 262f, 165f, 330f, 415f, 220f, 196f };
	private static readonly float[] VoiceGlide  = { 1.18f, 1.06f, 1.22f, 1.28f, 1.10f, 1.14f };
	private static readonly float[] VoiceF1     = { 310f, 390f, 780f, 450f, 800f, 440f };
	private static readonly float[] VoiceF2     = { 880f, 980f, 1150f, 2300f, 1250f, 820f };
	private static readonly float[] VoiceF3     = { 2200f, 2450f, 2850f, 3200f, 2800f, 2650f };
	private static readonly float[] VoiceDurSec = { 0.48f, 0.58f, 0.42f, 0.36f, 0.44f, 0.52f };
	private static readonly float[] VoiceBreath = { 0.05f, 0.04f, 0.04f, 0.07f, 0.03f, 0.09f };
	private static readonly float[] GlottalGain = { 1.00f, 0.50f, 0.33f, 0.25f, 0.20f, 0.17f, 0.14f, 0.12f };

	private static AudioClip MakeVoice(int layer) {
		const int sampleRate = 44100;
		float duration = VoiceDurSec[layer];
		int   n    = Mathf.RoundToInt(sampleRate * duration);
		float p0   = VoicePitch[layer];
		float g    = VoiceGlide[layer];
		float f1   = VoiceF1[layer], f2 = VoiceF2[layer], f3 = VoiceF3[layer];
		float bth  = VoiceBreath[layer];
		var   rng  = new System.Random(layer * 7919 + 42);
		var   buf  = new float[n];

		// Separate normalised-phase accumulator per harmonic [0, 1)
		var ph = new float[8];

		for (int i = 0; i < n; i++) {
			float t    = (float)i / sampleRate;
			float frac = t / duration;

			// Gliding fundamental with delayed vibrato
			float fund   = p0 * Mathf.Lerp(1f, g, frac);
			float vibAmt = Mathf.Clamp01((t - 0.06f) / 0.04f) * 0.018f;
			float curF   = fund * (1f + vibAmt * Mathf.Sin(2f * Mathf.PI * 5.2f * t));
			float dt     = curF / sampleRate;

			// Sum harmonics weighted by glottal roll-off and vowel formants
			float voiced = 0f;
			for (int h = 0; h < 8; h++) {
				ph[h] = (ph[h] + dt * (h + 1)) % 1f;
				float hFreq = curF * (h + 1);
				float amp   = VoiceFormant(hFreq, f1, f2, f3) * GlottalGain[h];
				voiced += Mathf.Sin(ph[h] * 2f * Mathf.PI) * amp;
			}

			// Breathy noise onset (voiced/unvoiced blend over first 40 ms)
			float noise       = ((float)rng.NextDouble() * 2f - 1f) * bth;
			float voicedBlend = Mathf.Clamp01(t / 0.04f);

			buf[i] = (voiced * voicedBlend + noise) * VoiceEnv(frac) * 0.5f;
		}

		// Normalise to ±0.88 peak
		float peak = 0f;
		for (int i = 0; i < n; i++) if (Mathf.Abs(buf[i]) > peak) peak = Mathf.Abs(buf[i]);
		if (peak > 0.01f) { float sc = 0.88f / peak; for (int i = 0; i < n; i++) buf[i] *= sc; }

		var clip = AudioClip.Create($"voice_{layer}", n, 1, sampleRate, false);
		clip.SetData(buf, 0);
		return clip;
	}

	// Gaussian formant filter: returns amplitude at freq given three resonance peaks.
	private static float VoiceFormant(float freq, float f1, float f2, float f3) {
		const float bw1 = 120f, bw2 = 150f, bw3 = 200f;
		float g1 = Mathf.Exp(-(freq - f1) * (freq - f1) / (2f * bw1 * bw1));
		float g2 = Mathf.Exp(-(freq - f2) * (freq - f2) / (2f * bw2 * bw2)) * 0.85f;
		float g3 = Mathf.Exp(-(freq - f3) * (freq - f3) / (2f * bw3 * bw3)) * 0.50f;
		return Mathf.Clamp01(g1 + g2 + g3);
	}

	// Attack → sustain → release envelope for the voice clip.
	private static float VoiceEnv(float frac) {
		const float attack = 0.08f, release = 0.30f;
		if (frac < attack)       return frac / attack;
		if (frac < 1f - release) return 1f;
		return (1f - frac) / release;
	}

	// ── Gate sounds ───────────────────────────────────────────────────────────

	private static AudioClip gateUp;
	private static AudioClip gateDown;

	// Cached gate clip per direction — call once per W-layer transition.
	public static AudioClip GetGateSound(int direction) {
		if (direction > 0) {
			if (gateUp   == null) gateUp   = MakeGateSound(1);
			return gateUp;
		}
		if (gateDown == null) gateDown = MakeGateSound(-1);
		return gateDown;
	}

	// Whoosh/portal tone used by W-gates — a sweeping band-limited noise burst.
	// Direction: +1 for ascending W, -1 for descending.
	private static AudioClip MakeGateSound(int direction) {
		const int sampleRate = 44100;
		const float duration = 0.45f;
		int n = Mathf.RoundToInt(sampleRate * duration);
		var clip = AudioClip.Create("gate_sound", n, 1, sampleRate, false);
		var buf = new float[n];

		// Two sweeping sine tones: one rising, one falling
		float freqStart = direction > 0 ? 220f : 880f;
		float freqEnd   = direction > 0 ? 880f : 220f;

		for (int i = 0; i < n; i++) {
			float t    = (float)i / sampleRate;
			float frac = t / duration;
			float freq = Mathf.Lerp(freqStart, freqEnd, frac);
			float env  = Mathf.Clamp01(frac / 0.05f) * Mathf.Clamp01((1f - frac) / 0.15f);
			buf[i] = (Mathf.Sin(2f * Mathf.PI * freq       * t) * 0.55f
			        + Mathf.Sin(2f * Mathf.PI * freq * 1.5f * t) * 0.25f) * env * 0.7f;
		}
		clip.SetData(buf, 0);
		return clip;
	}
}
