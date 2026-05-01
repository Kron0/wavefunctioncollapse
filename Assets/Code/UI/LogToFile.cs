using System;
using System.IO;
using UnityEngine;

// Attach to any persistent GameObject. Mirrors all Debug.Log output to a file
// next to the project (in editor) or next to the executable (in builds).
// Toggle SaveToFile in the Inspector to disable without removing the component.
public class LogToFile : MonoBehaviour {

	[Tooltip("Write all Unity console output to game_debug.log")]
	public bool SaveToFile = true;

	private StreamWriter writer;
	private string logPath;

	void OnEnable() {
		this.logPath = Application.isEditor
			? Path.Combine(Application.dataPath, "../game_debug.log")
			: Path.Combine(Application.persistentDataPath, "game_debug.log");

		if (!this.SaveToFile) return;

		try {
			this.writer = new StreamWriter(this.logPath, append: false) { AutoFlush = true };
			this.writer.WriteLine($"=== Session started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
			this.writer.WriteLine($"    Unity {Application.unityVersion}  |  {SystemInfo.operatingSystem}");
			this.writer.WriteLine($"    Log path: {Path.GetFullPath(this.logPath)}");
			this.writer.WriteLine();
			Application.logMessageReceived += this.HandleLog;
			Debug.Log($"[LogToFile] Writing to: {Path.GetFullPath(this.logPath)}");
		} catch (Exception e) {
			Debug.LogWarning($"[LogToFile] Could not open log file: {e.Message}");
		}
	}

	void OnDisable() {
		Application.logMessageReceived -= this.HandleLog;
		if (this.writer != null) {
			this.writer.WriteLine($"\n=== Session ended {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
			this.writer.Close();
			this.writer = null;
		}
	}

	private void HandleLog(string message, string stackTrace, LogType type) {
		if (this.writer == null) return;
		string prefix = type switch {
			LogType.Error     => "[ERR ] ",
			LogType.Assert    => "[ASRT] ",
			LogType.Warning   => "[WARN] ",
			LogType.Exception => "[EXCP] ",
			_                 => "[LOG ] ",
		};
		string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
		this.writer.WriteLine($"[{timestamp}] {prefix}{message}");
		if (type == LogType.Error || type == LogType.Exception) {
			this.writer.WriteLine($"           {stackTrace?.Replace("\n", "\n           ")}");
		}
	}
}
