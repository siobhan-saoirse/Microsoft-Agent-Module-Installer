using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

internal static class RegHelper
{
    /// <summary>
    /// Extracts an embedded .reg file and applies it silently
    /// </summary>
    /// <param name="regFileName">The name of the embedded .reg file (e.g., "SetAgentCompat.reg")</param>
    public static void ApplyEmbeddedRegFile(string regFileName)
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), "MSAgentModuleInstaller");
        Directory.CreateDirectory(tempFolder);

        string regPath = Path.Combine(tempFolder, regFileName);

        // Get the assembly
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Find the embedded resource
        string resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(regFileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName == null)
        {
            string available = string.Join(", ", assembly.GetManifestResourceNames());
            throw new Exception($"Embedded .reg file not found. Available: {available}");
        }

        // Extract to temp
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        using (var fs = new FileStream(regPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.CopyTo(fs);
        }

        // Apply silently
        ProcessStartInfo psi = new ProcessStartInfo("regedit.exe")
        {
            Arguments = $"/s \"{regPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            Process process = Process.Start(psi);
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to apply .reg file: " + ex.Message);
        }
        finally
        {
            // Optionally delete temp file
            if (File.Exists(regPath))
                File.Delete(regPath);
        }
    }
}
