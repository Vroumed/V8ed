using System;
using System.Collections.Generic;
using System.IO;

namespace Vroumed.V8ed.Utils.Logger;

public class Logger
{
  private readonly Dictionary<LogFile, string> _logFiles = new();

  public Logger()
  {
    Directory.CreateDirectory("Logs");
    foreach (LogFile logFile in Enum.GetValues(typeof(LogFile)).Cast<LogFile>())
    {
      string defaultPath = Path.Combine("Logs", $"{logFile}.txt");
      if (!File.Exists(defaultPath))
        File.CreateText(defaultPath).Close();
      _logFiles[logFile] = defaultPath;
    }
  }

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
        Console.WriteLine(logEntry);
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Failed to write to log file '{logFilePath}': {ex.Message}");
    }
  }
}