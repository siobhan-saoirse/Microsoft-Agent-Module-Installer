using G64Installer;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

internal static class EmbeddedExeRunner
{
    internal static void ExtractEmbeddedFile(string resourceName, string outputPath)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
                throw new Exception($"Resource {resourceName} not found!");

            using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(fs);
            }
        }
    }
    internal static void RunEmbeddedExe(string exeName, string arguments = "")
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), "MSAgentModuleInstaller");
        Directory.CreateDirectory(tempFolder);

        string exePath = Path.Combine(tempFolder, exeName);

        Assembly assembly = typeof(Installer).Assembly;
        // Find resource name that ends with the requested exe name (case-insensitive)
        string resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(exeName, StringComparison.OrdinalIgnoreCase));

        if (resourceName == null)
        {
            // Helpful debug: list available resources
            string available = string.Join(", ", assembly.GetManifestResourceNames());
            throw new Exception($"Embedded EXE not found (searched for '{exeName}'). Available resources: {available}");
        }

        // Extract the found resource to disk
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            using (var fs = new FileStream(exePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.CopyTo(fs);
            }
        }

        // Run it as Administrator
        ProcessStartInfo psi = new ProcessStartInfo(exePath, arguments)
        {
            UseShellExecute = true,
            WorkingDirectory = tempFolder,
            Verb = "runas" // <-- This requests Admin privileges
        };

        try
        {
            Process.Start(psi);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            // This exception occurs if the user cancels the UAC prompt
            Console.WriteLine("The operation requires admin privileges: " + ex.Message);
        }
    }

    internal static void SetNt4Sp5Compatibility(string exePath)
    {
        // Registry key for current user app compatibility
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
            @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers",
            true))
        {
            if (key == null)
                throw new Exception("Failed to open AppCompatFlags registry key.");

            key.SetValue(exePath, "WINNT4SP5", RegistryValueKind.String);
        }
    }
}