// Công cụ cài đặt gói ngôn ngữ tiếng Việt cho AION2 Global.
// Build bằng trình biên dịch .NET Framework có sẵn trên Windows (xem build.bat); dữ liệu gói ngôn ngữ được nhúng trong exe.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

// Thông tin phiên bản: metadata đầy đủ giúp giảm nguy cơ bị antivirus nhận nhầm là dropper dùng một lần, đồng thời cần thiết cho việc ký mã.
// Release workflow dùng tên tag (v1.2.3 → 1.2.3.0) để ghi đè hai số phiên bản bên dưới.
[assembly: AssemblyTitle("AION2_VIE - Công Cụ Cài Đặt Tiếng Việt")]
[assembly: AssemblyDescription("AION2 Global (Steam / PURPLE) - Công Cụ Cài Đặt Tiếng Việt")]
[assembly: AssemblyProduct("AION2_VIE · Gói Ngôn Ngữ Tiếng Việt")]
[assembly: AssemblyCompany("Steam-AION2-Vietnamese")]
[assembly: AssemblyCopyright("Copyright © Monohitoxx")]
[assembly: AssemblyVersion("1.0.1.0")]
[assembly: AssemblyFileVersion("1.0.1.0")]

static class Patch
{
    public const string PackVersion = "Đồng bộ phiên bản ngày 11/11";
    const string SteamAppId = "3393110";
    const string EnPakDir = @"Aion2\Content\Paks\L10N\Text\en-US";
    const string EnPakName = "pakchunk502000-Windows_0_P";
    const string PakPath = @"Aion2\Content\Paks\vietnamese.pak";
    const string BackupDir = "_AION2_VIE_Backup";
    const string FontName = "Aion2Atreia_EN_Bold.ufont";
    const string FontMd5 = "596dc9b2f792c524ef4245acc26522af";
    const string FontUrl = "https://raw.githubusercontent.com/yuezhang128/Steam-AION2-Vietnamese/main/langpack/Aion2Atreia_EN_Bold.ufont";
    static readonly string[] PakExts = { ".pak", ".sig", ".ucas", ".utoc" };
    static readonly string[] GameProcesses = { "AION2", "Aion2", "Aion2-Win64-Shipping" };

    public enum State { NoGame, NotInstalled, Installed, NeedsReinstall, OtherPatch }

    public static bool IsGameDir(string dir)
    {
        return !string.IsNullOrEmpty(dir) && Directory.Exists(Path.Combine(dir, @"Aion2\Content\Paks"));
    }

    // Người dùng có thể chọn thư mục con của game; tìm ngược lên để xác định thư mục gốc.
    public static string ResolveGameDir(string dir)
    {
        try
        {
            for (int i = 0; i < 5 && !string.IsNullOrEmpty(dir); i++)
            {
                if (IsGameDir(dir)) return dir.TrimEnd('\\');
                dir = Path.GetDirectoryName(dir.TrimEnd('\\'));
            }
        }
        catch (ArgumentException) { }
        return null;
    }

