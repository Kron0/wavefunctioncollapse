using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SlotInspector : MonoBehaviour {

	public MapBehaviour MapBehaviour;

#if UNITY_EDITOR
	[DrawGizmo(GizmoType.Selected)]
	static void DrawGizmo(SlotInspector target, GizmoType gizmoType) {
		if (target.MapBehaviour == null) {
			return;
		}
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(target.MapBehaviour.GetWorldspacePosition(target.MapBehaviour.GetMapPosition(target.transform.position)), Vector3.one * InfiniteMap.BLOCK_SIZE);
	}
#endif
}
