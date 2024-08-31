using System;
using System.Collections.Generic;
using System.IO;

namespace Vroumed.V8ed.Utils;

public static class Logger
{
  // Dictionnaire pour stocker les chemins des fichiers de log
  private static readonly Dictionary<string, string> _logFiles = new();

  // Méthode pour enregistrer un fichier de log avec un nom spécifique
  public static void RegisterLogFile(string logName, string filePath)
  {
    if (string.IsNullOrWhiteSpace(logName))
    {
      throw new ArgumentException("Log name cannot be null or empty.", nameof(logName));
    }

    if (string.IsNullOrWhiteSpace(filePath))
    {
      throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
    }

    if (!_logFiles.ContainsKey(logName))
    {
      // Vérifie si le dossier existe, sinon le crée
      string directoryPath = Path.GetDirectoryName(filePath);
      if (!Directory.Exists(directoryPath))
      {
        Directory.CreateDirectory(directoryPath);
      }

      _logFiles.Add(logName, filePath);
    }
  }

  // Méthode pour enregistrer un message dans le fichier de log spécifié
  public static void Log(string logName, string message)
  {
    if (string.IsNullOrWhiteSpace(logName))
    {
      Console.WriteLine("Log name cannot be null or empty.");
      return;
    }

    if (!_logFiles.TryGetValue(logName, out string? logFilePath))
    {
      Console.WriteLine($"Log file for '{logName}' not registered.");
      return;
    }

    try
    {
      using (StreamWriter writer = new(logFilePath, true))
      {
        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Failed to write to log file '{logFilePath}': {ex.Message}");
    }
  }
}