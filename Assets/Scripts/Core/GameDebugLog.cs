using System;
using System.IO;
using UnityEngine;

namespace Labyrinth.Core
{
    public static class GameDebugLog
    {
        private const string Prefix = "[Labyrinth]";
        private const string FileName = "debug.log";

        private static bool fileInitialized;
        private static string filePath;

        public static void Info(string category, string message)
        {
            var formatted = Format(category, message);
            Debug.Log(formatted);
            WriteToFile("INFO", formatted);
        }

        public static void Warning(string category, string message)
        {
            var formatted = Format(category, message);
            Debug.LogWarning(formatted);
            WriteToFile("WARN", formatted);
        }

        public static void Error(string category, string message)
        {
            var formatted = Format(category, message);
            Debug.LogError(formatted);
            WriteToFile("ERROR", formatted);
        }

        public static string Position(Vector2Int position)
        {
            return $"({position.x}, {position.y})";
        }

        private static string Format(string category, string message)
        {
            return $"{Prefix}[{category}] {message}";
        }

        private static void WriteToFile(string level, string formattedMessage)
        {
            if (!EnsureFileInitialized())
            {
                return;
            }

            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                File.AppendAllText(filePath, $"{timestamp} [{level}] {formattedMessage}{Environment.NewLine}");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{Prefix}[Log] Failed to write {FileName}: {exception.Message}");
            }
        }

        private static bool EnsureFileInitialized()
        {
            if (fileInitialized)
            {
                return !string.IsNullOrEmpty(filePath);
            }

            fileInitialized = true;
            try
            {
                filePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", FileName));
                var header = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [INFO] {Prefix}[Log] debug.log started at {filePath}{Environment.NewLine}";
                File.WriteAllText(filePath, header);
                return true;
            }
            catch (Exception exception)
            {
                filePath = string.Empty;
                Debug.LogWarning($"{Prefix}[Log] Failed to initialize {FileName}: {exception.Message}");
                return false;
            }
        }
    }
}
