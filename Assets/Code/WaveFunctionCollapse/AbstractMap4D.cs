using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public abstract class AbstractMap4D {
	public const float BLOCK_SIZE = 6f;
	public const int HISTORY_SIZE = 3000;

	public static System.Random Random;

	public readonly RingBuffer<HistoryItem4D> History;
	public readonly QueueDictionary<Vector4Int, ModuleSet> RemovalQueue;
	private HashSet<Slot4D> workArea;
	public readonly ConcurrentQueue<Slot4D> BuildQueue;

	private int backtrackBarrier;
	private int backtrackAmount = 0;

	public readonly short[][] InitialModuleHealth;

	public AbstractMap4D() {
		AbstractMap4D.Random = new System.Random();

		this.History = new RingBuffer<HistoryItem4D>(AbstractMap4D.HISTORY_SIZE);
		this.History.OnOverflow = item => item.Slot.Forget();
		this.RemovalQueue = new QueueDictionary<Vector4Int, ModuleSet>(() => new ModuleSet());
		this.BuildQueue = new ConcurrentQueue<Slot4D>();

		this.InitialModuleHealth = this.createInitialModuleHealth(ModuleData.Current);

		this.backtrackBarrier = 0;
	}

	public abstract Slot4D GetSlot(Vector4Int position);

	public abstract IEnumerable<Slot4D> GetAllSlots();

	public abstract void ApplyBoundaryConstraints(IEnumerable<BoundaryConstraint> constraints);

	public void NotifySlotCollapsed(Slot4D slot) {
		if (this.workArea != null) {
			this.workArea.Remove(slot);
		}
		this.BuildQueue.Enqueue(slot);
	}

	public void NotifySlotCollapseUndone(Slot4D slot) {
		if (this.workArea != null) {
			this.workArea.Add(slot);
		}
	}

	public void FinishRemovalQueue() {
		while (this.RemovalQueue.Any()) {
			var kvp = this.RemovalQueue.Dequeue();
			var slot = this.GetSlot(kvp.Key);
			if (!slot.Collapsed) {
				slot.RemoveModules(kvp.Value, false);
			}
		}
	}

	public void Collapse(IEnumerable<Vector4Int> targets, bool showProgress = false) {
#if UNITY_EDITOR
		try {
#endif
			this.RemovalQueue.Clear();
			this.workArea = new HashSet<Slot4D>(targets.Select(target => this.GetSlot(target)).Where(slot => slot != null && !slot.Collapsed));

			while (this.workArea.Any()) {
				float minEntropy = float.PositiveInfinity;
				Slot4D selected = null;

				foreach (var slot in workArea) {
					float entropy = slot.Modules.Entropy;
					if (entropy < minEntropy) {
						selected = slot;
						minEntropy = entropy;
					}
				}
				try {
					selected.CollapseRandom();
				}
				catch (CollapseFailedException4D) {
					this.RemovalQueue.Clear();
					if (this.History.TotalCount > this.backtrackBarrier) {
						this.backtrackBarrier = this.History.TotalCount;
						this.backtrackAmount = 2;
					} else {
						this.backtrackAmount *= 2;
					}
					if (this.backtrackAmount > 0) {
						Debug.Log(this.History.Count + " Backtracking " + this.backtrackAmount + " steps...");
					}
					this.Undo(this.backtrackAmount);
				}

#if UNITY_EDITOR
				if (showProgress && this.workArea.Count % 20 == 0) {
					if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("Collapsing 4D area... ", this.workArea.Count + " left...", 1f - (float)this.workArea.Count() / targets.Count())) {
						UnityEditor.EditorUtility.ClearProgressBar();
						throw new Exception("Map generation cancelled.");
					}
				}
#endif
			}

#if UNITY_EDITOR
			if (showProgress) {
				UnityEditor.EditorUtility.ClearProgressBar();
			}
		}
		catch (Exception exception) {
			if (showProgress) {
				UnityEditor.EditorUtility.ClearProgressBar();
			}
			Debug.LogWarning("Exception in 4D world generation thread at" + exception.StackTrace);
			throw exception;
		}
#endif
	}

	public void Collapse(Vector4Int start, Vector4Int size, bool showProgress = false) {
		var targets = new List<Vector4Int>();
		for (int x = 0; x < size.x; x++) {
			for (int y = 0; y < size.y; y++) {
				for (int z = 0; z < size.z; z++) {
					for (int w = 0; w < size.w; w++) {
						targets.Add(start + new Vector4Int(x, y, z, w));
					}
				}
			}
		}
		this.Collapse(targets, showProgress);
	}

	public void Undo(int steps) {
		while (steps > 0 && this.History.Any()) {
			var item = this.History.Pop();

			foreach (var slotAddress in item.RemovedModules.Keys) {
				this.GetSlot(slotAddress).AddModules(item.RemovedModules[slotAddress]);
			}

			item.Slot.Module = null;
			this.NotifySlotCollapseUndone(item.Slot);
			steps--;
		}
		if (this.History.Count == 0) {
			this.backtrackBarrier = 0;
		}
	}

	private short[][] createInitialModuleHealth(Module[] modules) {
		var initialModuleHealth = new short[8][];
		for (int i = 0; i < 8; i++) {
			initialModuleHealth[i] = new short[modules.Length];
			foreach (var module in modules) {
				int opposite = Orientations4D.Opposite(i);
				foreach (var possibleNeighbor in module.PossibleNeighbors4D[opposite]) {
					initialModuleHealth[i][possibleNeighbor.Index]++;
				}
			}
		}

#if UNITY_EDITOR
		for (int i = 0; i < modules.Length; i++) {
			for (int d = 0; d < 8; d++) {
				if (initialModuleHealth[d][i] == 0) {
					Debug.LogError("Module " + modules[i].Name + " cannot be reached from 4D direction " + d + " (" + Orientations4D.Names[d] + ")!", modules[i].Prefab);
					throw new Exception("Unreachable module in 4D.");
				}
			}
		}
#endif

		return initialModuleHealth;
	}

	public short[][] CopyInitialModuleHealth() {
		return this.InitialModuleHealth.Select(a => a.ToArray()).ToArray();
	}
}
