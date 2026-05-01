using System;
using UnityEngine;

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

// ── Sub-district system ───────────────────────────────────────────────────────
// Each large Voronoi district contains several smaller sub-districts, each with
// a whimsical neighbourhood identity expressed through lighting and creature sounds.

public enum SubDistrictType {
	ClockmakerQuarter = 0,  // Victorian amber lamplight, cats + mice
	InkQuarter        = 1,  // cold blue-white, crows + mice
	SaltWorks         = 2,  // pale coastal light, seagulls
	LanternRow        = 3,  // warm orange lanterns, pigeons + cat
	GreenhouseArcade  = 4,  // vivid green light, songbirds + mice
	FoundryYards      = 5,  // ember orange, dogs + cat
	EmberLanes        = 6,  // copper-red glow, cats + dog
	BoneQuarter       = 7,  // bone-cold white, crows + mice
	SpiralGardens     = 8,  // rose-gold, melodic songbirds
	VelvetQuarter     = 9,  // deep purple, cats + dove
	MossCourts        = 10, // soft green, mixed birds + cat + mouse
	FernMarket        = 11, // spring yellow-green, many birds + mouse
	GlassSpine        = 12, // ice-blue-white, pigeons + dog
}

public struct SubDistrictTheme {
	// No Name field — names are procedurally assembled from word pools each session.
	public UnityEngine.Color LightColor;
	public float  LightIntensity;
	public float  LightRange;
	public int    BirdCount;
	public int    CatCount;
	public int    DogCount;
	public int    MouseCount;
	public float  BirdPitch;  // 1.0=songbird, 0.65=crow, 0.80=pigeon, 1.30=gull
	public float  FogDensity; // exponential fog density for this neighbourhood's atmosphere

	public static readonly SubDistrictTheme[] All = new SubDistrictTheme[13] {
		// 0 ClockmakerQuarter — amber enclosed, moderate haze
		new SubDistrictTheme {
			LightColor=new UnityEngine.Color(1.00f,0.75f,0.28f), LightIntensity=0.60f, LightRange=12f,
			BirdCount=1, CatCount=2, DogCount=0, MouseCount=1, BirdPitch=0.90f, FogDensity=0.018f },
		// 1 InkQuarter — cold blue-white, slight ink-mist
		new SubDistrictTheme {
			LightColor=new UnityEngine.Color(0.72f,0.80f,1.00f), LightIntensity=0.30f, LightRange=8f,
			BirdCount=1, CatCount=0, DogCount=0, MouseCount=2, BirdPitch=0.65f, FogDensity=0.022f },
		// 2 SaltWorks — pale coastal, open sea breeze
		new SubDistrictTheme {
			LightColor=new UnityEngine.Color(0.88f,0.94f,1.00f), LightIntensity=0.40f, LightRange=16f,
			BirdCount=3, CatCount=0, DogCount=0, MouseCount=0, BirdPitch=1.30f, FogDensity=0.010f },
		// 3 LanternRow — warm orange, lively streets
		new SubDistrictTheme {
			LightColor=new UnityEngine.Color(1.00f,0.62f,0.18f), LightIntensity=0.80f, LightRange=10f,
			BirdCount=2, CatCount=1, DogCount=0, MouseCount=0, BirdPitch=0.80f, FogDensity=0.014f },
		// 4 GreenhouseArcade — bright vivid green, minimal haze
		new SubDistrictTheme {
			LightColor=new UnityEngine.Color(0.65f,1.00f,0.55f), LightIntensity=0.45f, LightRange=12f,
			BirdCount=3, CatCount=0, DogCount=0, MouseCount=1, BirdPitch=1.15f, FogDensity=0.012f },
		// 5 FoundryYards — forge smoke, thick orange haze
		new SubDistrictTheme {
			LightColor=new UnityEngine.Color(1.00f,0.48f,0.10f), LightIntensity=0.70f, LightRange=20f,
			BirdCount=0, CatCount=1, DogCount=2, MouseCount=0, BirdPitch=1.00f, FogDensity=0.032f },
		// 6 EmberLanes — copper smoulder, smoky copper air
		new SubDistrictTheme {
			LightColor=new UnityEngine.Color(0.92f,0.38f,0.20f), LightIntensity=0.55f, LightRange=10f,
			BirdCount=0, CatCount=2, DogCount=1, MouseCount=0, BirdPitch=1.00f, FogDensity=0.028f },
		// 7 BoneQuarter — bone-cold white, pale mist
		new SubDistrictTheme {
			LightColor=new UnityEngine.Color(0.92f,0.90f,0.88f), LightIntensity=0.28f, LightRange=18f,
			BirdCount=2, CatCount=0, DogCount=0, MouseCount=1, BirdPitch=0.65f, FogDensity=0.024f },
		// 8 SpiralGardens — rose-gold, morning mist
		new SubDistrictTheme {
			LightColor=new UnityEngine.Color(1.00f,0.72f,0.52f), LightIntensity=0.60f, LightRange=14f,
			BirdCount=3, CatCount=0, DogCount=0, MouseCount=0, BirdPitch=1.10f, FogDensity=0.016f },
		// 9 VelvetQuarter — deep purple, evening haze
		new SubDistrictTheme {
			LightColor=new UnityEngine.Color(0.68f,0.38f,0.92f), LightIntensity=0.50f, LightRange=12f,
			BirdCount=1, CatCount=2, DogCount=0, MouseCount=0, BirdPitch=0.80f, FogDensity=0.020f },
		// 10 MossCourts — damp green, mossy air
		new SubDistrictTheme {
			LightColor=new UnityEngine.Color(0.55f,0.88f,0.48f), LightIntensity=0.35f, LightRange=10f,
			BirdCount=2, CatCount=1, DogCount=0, MouseCount=1, BirdPitch=1.00f, FogDensity=0.022f },
		// 11 FernMarket — spring yellow-green, fresh clear air
		new SubDistrictTheme {
			LightColor=new UnityEngine.Color(0.82f,1.00f,0.38f), LightIntensity=0.45f, LightRange=12f,
			BirdCount=3, CatCount=0, DogCount=0, MouseCount=1, BirdPitch=1.20f, FogDensity=0.010f },
		// 12 GlassSpine — ice-blue, crisp arcade air
		new SubDistrictTheme {
			LightColor=new UnityEngine.Color(0.72f,0.88f,1.00f), LightIntensity=0.50f, LightRange=18f,
			BirdCount=2, CatCount=0, DogCount=1, MouseCount=0, BirdPitch=0.85f, FogDensity=0.012f },
	};

