using System;
using System.IO;

namespace AgendamentoWpfApp.Services;

internal static class SparkCoreRuntimePaths
{
    public static string DataDirectory
    {
        get
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RetaguardaAgendamento");

            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    public static string LogsDirectory
    {
        get
        {
            var folder = Path.Combine(DataDirectory, "logs");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }
}
