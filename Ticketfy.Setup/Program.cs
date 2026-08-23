using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ticketfy.Setup;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show($"Error crítico en el instalador:\n\n{ex?.Message}\n\n{ex?.StackTrace}", "Error de Instalación TICKETFY!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) =>
        {
            MessageBox.Show($"Error de hilo en el instalador:\n\n{e.Exception?.Message}\n\n{e.Exception?.StackTrace}", "Error de Instalación TICKETFY!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new InstallerForm());
    }
}

public class InstallerForm : Form
{
    private ProgressBar _progressBar;
    private Label _statusLabel;
    private Label _titleLabel;

    public InstallerForm()
    {
        this.Text = "Instalador TICKETFY!";
        this.Width = 460;
        this.Height = 220;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = System.Drawing.Color.FromArgb(15, 23, 42); // Slate dark
        this.ForeColor = System.Drawing.Color.White;

        _titleLabel = new Label
        {
            Text = "Instalando TICKETFY! POS...",
            Font = new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold),
            ForeColor = System.Drawing.Color.FromArgb(59, 130, 246), // Accent blue
            Left = 30,
            Top = 25,
            Width = 380,
            Height = 35
        };

        _statusLabel = new Label
        {
            Text = "Preparando archivos de instalación...",
            Font = new System.Drawing.Font("Segoe UI", 9),
            ForeColor = System.Drawing.Color.FromArgb(148, 163, 184), // Slate light
            Left = 30,
            Top = 70,
            Width = 380,
            Height = 25
        };

        _progressBar = new ProgressBar
        {
            Left = 30,
            Top = 105,
            Width = 380,
            Height = 22,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30
        };

        this.Controls.Add(_titleLabel);
        this.Controls.Add(_statusLabel);
        this.Controls.Add(_progressBar);

        this.Shown += async (s, e) => await StartInstallationAsync();
    }

    private async Task StartInstallationAsync()
    {
        try
        {
            UpdateStatus("Deteniendo instancias previas de TICKETFY!...");
            await Task.Run(() => KillExistingProcesses());

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string targetDir = Path.Combine(appData, "Ticketfy.Desktop", "current");
            
            // Clean old files in current target directory safely
            if (Directory.Exists(targetDir))
            {
                try { Directory.Delete(targetDir, true); } catch { }
            }
            Directory.CreateDirectory(targetDir);

            UpdateStatus("Extrayendo archivos del sistema...");
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("payload.zip");

            if (stream == null)
            {
                MessageBox.Show("Error crítico: No se encontró el paquete de instalación en el ejecutable.", "Error de Instalación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            await Task.Run(() =>
            {
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

                foreach (var entry in archive.Entries)
                {
                    string entryPath = entry.FullName.Replace('/', '\\');
                    string destinationPath = Path.GetFullPath(Path.Combine(targetDir, entryPath));

                    if (!destinationPath.StartsWith(targetDir, StringComparison.OrdinalIgnoreCase)) continue;

                    if (string.IsNullOrEmpty(entry.Name) || entryPath.EndsWith("\\"))
                    {
                        Directory.CreateDirectory(destinationPath);
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }
                }
            });

            UpdateStatus("Creando accesos directos de escritorio...");
            await Task.Run(() => CreateShortcuts(targetDir));

            UpdateStatus("¡Instalación completada con éxito! Iniciando TICKETFY!...");
            await Task.Delay(800);

            string exePath = Path.Combine(targetDir, "Ticketfy.Desktop.exe");
            if (File.Exists(exePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = targetDir,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show($"No se encontró el ejecutable principal en:\n{exePath}", "Error de Lanzamiento", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Application.Exit();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error durante la instalación:\n\n{ex.Message}\n\n{ex.StackTrace}", "Error de Instalación", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    private void UpdateStatus(string message)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => UpdateStatus(message)));
            return;
        }
        _statusLabel.Text = message;
    }

    private static void KillExistingProcesses()
    {
        try
        {
            foreach (var p in Process.GetProcessesByName("Ticketfy.Desktop"))
            {
                try { p.Kill(); p.WaitForExit(1000); } catch { }
            }
        }
        catch { }
    }

    private static void CreateShortcuts(string targetDir)
    {
        try
        {
            string exePath = Path.Combine(targetDir, "Ticketfy.Desktop.exe");
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");

            CreateShortcut(exePath, Path.Combine(desktopPath, "TICKETFY!.lnk"), targetDir);
            CreateShortcut(exePath, Path.Combine(startMenuPath, "TICKETFY!.lnk"), targetDir);
        }
        catch { }
    }

    private static void CreateShortcut(string targetExe, string shortcutPath, string workingDir)
    {
        try
        {
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;
            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell == null) return;
            var shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetExe;
            shortcut.WorkingDirectory = workingDir;
            shortcut.Description = "TICKETFY! Punto de Venta";
            shortcut.Save();
        }
        catch { }
    }
}
