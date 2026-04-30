using System.Collections.Generic;

public class HistoryItem4D {
	public Dictionary<Vector4Int, ModuleSet> RemovedModules;

	public readonly Slot4D Slot;

	public HistoryItem4D(Slot4D slot) {
		this.RemovedModules = new Dictionary<Vector4Int, ModuleSet>();
		this.Slot = slot;
	}
}