	public static SubDistrictTheme Get(int typeIndex) {
		return All[typeIndex < 0 || typeIndex >= All.Length ? 0 : typeIndex];
	}

	// ── Procedural name generation ────────────────────────────────────────────
	// Assembles a unique neighbourhood name from themed word pools.
	// Same subType always produces names with the same *feel*; nameSeed varies
	// the exact phrasing so two instances of the same theme read differently.

	public static string GenerateName(int subType, int nameSeed) {
		int idx = subType < 0 || subType >= NameAdj.Length ? 0 : subType;
		var rng = new System.Random(nameSeed);

		string adj    = NameAdj[idx][rng.Next(NameAdj[idx].Length)];
		string noun   = NameNoun[idx][rng.Next(NameNoun[idx].Length)];
		string suffix = NameSuffix[idx][rng.Next(NameSuffix[idx].Length)];

		// Five formats, weighted toward simpler forms
		int fmt = rng.Next(10);
		if      (fmt < 3) return $"The {adj} {suffix}";
		else if (fmt < 6) return $"The {noun} {suffix}";
		else if (fmt < 8) return $"{noun} {suffix}";
		else if (fmt < 9) return $"The {adj} {noun} {suffix}";
		else              return $"{adj} {suffix}";
	}

	// ── Word pools (13 themes × adjectives / nouns / suffixes) ───────────────

