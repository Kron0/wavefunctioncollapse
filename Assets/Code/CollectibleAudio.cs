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
