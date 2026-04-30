using UnityEngine;
using UnityEditor;

public class FourDSetupWizard {
	[MenuItem("Wave Function Collapse/Setup 4D Scene")]
	public static void SetupFourDScene() {
		var map4D = SetupMap4DObject();
		var player = SetupPlayer();
		WireReferences(map4D, player);
		EditorUtility.SetDirty(map4D);
		EditorUtility.SetDirty(player);
		UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
			UnityEngine.SceneManagement.SceneManager.GetActiveScene());
		Debug.Log("4D scene setup complete. Save the scene to keep the changes.");
	}

	private static GameObject SetupMap4DObject() {
		// Reuse existing Map object if present, otherwise create one
		var existing = GameObject.Find("Map4D");
		var go = existing != null ? existing : new GameObject("Map4D");

		EnsureComponent<MapBehaviour4D>(go, mb4d => {
			mb4d.MapHeight = 6;

			// Copy ModuleData reference from the 3D MapBehaviour if present
			var mb3d = Object.FindObjectOfType<MapBehaviour>();
			if (mb3d != null && mb4d.ModuleData == null) {
				mb4d.ModuleData = mb3d.ModuleData;
			}
		});

		EnsureComponent<TesseractProjection>(go, proj => {
			proj.ViewDistance = 10f;
			proj.WScale = 2f;
			proj.WRenderRange = 3;
			proj.MinAlpha = 0.1f;
		});

		EnsureComponent<GenerateMap4DNearPlayer>(go, gen => {
			gen.ChunkSize = 4;
			gen.Range = 30f;
		});

		EnsureComponent<LandmarkPlacer>(go);
		EnsureComponent<CollectiblePlacer>(go);
		EnsureComponent<WGatePlacer>(go);

		return go;
	}

	private static GameObject SetupPlayer() {
		// Find existing Player or create one
		var existing = GameObject.Find("Player");
		GameObject playerGO;

		if (existing != null) {
			playerGO = existing;
		} else {
			playerGO = new GameObject("Player4D");
			playerGO.tag = "Player";
			var cc = playerGO.AddComponent<CharacterController>();
			cc.height = 1.8f;
			cc.radius = 0.4f;
			cc.center = new Vector3(0, 0.9f, 0);

			// Camera child
			var camGO = new GameObject("Camera");
			camGO.transform.SetParent(playerGO.transform);
			camGO.transform.localPosition = new Vector3(0, 1.6f, 0);
			camGO.AddComponent<Camera>();
			camGO.AddComponent<AudioListener>();
			camGO.tag = "MainCamera";
		}

		// Replace FirstPersonController with FourDController if needed
		var fpc = playerGO.GetComponent<FirstPersonController>();
		FourDController fdc4D;
		if (fpc != null && playerGO.GetComponent<FourDController>() == null) {
			float speed = fpc.MovementSpeed;
			Object.DestroyImmediate(fpc);
			fdc4D = playerGO.AddComponent<FourDController>();
			fdc4D.MovementSpeed = speed;
			Debug.Log("Replaced FirstPersonController with FourDController on " + playerGO.name + ".");
		} else {
			fdc4D = EnsureComponent<FourDController>(playerGO, fdc => {
				fdc.MovementSpeed = 1f;
				fdc.WSpeed = 1.5f;
			});
		}

		// Rewire ControlModeSwitcher to reference FourDController instead of the old controller
		var switcher = playerGO.GetComponent<ControlModeSwitcher>();
		if (switcher != null && fdc4D != null) {
			foreach (var mode in switcher.ControlModes) {
				if (mode.Behaviour == null || mode.Behaviour is FirstPersonController) {
					mode.Behaviour = fdc4D;
				}
			}
			EditorUtility.SetDirty(switcher);
		}

		EnsureComponent<WLayerHUD>(playerGO, hud => {
			hud.player = fdc4D;
		});
		EnsureComponent<WTransitionEffect>(playerGO);
		EnsureComponent<WSkyboxController>(playerGO, sky => {
			sky.player = fdc4D;
		});
		EnsureComponent<PauseMenu>(playerGO);
		EnsureComponent<ProceduralAmbience>(playerGO);

		return playerGO;
	}

	private static void WireReferences(GameObject map4DGO, GameObject playerGO) {
		var gen = map4DGO.GetComponent<GenerateMap4DNearPlayer>();
		if (gen != null && gen.Target == null) {
			gen.Target = playerGO.transform;
		}

		// Position player at a sensible starting point above the map
		if (playerGO.transform.position == Vector3.zero) {
			playerGO.transform.position = new Vector3(0, 6f, 0);
		}

		// Disable the 3D map GameObject if present (avoid running both at once)
		var map3D = GameObject.Find("Map");
		if (map3D != null && map3D != map4DGO) {
			bool wasActive = map3D.activeSelf;
			map3D.SetActive(false);
			if (wasActive) {
				Debug.Log("Disabled 3D Map GameObject. Re-enable it to switch back to 3D mode.");
			}
		}

		// Disable 3D GenerateMapNearPlayer if present
		var gen3d = Object.FindObjectOfType<GenerateMapNearPlayer>();
		if (gen3d != null && gen3d.gameObject != map4DGO) {
			gen3d.enabled = false;
			Debug.Log("Disabled GenerateMapNearPlayer on 3D map.");
		}
	}

	private static T EnsureComponent<T>(GameObject go, System.Action<T> configure = null) where T : Component {
		var component = go.GetComponent<T>();
		bool isNew = component == null;
		if (isNew) {
			component = go.AddComponent<T>();
		}
		if (isNew || configure != null) {
			configure?.Invoke(component);
		}
		return component;
	}

	[MenuItem("Wave Function Collapse/Setup 4D Scene", true)]
	private static bool ValidateSetup() {
		return !Application.isPlaying;
	}
}
