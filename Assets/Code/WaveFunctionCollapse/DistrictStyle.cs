using System;

// Architectural style identifiers — shared between WFC weighting and the visual district system
public enum ArchStyle {
	Victorian     = 0,
	Modernist     = 1,
	Mediterranean = 2,
	Industrial    = 3,
	Brutalist     = 4,
	Ornate        = 5,
	Park          = 6,
	Commercial    = 7,
}

// Flags version used on ModulePrototype to let designers tag modules in the Inspector
[Flags]
public enum ArchStyleFlags {
	None          = 0,
	Victorian     = 1 << 0,
	Modernist     = 1 << 1,
	Mediterranean = 1 << 2,
	Industrial    = 1 << 3,
	Brutalist     = 1 << 4,
	Ornate        = 1 << 5,
	Park          = 1 << 6,
	Commercial    = 1 << 7,
	All           = Victorian | Modernist | Mediterranean | Industrial | Brutalist | Ornate | Park | Commercial,
}

// Thread-safe static provider — written from the main thread by DistrictStyler,
// read from the generation background thread by createChunk.
public static class DistrictBiasProvider {
	public struct SeedData {
		public float x, z;
		public int style; // cast of ArchStyle
	}

	private static volatile SeedData[] seeds;

	public static void SetSeeds(SeedData[] s) {
		seeds = s;
	}

	// Called from background thread — pure math, no Unity API.
	public static int GetChunkStyle(int chunkX, int chunkZ) {
		var s = seeds;
		if (s == null || s.Length == 0) return -1;

		float bestDist = float.MaxValue;
		int bestIdx = 0;
		for (int i = 0; i < s.Length; i++) {
			float dx = chunkX - s[i].x;
			float dz = chunkZ - s[i].z;
			float d  = dx * dx + dz * dz;
			if (d < bestDist) { bestDist = d; bestIdx = i; }
		}
		return s[bestIdx].style;
	}

	private const int StyleCount = 8;

	// Precompute per-style probability multipliers for a module.
	// Called once during initialization — result cached on Module.DistrictWeights.
	public static float[] ComputeWeights(string moduleName, ArchStyleFlags affinity) {
		var weights = new float[StyleCount];

		if (affinity != ArchStyleFlags.All && affinity != ArchStyleFlags.None) {
			// Designer-set tags: matching style gets 1.0, others get a strong penalty
			for (int i = 0; i < StyleCount; i++) {
				var flag = (ArchStyleFlags)(1 << i);
				weights[i] = (affinity & flag) != 0 ? 1.0f : 0.08f;
			}
			return weights;
		}

		// Name-based heuristic — covers the default case where designer hasn't tagged the module
		string n = moduleName.ToLower();
		// Strip rotation suffix ("wall_window r0" → "wall_window")
		int spaceIdx = n.IndexOf(' ');
		string base_ = spaceIdx >= 0 ? n.Substring(0, spaceIdx) : n;

		bool isArch         = base_.Contains("arch");
		bool isBalcony      = base_.Contains("balcony");
		bool isSpiral       = base_.Contains("spiral");
		bool isPillar       = base_.Contains("pillar");
		bool isFountain     = base_.Contains("fountain");
		bool isClock        = base_.Contains("clock");
		bool isRailing      = base_.Contains("railing");
		bool isWindow       = base_.Contains("window");
		bool isTunnel       = base_.Contains("tunnel");
		// "bridge" alone, not "arch_bridge"
		bool isBridge       = (base_ == "bridge" || base_.StartsWith("bridge_")) && !base_.Contains("arch");
		bool isEnclosed     = base_.Contains("enclosed");
		bool isHighWallPlain= base_.StartsWith("high_wall") && !isWindow && !isClock;
		bool isWalkable     = base_.Contains("walkablearea");
		bool isInteriorHigh = base_.Contains("interior_high") && !isWindow;

		bool hasOrnament = isArch || isBalcony || isSpiral || isPillar || isFountain || isClock || isRailing;
		bool hasIndustrial = isTunnel || isBridge || isEnclosed || isHighWallPlain;

		weights[(int)ArchStyle.Victorian] =
			(isBalcony || isClock || isSpiral || (isArch && !isTunnel) || isRailing) ? 1.35f :
			isWindow                                                                  ? 1.10f :
			hasIndustrial                                                             ? 0.15f : 1.0f;

		weights[(int)ArchStyle.Modernist] =
			isHighWallPlain || isWalkable || isInteriorHigh ? 1.25f :
			hasOrnament && !isRailing                       ? 0.18f :
			isWindow                                        ? 1.10f : 1.0f;

		weights[(int)ArchStyle.Mediterranean] =
			(isArch && !isBridge) || isPillar || isFountain || isTunnel || isRailing ? 1.35f :
			(hasIndustrial && !isTunnel)                                              ? 0.22f : 0.95f;

		weights[(int)ArchStyle.Industrial] =
			isHighWallPlain || isEnclosed || isBridge ? 1.45f :
			isTunnel                                  ? 1.20f :
			(hasOrnament && !isRailing)               ? 0.10f :
			isWindow                                  ? 0.55f : 1.0f;

		weights[(int)ArchStyle.Brutalist] =
			isHighWallPlain || isBridge || isEnclosed ? 1.45f :
			isTunnel                                  ? 1.10f :
			hasOrnament                               ? 0.08f :
			isWindow                                  ? 0.45f : 1.0f;

		weights[(int)ArchStyle.Ornate] =
			(isArch && !isBridge) || isBalcony || isSpiral || isPillar || isFountain || isClock ? 1.55f :
			isRailing                                                                            ? 1.30f :
			isWindow                                                                             ? 1.10f :
			hasIndustrial                                                                        ? 0.05f : 0.90f;

		// Park: strongly favour open walkable ground and low structures; push down tall walls
		weights[(int)ArchStyle.Park] =
			isWalkable                            ? 1.80f :
			isFountain || isPillar                ? 1.40f :
			isRailing                             ? 1.20f :
			isHighWallPlain || isEnclosed         ? 0.12f :
			isBridge                              ? 0.20f :
			isWindow                              ? 0.80f : 1.00f;

		// Commercial: glass facades, wide interiors, walkable ground floors
		weights[(int)ArchStyle.Commercial] =
			isWindow || isInteriorHigh            ? 1.35f :
			isWalkable                            ? 1.20f :
			isHighWallPlain                       ? 1.10f :
			(isArch && !isBridge) || isBalcony    ? 0.50f :
			isSpiral || isClock                   ? 0.30f :
			hasIndustrial && !isHighWallPlain     ? 0.25f : 1.00f;

		return weights;
	}
}
