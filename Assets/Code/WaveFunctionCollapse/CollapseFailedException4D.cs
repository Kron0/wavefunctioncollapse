using System;

public class CollapseFailedException4D : Exception {
	public readonly Slot4D Slot;

	public CollapseFailedException4D(Slot4D slot) {
		this.Slot = slot;
	}
}