	private static readonly string[][] NameAdj = {
		/* 0  Clockmaker */ new[] { "Amber",   "Gilded",    "Ticking",     "Brass",    "Burnished", "Wound",    "Chiming"   },
		/* 1  Ink        */ new[] { "Ink",     "Midnight",  "Ashen",       "Blotted",  "Still",     "Pale",     "Leaden"    },
		/* 2  Salt       */ new[] { "Salt",    "Pale",      "Brine",       "Bleached", "Open",      "Windswept","White"     },
		/* 3  Lantern    */ new[] { "Lantern", "Amber",     "Painted",     "Golden",   "Candlelit", "Festive",  "Warm"      },
		/* 4  Greenhouse */ new[] { "Glazed",  "Vivid",     "Verdant",     "Fern",     "Leafy",     "Bright",   "Glasswork" },
		/* 5  Foundry    */ new[] { "Forge",   "Iron",      "Ember",       "Cinder",   "Black",     "Sooty",    "Molten"    },
		/* 6  Ember      */ new[] { "Ember",   "Copper",    "Smouldering", "Rust",     "Dusk",      "Flushed",  "Cinder"    },
		/* 7  Bone       */ new[] { "Pale",    "Bone",      "Still",       "Ash",      "Hushed",    "Quiet",    "Sallow"    },
		/* 8  Spiral     */ new[] { "Rose",    "Curving",   "Flowering",   "Gilded",   "Ornate",    "Spiral",   "Braided"   },
		/* 9  Velvet     */ new[] { "Velvet",  "Deep",      "Draped",      "Plush",    "Hushed",    "Brocade",  "Damask"    },
		/* 10 Moss       */ new[] { "Mossy",   "Damp",      "Quiet",       "Ferny",    "Overgrown", "Deep",     "Lichened"  },
		/* 11 Fern       */ new[] { "Fern",    "Spring",    "Fresh",       "Leaf",     "Open",      "Bright",   "Budding"   },
		/* 12 Glass      */ new[] { "Glass",   "Crystal",   "Clear",       "Ice",      "Mirror",    "Vaulted",  "Open"      },
	};

	private static readonly string[][] NameNoun = {
		/* 0  */ new[] { "Clockmakers'",   "Watchsmiths'",  "Tinkers'",        "Gearwrights'",  "Horologists'",    "Winders'"         },
		/* 1  */ new[] { "Scribes'",       "Printers'",     "Ravens'",         "Copyists'",     "Etchers'",        "Scholars'"        },
		/* 2  */ new[] { "Salters'",       "Mariners'",     "Lightkeepers'",   "Drifters'",     "Fishers'",        "Tideworkers'"     },
		/* 3  */ new[] { "Lanternmakers'", "Chandlers'",    "Revellers'",      "Weavers'",      "Dyers'",          "Menders'"         },
		/* 4  */ new[] { "Gardeners'",     "Glassworkers'", "Botanists'",      "Florists'",     "Growers'",        "Nurserymen's"     },
		/* 5  */ new[] { "Founders'",      "Smiths'",       "Ironworkers'",    "Casters'",      "Hammerers'",      "Furnacemen's"     },
		/* 6  */ new[] { "Braziers'",      "Coppersmiths'", "Firelighters'",   "Stokers'",      "Bellmakers'",     "Tinmen's"         },
		/* 7  */ new[] { "Stonecutters'",  "Masons'",       "Carvers'",        "Chroniclers'",  "Surveyors'",      "Antiquaries'"     },
		/* 8  */ new[] { "Gardeners'",     "Florists'",     "Sculptors'",      "Mosaicists'",   "Gilders'",        "Ornamentalists'"  },
		/* 9  */ new[] { "Dyers'",         "Drapers'",      "Tailors'",        "Haberdashers'", "Silkworkers'",    "Upholsterers'"    },
		/* 10 */ new[] { "Stoneworkers'",  "Herbalists'",   "Groundskeepers'", "Mossmen's",     "Wardens'",        "Verderers'"       },
		/* 11 */ new[] { "Sellers'",       "Traders'",      "Hawkers'",        "Marketeers'",   "Farmers'",        "Vendors'"         },
		/* 12 */ new[] { "Glaziers'",      "Merchants'",    "Dealers'",        "Factors'",      "Commissioners'",  "Brokers'"         },
	};

	private static readonly string[][] NameSuffix = {
		/* 0  */ new[] { "Quarter", "Row",     "Close",    "Lane",    "Yard",    "Gate"    },
		/* 1  */ new[] { "Quarter", "Court",   "Alley",    "Close",   "Lane",    "Passage" },
		/* 2  */ new[] { "Wharf",   "Yard",    "Row",      "Works",   "Gate",    "Dock"    },
		/* 3  */ new[] { "Row",     "Lane",    "Close",    "Parade",  "Market",  "Walk"    },
		/* 4  */ new[] { "Arcade",  "Gardens", "Walk",     "Passage", "Lane",    "Row"     },
		/* 5  */ new[] { "Yard",    "Works",   "Gate",     "Row",     "Court",   "Lane"    },
		/* 6  */ new[] { "Lane",    "Alley",   "Row",      "Cut",     "Close",   "Way"     },
		/* 7  */ new[] { "Quarter", "Court",   "Close",    "Place",   "Yard",    "Passage" },
		/* 8  */ new[] { "Gardens", "Walk",    "Terrace",  "Row",     "Green",   "Close"   },
		/* 9  */ new[] { "Quarter", "Row",     "Passage",  "Lane",    "Court",   "Arcade"  },
		/* 10 */ new[] { "Court",   "Yard",    "Close",    "Green",   "Place",   "Mews"    },
		/* 11 */ new[] { "Market",  "Row",     "Green",    "Square",  "Parade",  "Place"   },
		/* 12 */ new[] { "Arcade",  "Row",     "Walk",     "Passage", "Street",  "Place"   },
	};
}

