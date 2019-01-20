using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Winform461
{
    public partial class Form1 : Form
    {
        #region Data Memebers
        //private static ResourceManager rm = new ResourceManager("WindowsFormsApp1.en_local", Assembly.GetExecutingAssembly());
        #endregion Data Memebers

        #region Constructor
        public Form1()
        {
            InitializeComponent();
        }
        #endregion Constructor

        #region Events
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void webSiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/RandallFlagg/chrlauncher/");
        }

        private void giveThanksToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutForm().ShowDialog();
        }

        private void linkLabelGitHub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Specify that the link was visited.
            this.linkLabelGitHub.LinkVisited = true;
            // Navigate to a URL.
            Process.Start("http://www.github.com/RandallFlagg");
        }

        private void linkLabelChromium_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Specify that the link was visited.
            this.linkLabelChromium.LinkVisited = true;
            // Navigate to a URL.
            Process.Start("https://chromium.woolyss.com/");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en");
            btnStart.Text = Properties.Resources.IDS_ACTION_DOWNLOAD;
            new Thread(new ThreadStart(_app_thread));
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            //if the form is minimized  
            //hide it from the task bar  
            //and show the system tray icon (represented by the NotifyIcon control)  
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(1000);
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
        }
        #endregion Events

        #region Help Method
        bool _app_installupdate(HWND hwnd, LPCWSTR path)
        {
            config.is_isinstalling = true;

            if (!_r_fs_exists(config.binary_dir))
                _r_fs_mkdir(config.binary_dir);

            bool result = false;
            bool is_stopped = false;
            ZIPENTRY ze = { 0 };

            HZIP hz = OpenZip(path, nullptr);

            if (IsZipHandleU(hz))
            {
                INT start_idx = 0;
                size_t title_length = wcslen(L"chrome-win32");

                // check archive is official package or not
                GetZipItem(hz, 0, &ze);

                if (_wcsnicmp(ze.name, L"chrome-win32", title_length) == 0)
                {
                    start_idx = 1;
                    title_length += 1;
                }
                else
                {
                    title_length = 0;
                }

                DWORDLONG total_size = 0;
                DWORDLONG total_read = 0; // this is our progress so far

                // count total files
                GetZipItem(hz, -1, &ze);
                const INT total_files = ze.index;

                // count total size of unpacked files
                for (INT i = start_idx; i < total_files; i++)
                {
                    GetZipItem(hz, i, &ze);

                    total_size += ze.unc_size;
                }

                rstring fpath;

                CHAR buffer[BUFFER_SIZE] = { 0 };
                DWORD written = 0;

                for (INT i = start_idx; i < total_files; i++)
                {
                    if (WaitForSingleObjectEx(config.stop_evt, 0, FALSE) == WAIT_OBJECT_0)
                    {
                        is_stopped = true;
                        break;
                    }

                    GetZipItem(hz, i, &ze);

                    fpath.Format(L"%s\\%s", config.binary_dir, rstring(ze.name + title_length).Replace(L"/", L"\\").GetString());

                    _app_setstatus(hwnd, app.LocaleString(IDS_STATUS_INSTALL, nullptr), total_read, total_size);

                    if ((ze.attr & FILE_ATTRIBUTE_DIRECTORY) != 0)
                    {
                        _r_fs_mkdir(fpath);
                    }
                    else
                    {
                        {
                            rstring dir = _r_path_extractdir(fpath);

                            if (!_r_fs_exists(dir))
                                _r_fs_mkdir(dir);
                        }

                        const HANDLE h = CreateFile(fpath, GENERIC_WRITE, FILE_SHARE_READ, nullptr, CREATE_ALWAYS, FILE_FLAG_WRITE_THROUGH, nullptr);

                        if (h != INVALID_HANDLE_VALUE)
                        {
                            DWORD total_read_file = 0;

                            for (ZRESULT zr = ZR_MORE; zr == ZR_MORE;)
                            {
                                DWORD bufsize = BUFFER_SIZE;

                                zr = UnzipItem(hz, i, buffer, bufsize);

                                if (zr == ZR_OK)
                                    bufsize = ze.unc_size - total_read_file;

                                buffer[bufsize] = 0;

                                WriteFile(h, buffer, bufsize, &written, nullptr);

                                total_read_file += bufsize;
                                total_read += bufsize;
                            }

                            CloseHandle(h);
                        }
                    }
                }

                if (!is_stopped)
                    result = true;

                CloseZip(hz);
            }
            else
            {
                _r_fs_delete(path, false); // remove cache file when zip cannot be opened
            }

            if (result)
            {
                _app_cleanup(_app_getversion(config.binary_path));
                _r_fs_delete(path, false); // remove cache file on installation finished
            }

            _app_setstatus(hwnd, nullptr, 0, 0);

            config.is_isinstalling = false;

            return result;
        }

        void ConfigSet(string key, object value) {
        }

        VOID _app_openbrowser(LPCWSTR url)
        {
            if (!_r_fs_exists(config.binary_path))
                return;

            if (!url && _r_process_is_exists(config.binary_dir, wcslen(config.binary_dir)))
                return;

            WCHAR args[2048] = { 0 };

            if (!ExpandEnvironmentStrings(config.args, args, _countof(config.args)))
                StringCchCopy(args, _countof(args), config.args);

            if (url)
                StringCchCat(args, _countof(args), url);

            rstring arg;
            arg.Format(L"\"%s\" %s", config.binary_path, args);

            _r_run(config.binary_path, arg, config.binary_dir);
        }

        bool _app_checkupdate(HWND hwnd)
        {
            HINTERNET hconnect = nullptr;
            HINTERNET hrequest = nullptr;

            rstring::map_one result;

            const INT days = app.ConfigGet(L"ChromiumCheckPeriod", 1).AsInt();
            const bool is_exists = _r_fs_exists(config.binary_path);
            bool is_stopped = false;

            if (days == -1) // there is .ini option to force update checking
                config.is_forcecheck = true;

            if (config.is_forcecheck || (days > 0 || !is_exists))
            {
                if (config.is_forcecheck || !is_exists || (_r_unixtime_now() - app.ConfigGet(L"ChromiumLastCheck", 0).AsLonglong()) >= (86400 * days))
                {
                    HINTERNET hsession = _r_inet_createsession(app.GetUserAgent());

                    if (hsession)
                    {
                        if (_r_inet_openurl(hsession, _r_fmt(app.ConfigGet(L"ChromiumUpdateUrl", CHROMIUM_UPDATE_URL), config.architecture, config.type), &hconnect, &hrequest, nullptr))
                        {
                            const DWORD buffer_length = 2048;

                            LPSTR buffera = new CHAR[buffer_length];
                            rstring bufferw;

                            if (buffera)
                            {
                                DWORD total_length = 0;

                                while (true)
                                {
                                    if (WaitForSingleObjectEx(config.stop_evt, 0, FALSE) == WAIT_OBJECT_0)
                                    {
                                        is_stopped = true;
                                        break;
                                    }

                                    if (!_r_inet_readrequest(hrequest, buffera, buffer_length - 1, nullptr, &total_length))
                                        break;

                                    bufferw.Append(buffera);
                                }

                                delete[] buffera;
                            }

                            if (!is_stopped && !bufferw.IsEmpty())
                            {
                                rstring::rvector vc = bufferw.AsVector(L";");

                                for (size_t i = 0; i < vc.size(); i++)
                                {
                                    const size_t pos = vc.at(i).Find(L'=');

                                    if (pos != rstring::npos)
                                        result[vc.at(i).Midded(0, pos)] = vc.at(i).Midded(pos + 1);
                                }
                            }
                        }

                        if (hrequest)
                            _r_inet_close(hrequest);

                        if (hconnect)
                            _r_inet_close(hconnect);

                        _r_inet_close(hsession);
                    }
                }
            }

            if (!is_stopped && !result.empty())
            {
                if (!is_exists || _wcsnicmp(result[L"version"], config.current_version, wcslen(config.current_version)) != 0)
                {
                    SetDlgItemText(hwnd, IDC_BROWSER, _r_fmt(app.LocaleString(IDS_BROWSER, nullptr), config.name_full));
                    SetDlgItemText(hwnd, IDC_CURRENTVERSION, _r_fmt(app.LocaleString(IDS_CURRENTVERSION, nullptr), !config.current_version[0] ? L"<not found>" : config.current_version));
                    SetDlgItemText(hwnd, IDC_VERSION, _r_fmt(app.LocaleString(IDS_VERSION, nullptr), result[L"version"].GetString()));
                    SetDlgItemText(hwnd, IDC_DATE, _r_fmt(app.LocaleString(IDS_DATE, nullptr), _r_fmt_date(result[L"timestamp"].AsLonglong(), FDTF_SHORTDATE | FDTF_SHORTTIME).GetString()));

                    StringCchCopy(config.download_url, _countof(config.download_url), result[L"download"]);

                    app.ConfigSet(L"ChromiumLastBuild", result[L"timestamp"]);
                    app.ConfigSet(L"ChromiumLastVersion", result[L"version"]);

                    return true;
                }
            }

            return false;
        }

        void _app_thread()
        {
            STATIC_DATA config = new STATIC_DATA();
            btnStart.Enabled = false;

            if (Directory.Exists(config.cache_path))
            {
                btnStart.Text = Properties.Resources.IDS_ACTION_INSTALL;
                //notifyIcon.Visible = true;

                if (!Directory.Exists(config.binary_dir))
                {
                    if (config.is_bringtofront)
                    {
                        Show(); // show window
                    }

                    if (_app_installupdate(IntPtr, config.cache_path))
                    {
                        var unixTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
                        ConfigSet("ChromiumLastCheck", unixTime);
                        config.is_isinstalled = true;
                    }
                }
                else
                {
                    btnStart.Enabled = true;
                }
            }

            if (!config.is_isinstalled)
            {
                if (!config.is_waitdownloadend)
                {
                    _app_openbrowser(nullptr);
                }

                if (_app_checkupdate(IntPtr))
                {
                    if (!Directory.Exists(config.binary_path) || config.is_autodownload)
                    {
                        SetEvent(config.download_evt);
                    }
                    else
                    {
                        btnStart.Text = Properties.Resources.IDS_ACTION_DOWNLOAD;
                        btnStart.Enabled = true;

                        notifyIcon.Visible = true; // show tray icon
                        notifyIcon.BalloonTipText = Properties.Resources.IDS_STATUS_FOUND;
                        notifyIcon.ShowBalloonTip(-1); // just inform user
                    }
                }
                //else
                //{
                //    break; // update not found
                //}
            }
        }
            else if (state == WAIT_OBJECT_0 + 2) // download_evt
        {
            _r_ctrl_enable(IntPtr, IDC_START_BTN, false);
        _r_ctrl_settext(IntPtr, IDC_START_BTN, app.LocaleString(IDS_ACTION_DOWNLOAD, nullptr));

        app.TrayToggle(UID, true); // show tray icon

            if (config.is_bringtofront)
                _r_wnd_toggle(IntPtr, true); // show window

            if (!config.is_isdownloaded && !config.is_waitdownloadend)
                _app_openbrowser(nullptr);

            if (config.is_isdownloaded || _app_downloadupdate(IntPtr, config.download_url, config.cache_path))
            {
                config.is_isdownloaded = true;

                if (!_r_process_is_exists(config.binary_dir, wcslen(config.binary_dir)))
                {
                    _r_ctrl_enable(IntPtr, IDC_START_BTN, false);

                    if (_app_installupdate(IntPtr, config.cache_path))
                    {
                        app.ConfigSet(L"ChromiumLastCheck", _r_unixtime_now());
                        config.is_isinstalled = true;
                    }
}
                else
                {
                    app.TrayPopup(UID, NIIF_USER, APP_NAME, app.LocaleString(IDS_STATUS_DOWNLOADED, nullptr)); // inform user

                    _r_ctrl_enable(IntPtr, IDC_START_BTN, true);
_r_ctrl_settext(IntPtr, IDC_START_BTN, app.LocaleString(IDS_ACTION_INSTALL, nullptr));
                }
            }
            else
            {
                _r_ctrl_enable(IntPtr, IDC_START_BTN, true);
            }

            if (config.is_isinstalled)
            {
                _app_openbrowser(nullptr);
                break;
            }
        }
        else
        {
            break; // stop or error
        }
        }
    }

    void parseCommandLine()
{
    // parse command line
    {
        INT numargs = 0;
        LPWSTR* arga = CommandLineToArgvW(GetCommandLine(), &numargs);

        if (arga)
        {
            for (INT i = 1; i < numargs; i++)
            {
                if (wcslen(arga[i]) < 2)
                    continue;

                if (arga[i][0] == L'/')
						{
                if (_wcsnicmp(arga[i], L"/a", 2) == 0 || _wcsnicmp(arga[i], L"/autodownload", 13) == 0)
                {
                    config.is_autodownload = true;
                }
                else if (_wcsnicmp(arga[i], L"/b", 2) == 0 || _wcsnicmp(arga[i], L"/bringtofront", 13) == 0)
                {
                    config.is_bringtofront = true;
                }
                else if (_wcsnicmp(arga[i], L"/f", 2) == 0 || _wcsnicmp(arga[i], L"/forcecheck", 11) == 0)
                {
                    config.is_forcecheck = true;
                }
                else if (_wcsnicmp(arga[i], L"/w", 2) == 0 || _wcsnicmp(arga[i], L"/wait", 5) == 0)
                {
                    config.is_waitdownloadend = true;
                }
            }
						else if (arga[i][0] == L'-' && arga[i][1] == L'-')
						{
                // there is Chromium arguments
                StringCchCat(config.args, _countof(config.args), L" ");
                StringCchCat(config.args, _countof(config.args), arga[i]);
            }
						else
						{
                // there is Chromium url
                StringCchCat(config.urls, _countof(config.urls), L" \"");
                StringCchCat(config.urls, _countof(config.urls), arga[i]);
                StringCchCat(config.urls, _countof(config.urls), L"\"");
            }
        }

        LocalFree(arga);
    }
}

void Initialize()
{
    TrayCreate(hwnd, UID, WM_TRAYICON, _r_loadicon(app.GetHINSTANCE(), MAKEINTRESOURCE(IDI_MAIN), GetSystemMetrics(SM_CXSMICON)), (config.is_isdownloading || config.is_isinstalling || config.is_isdownloaded) ? false : true);
    SetCurrentDirectory(app.GetDirectory());

    // configure paths
    GetTempPath(_countof(config.cache_path), config.cache_path);
    StringCchCat(config.cache_path, _countof(config.cache_path), APP_NAME_SHORT L"Cache.ZIP");

    StringCchCopy(config.binary_dir, _countof(config.binary_dir), _r_path_expand(app.ConfigGet(L"ChromiumDirectory", L".\\bin")));
    StringCchPrintf(config.binary_path, _countof(config.binary_path), L"%s\\%s", config.binary_dir, app.ConfigGet(L"ChromiumBinary", L"chrome.exe").GetString());

    // get browser architecture...
    if (_r_sys_validversion(5, 1, 0, VER_EQUAL) || _r_sys_validversion(5, 2, 0, VER_EQUAL))
        config.architecture = 32; // on XP only 32-bit supported
    else
        config.architecture = app.ConfigGet(L"ChromiumArchitecture", 0).AsUint();

    if (!config.architecture || (config.architecture != 64 && config.architecture != 32))
    {
        config.architecture = 0;

        // ...by executable
        if (_r_fs_exists(config.binary_path))
        {
            DWORD exe_type = 0;

            if (GetBinaryType(config.binary_path, &exe_type))
            {
                if (exe_type == SCS_32BIT_BINARY)
                    config.architecture = 32;
                else if (exe_type == SCS_64BIT_BINARY)
                    config.architecture = 64;
            }
        }

        // ...by processor architecture
        if (!config.architecture)
        {
            SYSTEM_INFO si = { 0 };
            GetNativeSystemInfo(&si);

            if (si.wProcessorArchitecture == PROCESSOR_ARCHITECTURE_AMD64)
                config.architecture = 64;
            else
                config.architecture = 32;
        }
    }

    if (!config.architecture || (config.architecture != 32 && config.architecture != 64))
        config.architecture = 32; // default architecture

    // set common data
    StringCchCopy(config.type, _countof(config.type), app.ConfigGet(L"ChromiumType", L"dev-codecs-sync"));
    StringCchPrintf(config.name_full, _countof(config.name_full), L"Chromium %d-bit (%s)", config.architecture, config.type);

    StringCchCopy(config.args, _countof(config.args), app.ConfigGet(L"ChromiumCommandLine", L"--user-data-dir=..\\profile --no-default-browser-check --allow-outdated-plugins --disable-logging --disable-breakpad"));

    StringCchCopy(config.current_version, _countof(config.current_version), _app_getversion(config.binary_path));

    // set controls data
    SetDlgItemText(hwnd, IDC_BROWSER, _r_fmt(app.LocaleString(IDS_BROWSER, nullptr), config.name_full));
    SetDlgItemText(hwnd, IDC_CURRENTVERSION, _r_fmt(app.LocaleString(IDS_CURRENTVERSION, nullptr), !config.current_version[0] ? L"<not found>" : config.current_version));
    SetDlgItemText(hwnd, IDC_VERSION, _r_fmt(app.LocaleString(IDS_VERSION, nullptr), app.ConfigGet(L"ChromiumLastVersion", L"<not found>").GetString()));
    SetDlgItemText(hwnd, IDC_DATE, _r_fmt(app.LocaleString(IDS_DATE, nullptr), _r_fmt_date(app.ConfigGet(L"ChromiumLastBuild", 0).AsLonglong(), FDTF_SHORTDATE | FDTF_SHORTTIME).GetString()));

    _r_wnd_addstyle(hwnd, IDC_START_BTN, app.IsClassicUI() ? WS_EX_STATICEDGE : 0, WS_EX_STATICEDGE, GWL_EXSTYLE);

    parseCommandLine();


    // set default config
    if (!config.is_autodownload)
        config.is_autodownload = app.ConfigGet(L"ChromiumAutoDownload", false).AsBool();

    if (!config.is_bringtofront)
        config.is_bringtofront = app.ConfigGet(L"ChromiumBringToFront", false).AsBool();

    if (!config.is_waitdownloadend)
        config.is_waitdownloadend = app.ConfigGet(L"ChromiumWaitForDownloadEnd", false).AsBool();

    // set ppapi info
    {
        rstring ppapi_path = _r_path_expand(app.ConfigGet(L"FlashPlayerPath", L".\\plugins\\pepflashplayer.dll"));

        if (_r_fs_exists(ppapi_path))
        {
            StringCchCat(config.args, _countof(config.args), L" --ppapi-flash-path=\"");
            StringCchCat(config.args, _countof(config.args), ppapi_path);
            StringCchCat(config.args, _countof(config.args), L"\" --ppapi-flash-version=\"");
            StringCchCat(config.args, _countof(config.args), _app_getversion(ppapi_path));
            StringCchCat(config.args, _countof(config.args), L"\"");
        }
    }
}
    #endregion Help Method
}
