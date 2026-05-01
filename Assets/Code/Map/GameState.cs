// Shared game-state flags. Lives in WFC.Map so every layer can read/write
// without creating cross-assembly circular dependencies.
public static class GameState {
    public static bool IsPaused       { get; set; }
    public static bool StartupComplete { get; set; }
    public static int  NumWLayers      { get; set; } = 6;
    public static int  BuiltSlotCount  { get; set; }
}