// ── Main district bias provider ───────────────────────────────────────────────
// Thread-safe static provider — written from the main thread by DistrictStyler,
// read from the generation background thread by createChunk.
public static class DistrictBiasProvider {
	public struct SeedData {
		public float x, z;
		public int style; // cast of ArchStyle
	}

	public struct SubSeedData {
		public float x, z;
		public int subDistrictType; // cast of SubDistrictType
		public int nameSeed;        // drives procedural name generation — unique per sub-seed instance
	}

	private static volatile SeedData[]    seeds;
	private static volatile SubSeedData[] subSeeds;

	public static void SetSeeds(SeedData[] s) { seeds = s; }
	public static void SetSubSeeds(SubSeedData[] s) { subSeeds = s; }

	// Called from main thread by ChunkAtmosphere4D — safe to call from Update.
	public static int GetSubDistrictType(float worldX, float worldZ) {
		var s = subSeeds;
		if (s == null || s.Length == 0) return 0;
		float bestDist = float.MaxValue;
		int   bestIdx  = 0;
		for (int i = 0; i < s.Length; i++) {
			float dx = worldX - s[i].x;
			float dz = worldZ - s[i].z;
			float d  = dx * dx + dz * dz;
			if (d < bestDist) { bestDist = d; bestIdx = i; }
		}
		return s[bestIdx].subDistrictType;
	}

	// Called from main thread — safe to call from Update or OnGUI.
	public static string GetSubDistrictName(float slotX, float slotZ) {
		var s = subSeeds;
		if (s == null || s.Length == 0) return "";
		float bestDist = float.MaxValue;
		int   bestIdx  = 0;
		for (int i = 0; i < s.Length; i++) {
			float dx = slotX - s[i].x;
			float dz = slotZ - s[i].z;
			float d  = dx * dx + dz * dz;
			if (d < bestDist) { bestDist = d; bestIdx = i; }
		}
		return SubDistrictTheme.GenerateName(s[bestIdx].subDistrictType, s[bestIdx].nameSeed);
	}

	// Smooth positional blend between the two nearest sub-districts.
	// slotX/slotZ are in slot coordinates (world position / BLOCK_SIZE).
	// t=0 means fully in the nearest district; t=1 means on the boundary.
	public struct SubDistrictBlend {
		public SubDistrictTheme near;
		public SubDistrictTheme far;
		public int nearIndex; // SubDistrictType of the dominant seed
		public float t;       // 0 = fully near, 1 = fully far (only nonzero near boundary)
	}

	public static SubDistrictBlend GetSubDistrictBlend(float slotX, float slotZ) {
		var s = subSeeds;
		var fallback = SubDistrictTheme.Get(0);
		if (s == null || s.Length < 2) {
			return new SubDistrictBlend { near = fallback, far = fallback, nearIndex = 0, t = 0f };
		}
		float best1 = float.MaxValue, best2 = float.MaxValue;
		int   idx1  = 0,              idx2  = 0;
		for (int i = 0; i < s.Length; i++) {
			float dx = slotX - s[i].x;
			float dz = slotZ - s[i].z;
			float d  = dx * dx + dz * dz;
			if (d < best1) { best2 = best1; idx2 = idx1; best1 = d; idx1 = i; }
			else if (d < best2) { best2 = d; idx2 = i; }
		}
		float r1    = Mathf.Sqrt(best1);
		float r2    = Mathf.Sqrt(best2);
		float rawT  = r1 / (r1 + r2 + 0.0001f); // 0 at nearest seed, 0.5 at boundary
		// Start blending at 30% of the way to boundary, complete at 50%
		float blendT = Mathf.Clamp01((rawT - 0.30f) / 0.20f);
		blendT = blendT * blendT * (3f - 2f * blendT); // smoothstep
		return new SubDistrictBlend {
			near      = SubDistrictTheme.Get(s[idx1].subDistrictType),
			far       = SubDistrictTheme.Get(s[idx2].subDistrictType),
			nearIndex = s[idx1].subDistrictType,
			t         = blendT,
		};
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
