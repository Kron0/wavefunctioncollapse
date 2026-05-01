using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Plays procedurally-generated peaceful garden ambience driven by the player's 4D position.
// Notes are drawn from a C pentatonic scale; W layer controls octave and tempo.
// No external assets required — all audio is synthesised at runtime.
[RequireComponent(typeof(AudioSource))]
public class ProceduralAmbience : MonoBehaviour {

	private FourDController player;

	// ── Synthesis parameters ──────────────────────────────────────────────
	private const int   SampleRate  = 44100;
	private const float NoteDuration = 1.8f;   // seconds per clip
	private const float DecayRate   = 3.2f;    // envelope decay
	private const float HarmDecay   = 5.0f;    // harmonic decay (softer)

	// C major pentatonic: intervals in semitones from C4
	private static readonly int[] Pentatonic = { 0, 2, 4, 7, 9 };
	private const float C4 = 261.63f;   // Hz

	// Per-layer tempo multiplier (higher W = quicker notes)
	private static readonly float[] LayerTempo = { 1.0f, 1.15f, 0.85f, 1.3f, 0.7f, 1.1f };
	// Per-layer note density bias (0=sparse .. 1=full)
	private static readonly float[] LayerDensity = { 0.5f, 0.7f, 0.3f, 0.8f, 0.4f, 0.65f };

	// Pre-baked clips: [octave 0..2][pentatonic index 0..4]
	private AudioClip[,] noteClips;
	private AudioSource  audioSource;

	// WFC-style note constraint: avoid repeating the same index twice in a row
	private int lastNoteIndex = -1;

	void Start() {
		this.audioSource = this.GetComponent<AudioSource>();
		this.audioSource.spatialBlend = 0f;   // 2D — ambient, not positional
		this.audioSource.volume       = 0.55f;

		if (this.player == null) {
			var go = GameObject.FindGameObjectWithTag("Player");
			if (go != null) this.player = go.GetComponent<FourDController>();
		}

		this.BakeNoteClips();
		this.StartCoroutine(this.AmbienceLoop());
	}

	// =========================================================================
	// MAIN LOOP
	// =========================================================================
	private IEnumerator AmbienceLoop() {
		// Wait for startup fade
		while (!GameState.StartupComplete) yield return null;
		yield return new WaitForSeconds(1.5f);

		while (true) {
			if (!GameState.IsPaused && this.player != null) {
				this.PlayNote();
			}

			int   layer  = MapBehaviour4D.ActiveWLayer;
			int   li     = ((layer % LayerTempo.Length) + LayerTempo.Length) % LayerTempo.Length;
			float tempo  = LayerTempo[li];
			float dens   = LayerDensity[li];

			// Interval: 1.5s (dense) to 5s (sparse), modulated by density + W position
			float wFrac  = this.player != null ? Mathf.Repeat(this.player.WPosition, 1f) : 0.5f;
			float interval = Mathf.Lerp(5f, 1.5f, dens) / tempo;
			interval += (wFrac - 0.5f) * 0.8f;  // slight rhythmic drift per W position
			interval  = Mathf.Clamp(interval, 1.2f, 6f);

			yield return new WaitForSeconds(interval);
		}
	}

	// =========================================================================
	// NOTE SELECTION & PLAYBACK
	// =========================================================================
	private void PlayNote() {
		if (this.noteClips == null) return;

		Vector3 pos   = this.player.transform.position;
		float   wPos  = this.player.WPosition;
		int     layer = MapBehaviour4D.ActiveWLayer;

		// Hash player XZ to pick pentatonic degree
		int xzSeed = Mathf.FloorToInt(pos.x * 0.25f) * 397 + Mathf.FloorToInt(pos.z * 0.25f) * 1013;
		xzSeed += Mathf.FloorToInt(Time.time * 0.4f);  // slow temporal drift

		var rng = new System.Random(xzSeed);

		// WFC constraint: weight away from lastNoteIndex
		float[] weights = new float[Pentatonic.Length];
		for (int i = 0; i < weights.Length; i++) {
			weights[i] = 1f;
			if (i == this.lastNoteIndex) weights[i] = 0.15f;          // penalise repeat
			if (Mathf.Abs(i - this.lastNoteIndex) == 1) weights[i] += 0.6f; // prefer step motion
		}
		int noteIdx = this.WeightedRandom(weights, rng);

		// Octave: W layer biases toward higher octaves
		int li     = Mathf.Clamp(layer / 2, 0, 2);
		int octave = (int)Mathf.Clamp(li + rng.Next(0, 2), 0, 2);

		// Volume envelope: softer at W fractions near 0.5 (mid-transition)
		float wFrac = Mathf.Repeat(wPos, 1f);
		float vol   = Mathf.Lerp(0.25f, 0.75f, 1f - Mathf.Abs(wFrac - 0.5f) * 2f);

		this.audioSource.PlayOneShot(this.noteClips[octave, noteIdx], vol);

		// Occasional harmony: play a fifth above (3 semitones up in pentatonic ≈ index +2)
		if (rng.NextDouble() < 0.35f) {
			int harmIdx = Mathf.Min(noteIdx + 2, Pentatonic.Length - 1);
			this.audioSource.PlayOneShot(this.noteClips[octave, harmIdx], vol * 0.45f);
		}

		this.lastNoteIndex = noteIdx;
	}

	private int WeightedRandom(float[] weights, System.Random rng) {
		float sum = 0f;
		foreach (var w in weights) sum += w;
		float pick = (float)rng.NextDouble() * sum;
		for (int i = 0; i < weights.Length; i++) {
			pick -= weights[i];
			if (pick <= 0f) return i;
		}
		return weights.Length - 1;
	}

	// =========================================================================
	// CLIP BAKING
	// =========================================================================
	private void BakeNoteClips() {
		int    samples  = Mathf.RoundToInt(SampleRate * NoteDuration);
		this.noteClips  = new AudioClip[3, Pentatonic.Length];

		for (int oct = 0; oct < 3; oct++) {
			for (int n = 0; n < Pentatonic.Length; n++) {
				int   semitone = Pentatonic[n] + oct * 12;
				float freq     = C4 * Mathf.Pow(2f, semitone / 12f);
				var   clip     = AudioClip.Create($"Note_{oct}_{n}", samples, 1, SampleRate, false);
				clip.SetData(this.BellSamples(freq, samples), 0);
				this.noteClips[oct, n] = clip;
			}
		}
	}

	private float[] BellSamples(float freq, int count) {
		var data   = new float[count];
		float tau  = 2f * Mathf.PI;

		for (int i = 0; i < count; i++) {
			float t = (float)i / SampleRate;

			// Fast attack, exponential decay envelope
			float env = Mathf.Exp(-t * DecayRate);

			// Fundamental + 2nd harmonic (bell timbre)
			float fundamental = Mathf.Sin(tau * freq * t) * env;
			float harmonic    = Mathf.Sin(tau * freq * 2.01f * t) * Mathf.Exp(-t * HarmDecay) * 0.28f;

			// Soft 5th overtone for warmth
			float fifth       = Mathf.Sin(tau * freq * 3f * t) * Mathf.Exp(-t * HarmDecay * 1.8f) * 0.10f;

			data[i] = (fundamental + harmonic + fifth) * 0.7f;
		}

		return data;
	}
}
