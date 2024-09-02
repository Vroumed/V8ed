using System;
using System.Collections.Generic;
using System.IO;

namespace Vroumed.V8ed.Utils.Logger;

public class Logger
{
  private readonly Dictionary<LogFile, string> _logFiles = new();

  public Logger()
  {
    // Boucle sur tous les enums pour initialiser les fichiers de log par défaut
    foreach (LogFile logFile in Enum.GetValues(typeof(LogFile)).Cast<LogFile>())
    {
      string defaultPath = Path.Combine("Serveurlog", $"{logFile}.txt");
      _logFiles[logFile] = defaultPath;
    }
  }

  // Méthode pour enregistrer un fichier de log avec un type spécifique
  public void RegisterLogFile(LogFile logName, string filePath)
  {
    if (string.IsNullOrWhiteSpace(filePath))
    {
      throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
    }

    string directoryPath = Path.GetDirectoryName(filePath);
    if (!Directory.Exists(directoryPath))
    {
      Directory.CreateDirectory(directoryPath);
    }

    _logFiles[logName] = filePath;
  }

  // Méthode pour enregistrer un message dans le fichier de log spécifié
  public void Log(LogFile logName, string message)
  {
    if (!_logFiles.TryGetValue(logName, out string? logFilePath))
    {
      Console.WriteLine($"Log file for '{logName}' not registered.");
      return;
    }

    try
    {
      using (StreamWriter writer = new(logFilePath, true))
      {
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}";
        writer.WriteLine(logEntry);
        Console.WriteLine(logEntry); // Affichage également dans la console
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Failed to write to log file '{logFilePath}': {ex.Message}");
    }
  }
}