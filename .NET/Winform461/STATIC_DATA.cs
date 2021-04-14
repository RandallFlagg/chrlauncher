using System;

namespace Winform461
{
    class STATIC_DATA
    {
        //IntPtr thread = IntPtr.Zero;
        //IntPtr end_evt = IntPtr.Zero;
        //IntPtr stop_evt = IntPtr.Zero;
        //IntPtr check_evt = IntPtr.Zero;
        //IntPtr download_evt = IntPtr.Zero;

        public bool is_autodownload = false;
        public bool is_bringtofront = false;
        //bool is_forcecheck = false;
        public bool is_waitdownloadend = false;

        //bool is_isdownloading = false;
        //bool is_isinstalling = false;

        //bool is_isdownloaded = false;
        public bool is_isinstalled = false;

        //uint architecture = 0;

        //char[] current_version = new char[32];

        //char[] name = new char[64];
        //char[] name_full = new char[64];
        //char[] type = new char[64];

        //char[] cache_path = new char[512];
        //char[] binary_dir = new char[512];
        //char[] binary_path = new char[512];
        public string cache_path;//512
        public string binary_dir;//512
        public string  binary_path;//512
        //char[] download_url = new char[512];

        //char[] urls = new char[1024];

        //char[] args = new char[2048];
    };
}