    public static string FindGameDir()
    {
        var libraries = new List<string>();
        foreach (var key in new[] { @"HKEY_CURRENT_USER\Software\Valve\Steam", @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam" })
        {
            foreach (var name in new[] { "SteamPath", "InstallPath" })
            {
                var steam = Registry.GetValue(key, name, null) as string;
                if (string.IsNullOrEmpty(steam)) continue;
                steam = steam.Replace('/', '\\');
                libraries.Add(steam);
                var vdf = Path.Combine(steam, @"steamapps\libraryfolders.vdf");
                if (!File.Exists(vdf)) continue;
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                    libraries.Add(m.Groups[1].Value.Replace(@"\\", @"\"));
            }
        }
        foreach (var lib in libraries)
        {
            try
            {
                var acf = Path.Combine(lib, @"steamapps\appmanifest_" + SteamAppId + ".acf");
                if (!File.Exists(acf)) continue;
                var m = Regex.Match(File.ReadAllText(acf), "\"installdir\"\\s+\"([^\"]+)\"");
                var dir = Path.Combine(lib, @"steamapps\common", m.Success ? m.Groups[1].Value : "AION2");
                if (IsGameDir(dir)) return dir;
            }
            catch (IOException) { }
        }
        return null;
    }

    // ---- Tự động tìm thư mục game ----
    // Bản Steam thường nằm ở x:\SteamLibrary\steamapps\common\AION2, bản PURPLE thường nằm ở x:\AION2.
    // Cả hai đều dùng tên thư mục AION2 viết hoa, nên tìm thư mục tên AION2 có chứa Aion2\Content\Paks.

    static List<string> DriveRoots()
    {
        var roots = new List<string>();
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                try
                {
                    if (!d.IsReady) continue;
                    if (d.DriveType != DriveType.Fixed && d.DriveType != DriveType.Removable) continue;
                    roots.Add(d.RootDirectory.FullName.TrimEnd('\\') + "\\");
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
        catch (IOException) { }
        return roots;
    }

    static void AddDir(List<string> list, string dir)
    {
        if (!IsGameDir(dir)) return;
        var full = dir.TrimEnd('\\');
        foreach (var d in list)
            if (string.Equals(d, full, StringComparison.OrdinalIgnoreCase)) return;
        list.Add(full);
    }

    // Kiểm tra nhanh: Registry của Steam và các vị trí phổ biến trên từng ổ đĩa (không quét toàn bộ ổ).
    public static List<string> QuickGameDirs()
    {
        var list = new List<string>();
        AddDir(list, FindGameDir());
        foreach (var root in DriveRoots())
        {
            AddDir(list, Path.Combine(root, "AION2"));
            AddDir(list, Path.Combine(root, @"SteamLibrary\steamapps\common\AION2"));
            AddDir(list, Path.Combine(root, @"Steam\steamapps\common\AION2"));
            AddDir(list, Path.Combine(root, @"Program Files (x86)\Steam\steamapps\common\AION2"));
            AddDir(list, Path.Combine(root, @"Program Files\Steam\steamapps\common\AION2"));
            AddDir(list, Path.Combine(root, @"Program Files (x86)\NC\AION2"));
            AddDir(list, Path.Combine(root, @"Program Files\NC\AION2"));
            AddDir(list, Path.Combine(root, @"Program Files (x86)\NCSOFT\AION2"));
            AddDir(list, Path.Combine(root, @"Program Files\NCSOFT\AION2"));
        }
        return list;
    }

    const int ScanMaxDepth = 5;
    const int ScanMaxResults = 12;
    static readonly string[] ScanSkipNames = {
        "windows", "$recycle.bin", "system volume information", "programdata", "recovery", "perflogs",
        "msocache", "config.msi", "appdata", "node_modules", ".git", ".svn", "temp", "tmp", "onedrive",
        "$windows.~bt", "$windows.~ws"
    };

    static bool IsSkipName(string name)
    {
        var lower = name.ToLowerInvariant();
        foreach (var s in ScanSkipNames)
            if (s == lower) return true;
        return false;
    }

    // Quét tất cả ổ đĩa cục bộ (tối đa 5 cấp), trả về các thư mục AION2 hợp lệ.
    public static List<string> ScanGameDirs(Action<string> progress, Func<bool> cancelled)
    {
        var found = new List<string>();
        int visited = 0;
        foreach (var root in DriveRoots())
        {
            if (cancelled != null && cancelled()) break;
            ScanTree(root, ScanMaxDepth, found, progress, cancelled, ref visited);
        }
        found.Sort(delegate(string a, string b)
        {
            int c = a.Length.CompareTo(b.Length);
            return c != 0 ? c : string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        });
        return found;
    }

    static void ScanTree(string root, int maxDepth, List<string> found, Action<string> progress, Func<bool> cancelled, ref int visited)
    {
        var queue = new Queue<KeyValuePair<string, int>>();
        queue.Enqueue(new KeyValuePair<string, int>(root, 0));
        while (queue.Count > 0)
        {
            if (cancelled != null && cancelled()) return;
            if (found.Count >= ScanMaxResults) return;
            var node = queue.Dequeue();
            string[] subs;
            try { subs = Directory.GetDirectories(node.Key); }
            catch (UnauthorizedAccessException) { continue; }
            catch (IOException) { continue; }
            visited++;
            if (progress != null && visited % 128 == 0) progress(node.Key);
            foreach (var sub in subs)
            {
                string name;
                try { name = Path.GetFileName(sub); }
                catch (ArgumentException) { continue; }
                if (string.IsNullOrEmpty(name)) continue;
                if (name.Equals("AION2", StringComparison.OrdinalIgnoreCase))
                {
                    if (IsGameDir(sub)) AddDir(found, sub);
                    continue; // Không cần tìm sâu hơn trong thư mục game.
                }
                if (node.Value + 1 >= maxDepth) continue;
                if (IsSkipName(name)) continue;
                try { if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) != 0) continue; }
                catch (IOException) { continue; }
                catch (UnauthorizedAccessException) { continue; }
                queue.Enqueue(new KeyValuePair<string, int>(sub, node.Value + 1));
            }
        }
    }

    public static bool GameRunning()
    {
        foreach (var name in GameProcesses)
            if (Process.GetProcessesByName(name).Length > 0) return true;
        return false;
    }

    static readonly Lazy<byte[]> GeneratedPack = new Lazy<byte[]>(delegate { return PakBuilder.Build(null); });

    static byte[] PackData() { return GeneratedPack.Value; }

    // Ưu tiên font cạnh exe; nếu chưa có thì tải về.
    static string LocalFont()
    {
        var exeDir = Path.GetDirectoryName(Application.ExecutablePath);
        foreach (var path in new[] { Path.Combine(exeDir, FontName), Path.Combine(exeDir, "langpack", FontName) })
            if (File.Exists(path) && Md5File(path) == FontMd5) return path;
        return null;
    }

    static string DownloadFont(Action<int, string> progress)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "AION2_VIE_" + FontName);
        if (File.Exists(tmp) && Md5File(tmp) == FontMd5) return tmp;
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
        var req = (HttpWebRequest)WebRequest.Create(FontUrl);
        req.Timeout = 30000;
        using (var resp = req.GetResponse())
        using (var src = resp.GetResponseStream())
        using (var dst = File.Create(tmp))
        {
            long total = resp.ContentLength, done = 0;
            var buf = new byte[81920];
            int n;
            while ((n = src.Read(buf, 0, buf.Length)) > 0)
            {
                dst.Write(buf, 0, n);
                done += n;
                if (total > 0) progress((int)(done * 100 / total), "Đang tải font… " + (done >> 10) + " / " + (total >> 10) + " KB");
            }
        }
        if (Md5File(tmp) != FontMd5)
        {
            File.Delete(tmp);
            throw new IOException("File font tải về không khớp. Hãy thử lại.");
        }
        return tmp;
    }

    // Chỉ đọc entry đầu của pak do tool tạo; GetState đối chiếu lại toàn bộ pak.
    static byte[] InstalledFont(string pak)
    {
        using (var stream = File.OpenRead(pak))
        using (var reader = new BinaryReader(stream))
        {
            if (stream.Length < 53 || reader.ReadInt64() != 0) return null;
            long size = reader.ReadInt64();
            if (size <= 0 || size > 16 * 1024 * 1024 || size > stream.Length - 53) return null;
            if (reader.ReadInt64() != size || reader.ReadInt32() != 0) return null;
            stream.Position = 53;
            var data = reader.ReadBytes((int)size);
            return Md5(data) == FontMd5 ? data : null;
        }
    }

    static string Md5(byte[] data)
    {
        using (var md5 = MD5.Create()) return BitConverter.ToString(md5.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
    }

    static string Md5File(string path)
    {
        using (var md5 = MD5.Create())
        using (var s = File.OpenRead(path))
            return BitConverter.ToString(md5.ComputeHash(s)).Replace("-", "").ToLowerInvariant();
    }

    // Kiểm tra pak ngôn ngữ en-US đã bị thay bằng file giữ chỗ 0 byte hoặc bị xóa hay chưa.
    // Game cập nhật sẽ khôi phục pak, nên phải kiểm tra file thực tế để xác định trạng thái cài đặt, không dựa vào thư mục backup.
    public static bool PakNeutralized(string game)
    {
        foreach (var ext in new[] { ".pak", ".ucas", ".utoc" })
        {
            var path = Path.Combine(game, EnPakDir, EnPakName + ext);
            if (File.Exists(path) && new FileInfo(path).Length > 0) return false;
        }
        return true;
    }

    public static State GetState(string game)
    {
        if (!IsGameDir(game)) return State.NoGame;
        var pak = Path.Combine(game, PakPath);
        if (!File.Exists(pak)) return State.NotInstalled;
        // Game đã khôi phục pak ngôn ngữ → game đọc lại tiếng Anh, bản dịch tiếng Việt không còn hiệu lực.
        if (!PakNeutralized(game)) return State.NeedsReinstall;
        var hash = Md5File(pak);
        if (hash == Md5(PackData())) return State.Installed;
        var font = InstalledFont(pak);
        return font != null && hash == Md5(PakBuilder.Build(font)) ? State.Installed : State.OtherPatch;
    }

    public static void Install(string game, bool withFont, Action<int, string> progress)
    {
        byte[] font = null;
        if (withFont)
        {
            font = File.ReadAllBytes(LocalFont() ?? DownloadFont(progress));
            if (Md5(font) != FontMd5) throw new IOException("File font không khớp với phiên bản này.");
        }
        var data = font == null ? PackData() : PakBuilder.Build(font);
        var pak = Path.Combine(game, PakPath);
        var staged = pak + ".stage-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(Path.GetDirectoryName(pak));
        try
        {
            // Finish and verify the generated package before modifying original files.
            File.WriteAllBytes(staged, data);
            if (Md5File(staged) != Md5(data))
                throw new IOException("File PAK vừa tạo không khớp dữ liệu gói ngôn ngữ.");
            InstallPrepared(game, staged, pak, progress);
        }
        finally
        {
            if (File.Exists(staged)) File.Delete(staged);
        }
    }

    static void InstallPrepared(string game, string staged, string pak, Action<int, string> progress)
    {
        progress(0, "Đang backup file ngôn ngữ gốc…");
        var enDir = Path.Combine(game, EnPakDir);
        var backup = Path.Combine(game, BackupDir);
        Directory.CreateDirectory(enDir);
        foreach (var ext in PakExts)
        {
            var src = Path.Combine(enDir, EnPakName + ext);
            if (!File.Exists(src)) continue;
            // pak 0 byte là file giữ chỗ do bản patch trước tạo ra, không phải file gốc.
            if (ext == ".pak" && new FileInfo(src).Length == 0) continue;
            Directory.CreateDirectory(backup);
            File.Copy(src, Path.Combine(backup, EnPakName + ext), true);
            File.Delete(src);
        }
        File.WriteAllBytes(Path.Combine(enDir, EnPakName + ".pak"), new byte[0]);

        progress(0, "Đang cài bản dịch tiếng Việt…");
        if (File.Exists(pak)) File.Replace(staged, pak, null);
        else File.Move(staged, pak);

    }

    // Trả về true nếu đã khôi phục tiếng Anh; false nếu không còn pak gốc và cần xác minh file bằng Steam/PURPLE.
    public static bool Uninstall(string game)
    {
        var pak = Path.Combine(game, PakPath);
        if (File.Exists(pak)) File.Delete(pak);

        var enDir = Path.Combine(game, EnPakDir);
        var backup = Path.Combine(game, BackupDir);
        var placeholder = Path.Combine(enDir, EnPakName + ".pak");
        bool hasBackup = File.Exists(Path.Combine(backup, EnPakName + ".pak"));

        if (hasBackup)
        {
            if (File.Exists(placeholder) && new FileInfo(placeholder).Length == 0) File.Delete(placeholder);
            foreach (var ext in PakExts)
            {
                var src = Path.Combine(backup, EnPakName + ext);
                if (!File.Exists(src)) continue;
                File.Copy(src, Path.Combine(enDir, EnPakName + ext), true);
            }
            return true;
        }

        // Không có backup: nếu pak gốc vẫn còn (ví dụ đã được game cập nhật khôi phục), chỉ cần xóa pak bổ sung để trở lại tiếng Anh.
        if (PakNeutralized(game))
        {
            // Chỉ còn file giữ chỗ 0 byte của công cụ; sau khi xóa, người dùng cần tự xác minh file game.
            if (File.Exists(placeholder) && new FileInfo(placeholder).Length == 0) File.Delete(placeholder);
            return false;
        }
        return true;
    }
}

public class MainForm : Form
{
    readonly TextBox pathBox = new TextBox();
    readonly CheckBox fontCheck = new CheckBox();
    readonly Label stateLabel = new Label();
    readonly Label statusLabel = new Label();
    readonly Button browseButton = new Button();
    readonly Button installButton = new Button();
    readonly Button uninstallButton = new Button();
    readonly ProgressBar progressBar = new ProgressBar();
    readonly BackgroundWorker worker = new BackgroundWorker();
    readonly BackgroundWorker scanWorker = new BackgroundWorker();
    readonly BackgroundWorker updateWorker = new BackgroundWorker();
    readonly BackgroundWorker downloadWorker = new BackgroundWorker();
    bool userPicked;
    bool updatePrompted;

    public MainForm()
    {
        Text = "AION2 · Gói Ngôn Ngữ Tiếng Việt";
        Font = new Font("Segoe UI", 9f);
        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(480, 250);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var title = new Label { Text = "AION2 Global · Gói Ngôn Ngữ Tiếng Việt", Font = new Font(Font.FontFamily, 13f, FontStyle.Bold), Location = new Point(16, 14), AutoSize = true };
        var version = new Label { Text = Patch.PackVersion + " · Công Cụ v" + Updater.CurrentText, ForeColor = SystemColors.GrayText, Location = new Point(18, 44), AutoSize = true };
        var pathLabel = new Label { Text = "Đường Dẫn Cài Đặt Game", Location = new Point(18, 76), AutoSize = true };
        pathBox.SetBounds(18, 96, 362, 23);
        browseButton.Text = "Duyệt…";
        browseButton.SetBounds(388, 95, 74, 25);
        stateLabel.SetBounds(18, 126, 444, 20);
        fontCheck.Text = "Cài kèm font tiếng Việt (khuyên dùng, lần đầu tải khoảng 334 KB)";
        fontCheck.Checked = true;
        fontCheck.SetBounds(18, 150, 444, 22);
        installButton.Text = "Cài Đặt";
        installButton.SetBounds(18, 182, 218, 32);
        uninstallButton.Text = "Khôi Phục Tiếng Anh";
        uninstallButton.SetBounds(244, 182, 218, 32);
        progressBar.SetBounds(18, 182, 444, 32);
        progressBar.Visible = false;
        statusLabel.SetBounds(18, 222, 444, 20);
        statusLabel.ForeColor = SystemColors.GrayText;
        Controls.AddRange(new Control[] { title, version, pathLabel, pathBox, browseButton, stateLabel, fontCheck, installButton, uninstallButton, progressBar, statusLabel });
        AcceptButton = installButton;

        pathBox.TextChanged += delegate { RefreshState(); };
        browseButton.Click += delegate { Browse(); };
        installButton.Click += delegate { Start(true); };
        uninstallButton.Click += delegate { Start(false); };
        worker.WorkerReportsProgress = true;
        worker.DoWork += DoWork;
        worker.ProgressChanged += ShowProgress;
        worker.RunWorkerCompleted += Completed;

        // Tự động cập nhật: kiểm tra tag GitHub Release ở nền, hỏi người dùng khi có bản mới.
        updateWorker.DoWork += UpdateDoWork;
        updateWorker.RunWorkerCompleted += UpdateCompleted;
        downloadWorker.WorkerReportsProgress = true;
        downloadWorker.DoWork += DownloadDoWork;
        downloadWorker.ProgressChanged += ShowProgress;
        downloadWorker.RunWorkerCompleted += DownloadCompleted;

        // Tìm nhanh đường dẫn game ở nền trước, rồi quét tất cả ổ đĩa ở nền (hỗ trợ cả Steam và PURPLE).
        scanWorker.WorkerReportsProgress = true;
        scanWorker.WorkerSupportsCancellation = true;
        scanWorker.DoWork += ScanDoWork;
        scanWorker.ProgressChanged += delegate(object s, ProgressChangedEventArgs e)
        {
            if (!worker.IsBusy && !userPicked) statusLabel.Text = "Đang tìm thư mục AION2… " + Shorten((string)e.UserState);
        };
        scanWorker.RunWorkerCompleted += ScanCompleted;
        Shown += delegate
        {
            scanWorker.RunWorkerAsync();
            if (!Updater.Disabled) updateWorker.RunWorkerAsync();
        };
        FormClosing += delegate { scanWorker.CancelAsync(); };

        var quick = Patch.QuickGameDirs();
        if (quick.Count > 0) pathBox.Text = quick[0];
        RefreshState();
        ActiveControl = installButton.Enabled ? (Control)installButton : browseButton;
    }

    static string Shorten(string path)
    {
        if (string.IsNullOrEmpty(path)) return "";
        return path.Length <= 42 ? path : "…" + path.Substring(path.Length - 41);
    }

    void ScanDoWork(object sender, DoWorkEventArgs e)
    {
        e.Result = Patch.ScanGameDirs(
            delegate(string path) { scanWorker.ReportProgress(0, path); },
            delegate { return scanWorker.CancellationPending; });
    }

    void ScanCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        if (userPicked || worker.IsBusy) return;
        var found = e.Error == null ? e.Result as List<string> : null;
        if (found == null || found.Count == 0)
        {
            if (GameDir == null)
            {
                RefreshState();
                statusLabel.Text = "Chưa tìm thấy game. Bấm Duyệt… để chọn thư mục AION2.";
            }
            else
            {
                statusLabel.Text = "";
            }
            return;
        }
        if (found.Count == 1)
        {
            if (GameDir == null) pathBox.Text = found[0];
            RefreshState();
            statusLabel.Text = "Đã tìm thấy: " + Shorten(found[0]);
            return;
        }
        // Tìm thấy nhiều bản cài (ví dụ cả Steam và PURPLE) → hỏi người dùng muốn chọn bản nào.
        using (var pick = new PickFolderForm(found, GameDir))
        {
            if (pick.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(pick.Selected))
            {
                userPicked = true;
                pathBox.Text = pick.Selected;
            }
        }
        statusLabel.Text = "";
        RefreshState();
    }

    string GameDir { get { return Patch.ResolveGameDir(pathBox.Text.Trim().Trim('"')); } }

    void RefreshState()
    {
        Patch.State state;
        try { state = GameDir == null ? Patch.State.NoGame : Patch.GetState(GameDir); }
        catch (IOException) { state = Patch.State.NoGame; }
        catch (UnauthorizedAccessException) { state = Patch.State.NoGame; }
        switch (state)
        {
            case Patch.State.NoGame:
                stateLabel.Text = "Chưa tìm thấy game. Hãy chọn thư mục AION2.";
                stateLabel.ForeColor = Color.Firebrick;
                break;
            case Patch.State.NotInstalled:
                stateLabel.Text = "Trạng thái: Chưa cài đặt (hiện là tiếng Anh)";
                stateLabel.ForeColor = SystemColors.ControlText;
                break;
            case Patch.State.Installed:
                stateLabel.Text = "Trạng thái: Đã cài gói tiếng Việt này";
                stateLabel.ForeColor = Color.SeaGreen;
                break;
            case Patch.State.NeedsReinstall:
                stateLabel.Text = "Trạng thái: Game đã khôi phục file gốc. Hãy cài lại.";
                stateLabel.ForeColor = Color.Firebrick;
                break;
            case Patch.State.OtherPatch:
                stateLabel.Text = "Trạng thái: Đã cài bản dịch khác. Có thể cập nhật.";
                stateLabel.ForeColor = Color.DarkGoldenrod;
                break;
        }
        installButton.Enabled = state != Patch.State.NoGame;
        installButton.Text = state == Patch.State.OtherPatch ? "Cập Nhật Bản Dịch" :
            state == Patch.State.Installed || state == Patch.State.NeedsReinstall ? "Cài Đặt Lại" : "Cài Đặt";
        uninstallButton.Enabled = state == Patch.State.Installed || state == Patch.State.NeedsReinstall || state == Patch.State.OtherPatch;
    }

    void Browse()
    {
        userPicked = true;
        scanWorker.CancelAsync();
        using (var dialog = new FolderBrowserDialog { Description = "Chọn thư mục cài AION2 (Steam: D:\\SteamLibrary\\steamapps\\common\\AION2; PURPLE: D:\\AION2).", ShowNewFolderButton = false })
        {
            if (Directory.Exists(pathBox.Text)) dialog.SelectedPath = pathBox.Text;
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            pathBox.Text = Patch.ResolveGameDir(dialog.SelectedPath) ?? dialog.SelectedPath;
        }
        statusLabel.Text = "";
    }

    void Start(bool install)
    {
        if (Patch.GameRunning())
        {
            MessageBox.Show(this, "Game đang chạy. Hãy đóng game trước khi cài hoặc phục hồi.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        SetBusy(true);
        worker.RunWorkerAsync(new object[] { install, GameDir, fontCheck.Checked });
    }

    void SetBusy(bool busy)
    {
        installButton.Visible = uninstallButton.Visible = !busy;
        progressBar.Visible = busy;
        progressBar.Style = ProgressBarStyle.Marquee;
        pathBox.Enabled = browseButton.Enabled = fontCheck.Enabled = !busy;
    }

    void ShowProgress(object sender, ProgressChangedEventArgs e)
    {
        progressBar.Style = e.ProgressPercentage > 0 ? ProgressBarStyle.Continuous : ProgressBarStyle.Marquee;
        progressBar.Value = Math.Min(100, e.ProgressPercentage);
        statusLabel.Text = (string)e.UserState;
    }

    void UpdateDoWork(object sender, DoWorkEventArgs e)
    {
        e.Result = Updater.Check();
    }

    void UpdateCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        // Không có mạng, GitHub API lỗi hoặc người dùng đang thao tác: bỏ qua kiểm tra cập nhật để không cản trở chức năng chính.
        if (e.Error != null || e.Result == null) return;
        var rel = (Updater.Release)e.Result;
        if (!Updater.IsNewer(rel) || updatePrompted || worker.IsBusy || downloadWorker.IsBusy) return;
        updatePrompted = true;
        var answer = MessageBox.Show(this,
            "Có phiên bản mới " + rel.Tag + " (hiện tại v" + Updater.CurrentText + ").\n\nTải và cập nhật ngay? Công cụ sẽ khởi động lại sau khi cập nhật.",
            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        if (answer != DialogResult.Yes)
        {
            statusLabel.Text = "Có phiên bản mới " + rel.Tag + "：" + Updater.ReleasePage;
            return;
        }
        SetBusy(true);
        statusLabel.Text = "Đang tải bản cập nhật…";
        downloadWorker.RunWorkerAsync(rel);
    }

    void DownloadDoWork(object sender, DoWorkEventArgs e)
    {
        e.Result = Updater.Prepare((Updater.Release)e.Argument, downloadWorker.ReportProgress);
    }

    void DownloadCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        SetBusy(false);
        if (e.Error != null)
        {
            statusLabel.Text = "Cập nhật thất bại.";
            MessageBox.Show(this, "Không thể tự động cập nhật: " + e.Error.Message + "\n\nTải thủ công tại trang Releases:\n" + Updater.ReleasePage,
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var file = (string)e.Result;
        try
        {
            Process.Start(file, "/selfupdate " + Process.GetCurrentProcess().Id + " \"" + Application.ExecutablePath + "\"");
            Application.Exit();
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Cập nhật thất bại.";
            MessageBox.Show(this, "Đã tải bản cập nhật nhưng không thể khởi chạy: " + ex.Message + "\n\nVị trí file:\n" + file + "\nHãy mở file này để cập nhật.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    void DoWork(object sender, DoWorkEventArgs e)
    {
        var args = (object[])e.Argument;
        var game = (string)args[1];
        if ((bool)args[0])
        {
            Patch.Install(game, (bool)args[2], worker.ReportProgress);
            e.Result = "Cài đặt hoàn tất. Khởi động lại game để có hiệu lực.";
        }
        else
        {
            e.Result = Patch.Uninstall(game)
                ? "Đã khôi phục tiếng Anh."
                : "Đã gỡ gói ngôn ngữ nhưng không tìm thấy bản sao lưu file tiếng Anh gốc.\nHãy dùng “Xác minh tính toàn vẹn của file game” cho AION2 trong Steam để khôi phục tiếng Anh.";
        }
    }

    void Completed(object sender, RunWorkerCompletedEventArgs e)
    {
        SetBusy(false);
        RefreshState();
        if (e.Error == null)
        {
            statusLabel.Text = ((string)e.Result).Split('\n')[0];
            MessageBox.Show(this, (string)e.Result, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        statusLabel.Text = "Thao tác thất bại.";
        var message = e.Error is UnauthorizedAccessException
            ? "Không có quyền ghi vào thư mục game. Hãy chạy công cụ với quyền quản trị."
            : e.Error.Message;
        MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}

// Khi tìm thấy nhiều thư mục AION2, cho người dùng chọn bản cần cài (hoặc tự duyệt thư mục).
public class PickFolderForm : Form
{
    readonly ListBox list = new ListBox();

    public string Selected { get; private set; }

    public PickFolderForm(List<string> items, string preselect)
    {
        Text = "Chọn Thư Mục Cài Đặt AION2";
        Font = new Font("Segoe UI", 9f);
        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(480, 250);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        var label = new Label { Text = "Tìm thấy nhiều thư mục AION2. Chọn bản game cần cài:", Location = new Point(14, 12), AutoSize = true };
        list.SetBounds(14, 38, 452, 158);
        list.Items.AddRange(items.ToArray());
        if (!string.IsNullOrEmpty(preselect))
        {
            for (int i = 0; i < list.Items.Count; i++)
                if (string.Equals((string)list.Items[i], preselect, StringComparison.OrdinalIgnoreCase)) { list.SelectedIndex = i; break; }
        }
        if (list.SelectedIndex < 0 && list.Items.Count > 0) list.SelectedIndex = 0;

        var ok = new Button { Text = "Xác Nhận", DialogResult = DialogResult.OK };
        ok.SetBounds(224, 206, 76, 28);
        var browse = new Button { Text = "Duyệt…" };
        browse.SetBounds(308, 206, 76, 28);
        var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel };
        cancel.SetBounds(392, 206, 74, 28);
        browse.Click += delegate
        {
            using (var dialog = new FolderBrowserDialog { Description = "Chọn thư mục cài đặt AION2", ShowNewFolderButton = false })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                Selected = Patch.ResolveGameDir(dialog.SelectedPath) ?? dialog.SelectedPath;
                DialogResult = DialogResult.OK;
                Close();
            }
        };
        AcceptButton = ok;
        CancelButton = cancel;
        Controls.AddRange(new Control[] { label, list, ok, browse, cancel });
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK && string.IsNullOrEmpty(Selected) && list.SelectedItem != null)
            Selected = (string)list.SelectedItem;
        base.OnFormClosing(e);
    }
}

// Tự động cập nhật: so sánh tag GitHub Release với phiên bản EXE; nếu có bản mới thì tải, xác minh, thay EXE rồi khởi động lại.
static class Updater
{
    public const string ReleasePage = "https://github.com/yuezhang128/Steam-AION2-Vietnamese/releases/latest";
    const string ApiUrl = "https://api.github.com/repos/yuezhang128/Steam-AION2-Vietnamese/releases/latest";
    const string AssetName = "AION2_VIE.exe";
    const string ShaAssetName = "AION2_VIE.exe.sha256";

    public class Release
    {
        public string Tag;
        public Version Version;
        public string ExeUrl;
        public string Digest;   // Do GitHub API cung cấp (có thể không có).
        public string ShaUrl;   // File .sha256 được tải lên riêng (có thể không có).
        public string PageUrl;
    }

    // Dùng khi kiểm thử: có thể đặt biến môi trường trỏ đến file JSON cục bộ qua file://.
    public static string FeedUrl
    {
        get
        {
            var custom = Environment.GetEnvironmentVariable("AION2_VIE_UPDATE_URL");
            return string.IsNullOrEmpty(custom) ? ApiUrl : custom;
        }
    }

    public static bool Disabled
    {
        get { return Environment.GetEnvironmentVariable("AION2_VIE_NO_UPDATE") == "1"; }
    }

    // File cập nhật nằm ở %LOCALAPPDATA%\AION2_VIE\update (ít gây nghi ngờ cho antivirus hơn TEMP); có thể đổi bằng biến môi trường khi kiểm thử.
    public static string UpdateDir
    {
        get
        {
            var custom = Environment.GetEnvironmentVariable("AION2_VIE_UPDATE_DIR");
            if (!string.IsNullOrEmpty(custom)) return custom;
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AION2_VIE", "update");
        }
    }

    // Sau khi cập nhật, bản mới dọn các file tải xuống cũ khi khởi động.
    public static void CleanupDownloads()
    {
        try
        {
            var dir = UpdateDir;
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;
            var self = Application.ExecutablePath;
            if (!string.IsNullOrEmpty(self) && self.StartsWith(dir, StringComparison.OrdinalIgnoreCase)) return;
            foreach (var file in Directory.GetFiles(dir))
                try { File.Delete(file); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        catch (Exception) { }
    }

    public static Version Current
    {
        get { return Assembly.GetExecutingAssembly().GetName().Version; }
    }

    public static string CurrentText
    {
        get { var v = Current; return v.Major + "." + v.Minor + "." + v.Build; }
    }

    public static Version ParseTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return null;
        var m = Regex.Match(tag, @"(\d+)(?:\.(\d+))?(?:\.(\d+))?(?:\.(\d+))?");
        if (!m.Success) return null;
        Func<Group, int> num = delegate(Group g) { return g.Success ? int.Parse(g.Value) : 0; };
        return new Version(num(m.Groups[1]), num(m.Groups[2]), num(m.Groups[3]), num(m.Groups[4]));
    }

    static string FetchText(string url, int timeoutMs)
    {
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
        var req = WebRequest.Create(url);
        req.Timeout = timeoutMs;
        var http = req as HttpWebRequest;
        if (http != null)
        {
            http.UserAgent = "AION2_VIE-Updater";
            http.Accept = "application/vnd.github+json";
        }
        using (var resp = req.GetResponse())
        using (var stream = resp.GetResponseStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8))
            return reader.ReadToEnd();
    }

    static string JsonString(string json, string key)
    {
        var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
        return m.Success ? m.Groups[1].Value : null;
    }

    static string AssetField(string json, string asset, string field, string pattern)
    {
        var m = Regex.Match(json, "\"name\"\\s*:\\s*\"" + Regex.Escape(asset) + "\"[\\s\\S]{0,3000}?\"" + field + "\"\\s*:\\s*\"(" + pattern + ")\"");
        return m.Success ? m.Groups[1].Value : null;
    }

    // Kiểm tra release mới nhất trên GitHub; lỗi mạng sẽ phát sinh WebException.
    public static Release Check()
    {
        var json = FetchText(FeedUrl, 10000);
        var rel = new Release();
        rel.Tag = JsonString(json, "tag_name");
        rel.PageUrl = JsonString(json, "html_url");
        rel.Version = ParseTag(rel.Tag);
        rel.ExeUrl = AssetField(json, AssetName, "browser_download_url", "[^\"]+");
        if (rel.ExeUrl == null) rel.ExeUrl = JsonString(json, "browser_download_url");
        var digest = AssetField(json, AssetName, "digest", "[^\"]+");
        if (!string.IsNullOrEmpty(digest) && digest.StartsWith("sha256:")) rel.Digest = digest.Substring(7);
        rel.ShaUrl = AssetField(json, ShaAssetName, "browser_download_url", "[^\"]+");
        if (rel.Version == null)
            throw new InvalidOperationException("Release không có tag phiên bản hợp lệ: " + (rel.Tag ?? "(null)"));
        return rel;
    }

    public static bool IsNewer(Release rel)
    {
        return rel != null && rel.Version != null && rel.Version > Current;
    }

    public static string Sha256File(string path)
    {
        using (var sha = SHA256.Create())
        using (var s = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(s)).Replace("-", "").ToLowerInvariant();
    }

    static string Download(string url, string dest, Action<int, string> progress)
    {
        var req = WebRequest.Create(url);
        req.Timeout = 30000;
        using (var resp = req.GetResponse())
        using (var src = resp.GetResponseStream())
        using (var dst = File.Create(dest))
        {
            long total = resp.ContentLength, done = 0;
            var buf = new byte[81920];
            int n;
            while ((n = src.Read(buf, 0, buf.Length)) > 0)
            {
                dst.Write(buf, 0, n);
                done += n;
                if (progress != null && total > 0) progress((int)(done * 100 / total), "Đang tải bản cập nhật… " + (done >> 20) + " / " + (total >> 20) + " MB");
            }
        }
        return dest;
    }

    // Tải xuống → xác minh SHA-256 → để bản mới tự thay thế EXE.
    public static string Prepare(Release rel, Action<int, string> progress)
    {
        if (rel == null || string.IsNullOrEmpty(rel.ExeUrl)) throw new InvalidOperationException("Release không có file AION2_VIE.exe");
        Directory.CreateDirectory(UpdateDir);
        var tmp = Path.Combine(UpdateDir, "AION2_VIE-" + rel.Version + ".exe");
        Download(rel.ExeUrl, tmp, progress);
        var expected = rel.Digest;
        if (string.IsNullOrEmpty(expected) && !string.IsNullOrEmpty(rel.ShaUrl))
        {
            try
            {
                var text = FetchText(rel.ShaUrl, 15000).Trim();
                var m = Regex.Match(text, "[0-9a-fA-F]{64}");
                if (m.Success) expected = m.Value.ToLowerInvariant();
            }
            catch (WebException) { }
        }
        if (!string.IsNullOrEmpty(expected))
        {
            var actual = Sha256File(tmp);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(tmp);
                throw new IOException("Bản cập nhật không khớp SHA-256. Đã hủy cập nhật.");
            }
        }
        return tmp;
    }

    // Bản mới thực hiện: chờ process cũ thoát → ghi đè → khởi động lại.
    public static int SelfUpdate(string[] args)
    {
        int pid = 0;
        int.TryParse(args.Length > 1 ? args[1] : "", out pid);
        string target = args.Length > 2 ? args[2] : null;
        bool restart = Array.IndexOf(args, "/norestart") < 0;
        if (string.IsNullOrEmpty(target)) { Console.WriteLine("Cách dùng: /selfupdate <pid> <target.exe> [/norestart]"); return 2; }

        if (pid > 0)
        {
            try { Process.GetProcessById(pid).WaitForExit(30000); }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
        }
        string self = Assembly.GetExecutingAssembly().Location;
        Exception last = null;
        for (int i = 0; i < 25; i++)
        {
            try { File.Copy(self, target, true); last = null; break; }
            catch (IOException ex) { last = ex; }
            catch (UnauthorizedAccessException ex) { last = ex; }
            System.Threading.Thread.Sleep(400);
        }
        if (last != null)
        {
            var message = "Không thể cập nhật (file đang bị khóa hoặc thiếu quyền ghi): " + last.Message + "\nTải bản mới tại:\n" + ReleasePage;
            Console.WriteLine(message);
            try { MessageBox.Show(message, "AION2_VIE · Bản Dịch Tiếng Việt", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            catch (Exception) { }
            return 1;
        }
        Console.WriteLine("Đã cập nhật: " + target);
        if (restart)
        {
            try { Process.Start(target); }
            catch (Exception ex) { Console.WriteLine("Không thể khởi động lại: " + ex.Message); }
        }
        return 0;
    }

    // Dòng lệnh: /checkupdate chỉ báo kết quả; /update tải xuống và tự cập nhật.
    public static int CliCheck()
    {
        try
        {
            var rel = Check();
            Console.WriteLine("Phiên bản hiện tại: v" + CurrentText);
            Console.WriteLine("Phiên bản mới nhất: " + rel.Tag + "　" + (rel.PageUrl ?? ReleasePage));
            if (IsNewer(rel))
            {
                Console.WriteLine("Có bản cập nhật mới.");
                return 1;
            }
            Console.WriteLine("Đang dùng phiên bản mới nhất.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Không thể kiểm tra cập nhật: " + ex.Message);
            return 2;
        }
    }

    public static int CliUpdate(string[] args)
    {
        bool noRestart = Array.IndexOf(args, "/norestart") >= 0;
        try
        {
            var rel = Check();
            if (!IsNewer(rel))
            {
                Console.WriteLine("Đang dùng phiên bản mới nhất (v" + CurrentText + ").");
                return 0;
            }
            Console.WriteLine("Có phiên bản mới " + rel.Tag + ", đang tải…");
            int lastPct = -1;
            var file = Prepare(rel, delegate(int pct, string msg)
            {
                if (pct >= lastPct + 10 || pct >= 100) { lastPct = pct; Console.WriteLine("  Đang tải… " + pct + "%"); }
            });
            Console.WriteLine("Đã tải và kiểm tra: " + file);
            try
            {
                var psi = "/selfupdate " + Process.GetCurrentProcess().Id + " \"" + Application.ExecutablePath + "\"" + (noRestart ? " /norestart" : "");
                Process.Start(file, psi);
                Console.WriteLine("Bản mới sẽ thay thế file. Công cụ hiện tại sẽ đóng.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Không thể khởi chạy bản cập nhật: " + ex.Message);
                return 4;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Cập nhật thất bại: " + ex.Message);
            return 3;
        }
    }
}

// Bộ ghi PAK v11 không nén, không mã hóa dành cho bản patch này.
// Tham khảo định dạng: https://github.com/trumank/repak (pak.rs, entry.rs).
static class PakBuilder
{
    sealed class Entry
    {
        public string Path;
        public byte[] Data;
        public uint Offset;
        public Entry(string path, byte[] data)
        {
            Path = path;
            Data = data;
        }

        public Entry(string path, string resource)
        {
            Path = path;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            {
                if (stream == null) throw new InvalidDataException("Thiếu dữ liệu nhúng: " + resource);
                if (stream.Length == 0 || stream.Length > 256 * 1024 * 1024)
                    throw new InvalidDataException("Kích thước dữ liệu nhúng không hợp lệ: " + resource);
                using (var buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);
                    Data = buffer.ToArray();
                }
                if (Data.LongLength != stream.Length) throw new EndOfStreamException(resource);
            }
        }
    }

    static byte[] Sha1(byte[] data)
    {
        using (var sha = SHA1.Create()) return sha.ComputeHash(data);
    }

    static void WriteString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length + 1);
        writer.Write(bytes);
        writer.Write((byte)0);
    }

    static byte[] WriteSection(Action<BinaryWriter> write)
    {
        using (var buffer = new MemoryStream())
        using (var writer = new BinaryWriter(buffer))
        {
            write(writer);
            writer.Flush();
            return buffer.ToArray();
        }
    }

    static ulong PathHash(string path)
    {
        ulong hash = 0xcbf29ce484222325UL; // FNV-1a, seed bằng 0, đường dẫn viết thường mã hóa UTF-16LE.
        foreach (byte value in Encoding.Unicode.GetBytes(path.ToLowerInvariant()))
            hash = unchecked((hash ^ value) * 0x100000001b3UL);
        return hash;
    }

    static void IndexReference(BinaryWriter writer, long offset, byte[] section)
    {
        writer.Write(1);
        writer.Write(offset);
        writer.Write((long)section.Length);
        writer.Write(Sha1(section));
    }

    public static byte[] Build(byte[] font)
    {
        // Giữ thứ tự ghi dữ liệu của gói đã kiểm thử.
        var files = new List<Entry>();
        if (font != null) files.Add(new Entry("Aion2/Content/UI/Font/Aion2Atreia_EN_Bold.ufont", font));
        files.Add(new Entry("Aion2/Content/L10N/Text/en-US/L10NString.dat", "vie.dat"));
        var entries = files.ToArray();
        return WriteSection(delegate(BinaryWriter output)
        {
            foreach (var entry in entries)
            {
                entry.Offset = checked((uint)output.BaseStream.Position);
                output.Write(0L); // Header dữ liệu ghi offset bằng 0; chỉ mục lưu offset thật của file.
                output.Write((long)entry.Data.Length);
                output.Write((long)entry.Data.Length);
                output.Write(0); // Không nén PAK.
                output.Write(Sha1(entry.Data));
                output.Write((byte)0); // Không mã hóa PAK.
                output.Write(0); // Không có block nén.
                output.Write(entry.Data);
            }
            Array.Sort(entries, delegate(Entry a, Entry b) { return string.CompareOrdinal(a.Path, b.Path); });
            var encoded = WriteSection(delegate(BinaryWriter writer)
            {
                foreach (var entry in entries)
                {
                    writer.Write(0xe0000000U); // Offset và kích thước 32 bit; không có block nén.
                    writer.Write(entry.Offset);
                    writer.Write((uint)entry.Data.Length);
                }
            });
            var paths = WriteSection(delegate(BinaryWriter writer)
            {
                writer.Write(entries.Length);
                for (int i = 0; i < entries.Length; i++)
                {
                    writer.Write(PathHash(entries[i].Path));
                    writer.Write(i * 12);
                }
                writer.Write(0);
            });
            var folders = new SortedDictionary<string, SortedDictionary<string, int>>(StringComparer.Ordinal);
            folders.Add("/", new SortedDictionary<string, int>(StringComparer.Ordinal));
            for (int i = 0; i < entries.Length; i++)
            {
                string path = entries[i].Path;
                for (int j = 0; j < path.Length; j++)
                    if (path[j] == '/' && !folders.ContainsKey(path.Substring(0, j + 1)))
                        folders.Add(path.Substring(0, j + 1), new SortedDictionary<string, int>(StringComparer.Ordinal));
                int split = path.LastIndexOf('/') + 1;
                folders[path.Substring(0, split)].Add(path.Substring(split), i * 12);
            }
            var directory = WriteSection(delegate(BinaryWriter writer)
            {
                writer.Write(folders.Count);
                foreach (var folder in folders)
                {
                    WriteString(writer, folder.Key);
                    writer.Write(folder.Value.Count);
                    foreach (var file in folder.Value) { WriteString(writer, file.Key); writer.Write(file.Value); }
                }
            });
            long indexOffset = output.BaseStream.Position;
            // FString điểm gắn + số file + seed + hai tham chiếu chỉ mục + độ dài/dữ liệu entry mã hóa + số entry không dùng.
            long indexSize = 4 + 10 + 4 + 8 + 40 + 40 + 4 + encoded.Length + 4;
            var index = WriteSection(delegate(BinaryWriter writer)
            {
                WriteString(writer, "../../../");
                writer.Write(entries.Length);
                writer.Write(0UL);
                IndexReference(writer, indexOffset + indexSize, paths);
                IndexReference(writer, indexOffset + indexSize + paths.Length, directory);
                writer.Write(encoded.Length);
                writer.Write(encoded);
                writer.Write(0);
            });
            if (index.Length != indexSize) throw new InvalidDataException("Kích thước index PAK không khớp.");
            output.Write(index);
            output.Write(paths);
            output.Write(directory);
            output.Write(new byte[16]); // GUID mã hóa.
            output.Write((byte)0);
            output.Write(0x5a6f12e1U);
            output.Write(11);
            output.Write(indexOffset);
            output.Write(indexSize);
            output.Write(Sha1(index));
            output.Write(new byte[160]); // Năm tên phương thức nén để trống.
        });
    }
}

static class Program
{
    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();

    // CLI: /install, /uninstall, /scan, /checkupdate, /update, /selfupdate, /noupdate.
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length >= 1 && args[0] == "/selfupdate")
            return Updater.SelfUpdate(args);
        if (args.Length >= 1 && args[0] == "/checkupdate")
            return Updater.CliCheck();
        if (args.Length >= 1 && args[0] == "/update")
            return Updater.CliUpdate(args);
        // /noupdate là cờ có thể dùng cùng tham số khác (tách cờ trước khi xác định thao tác).
        var rest = new List<string>(args);
        if (rest.Remove("/noupdate"))
            Environment.SetEnvironmentVariable("AION2_VIE_NO_UPDATE", "1");
        args = rest.ToArray();

        Updater.CleanupDownloads();
        if (args.Length >= 1 && args[0] == "/scan")
        {
            var found = Patch.ScanGameDirs(null, null);
            foreach (var dir in found) Console.WriteLine(dir);
            return found.Count > 0 ? 0 : 2;
        }
        if (args.Length >= 2 && (args[0] == "/install" || args[0] == "/uninstall"))
        {
            var game = Patch.ResolveGameDir(args[1]);
            if (game == null) return 2;
            try
            {
                if (args[0] == "/install") Patch.Install(game, Array.IndexOf(args, "/nofont") < 0, delegate { });
                else return Patch.Uninstall(game) ? 0 : 3;
                return 0;
            }
            catch (Exception) { return 1; }
        }
        SetProcessDPIAware();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
        return 0;
    }
}
