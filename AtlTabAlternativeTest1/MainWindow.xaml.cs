using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Drawing;
using System.Windows.Interop;

namespace AtlTabAlternativeTest1
{


    
    
    public partial class MainWindow : Window
    {

        const int MYACTION_HOTKEY_ID = 1;

        private int nextId = 0;

        public MainWindow()
        {
            InitializeComponent();

            UpdateProcessesList();

        }

        

        [DllImport("User32.dll")]
        private static extern bool RegisterHotKey(
             [In] IntPtr hWnd,
             [In] int id,
             [In] uint fsModifiers,
             [In] uint vk);

        [DllImport("User32.dll")]
        private static extern bool UnregisterHotKey(
            [In] IntPtr hWnd,
            [In] int id);

        private HwndSource _source;
        private const int HOTKEY_ID = 9000;
        private const int HOTKEY_ID_2 = 9001;

        protected override void OnSourceInitialized(EventArgs e)
        {
            
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);
            RegisterHotKey();
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(HwndHook);
            _source = null;
            UnregisterHotKey();
            base.OnClosed(e);
        }

        private void RegisterHotKey()
        {
            var helper = new WindowInteropHelper(this);
            const uint VK_F9 = 0x78;
            const uint VK_F10 = 0x79;
            const uint VK_F11 = 0x7A;
            const uint VK_F12 = 0x7B;
            const uint VK_TAB = 0x009;
            const uint VK_1 = 0x31;
            const uint VK_Q = 0x51;

            const uint MOD_CTRL = 0x0002;
            const uint MOD_ALT = 0x0001;
            
            if (!RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_ALT, VK_Q))
            {
                // handle error
            }
            if (!RegisterHotKey(helper.Handle, HOTKEY_ID_2, MOD_ALT, VK_F11))
            {
                // handle error
            }


        }

        private void UnregisterHotKey()
        {
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            OnHotKeyPressed(1);
                            handled = true;
                            break;
                        case HOTKEY_ID_2:
                            OnHotKeyPressed(2);
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private void OnHotKeyPressed(int hotkeyNumber)
        {
            if (hotkeyNumber == 1)
            {
                //Console.WriteLine("hit alt-q");
                CycleToNextWindow();
            }
            else
            {
                //Console.WriteLine("hit alt-f11");
                ShowOrHideWindow(true, Process.GetCurrentProcess().Id);
                return;
            }

            
            
        }

        private void CycleToNextWindow()
        {
            Process[] processes = (Process[])ListView1.ItemsSource;

            Process[] selectedProcesses = (ListView1.SelectedItems.OfType<Process>().ToArray());

            
            if (selectedProcesses.Length == 0)
            {
                return;
            }

            int idAfterNextId = 0;
            for (int i = 0; i < selectedProcesses.Length; i++)
            {
                Process p = selectedProcesses[i];
                if (p.Id == nextId)
                {
                    idAfterNextId = selectedProcesses[(i + 1) % selectedProcesses.Length].Id;
                    break;
                }
                //if the window you were planning on going to isn't in the selection anymore, 
                // then start over with first selected window
                if (i == selectedProcesses.Length - 1)
                {
                    nextId = 0;
                }
            }

            if (nextId == 0)
            {
                ShowOrHideWindow(true, selectedProcesses[0].Id);
                nextId = selectedProcesses[0].Id;
                if (selectedProcesses.Length > 1)
                {
                    nextId = selectedProcesses[1].Id;
                }
            }
            else
            {

                ShowOrHideWindow(true, nextId);


                nextId = idAfterNextId;
            }
        }

        public void UpdateProcessesList()
        {

            Process[] allProcesses = Process.GetProcesses();

            foreach (Process p in allProcesses)
            {
                Console.WriteLine(p.StartInfo.WorkingDirectory);
            }

            //string[] titlesBlacklist = 
            //    excludedTitlesTextBox.Text.Split(new string[] {", "}, StringSplitOptions.None);

            //allProcesses = allProcesses.Where(p => 
                    //!titlesBlacklist.Contains(p.MainWindowTitle))
            //        .ToArray();

            ListView1.ItemsSource = allProcesses
                .Where(p => !String.IsNullOrEmpty(p.MainWindowTitle))
                //.Sort((x, y) => DateTime.Compare(x.StartTime, y.StartTime))
                //.Select(p => p.MainWindowTitle + ": started at " + p.StartTime)
                .ToArray();
            
        }


        public void ShowOrHideWindow(bool showIfTrue, int processId)
        {
            
            Process process = Process.GetProcessById(processId);
            
            IDictionary<IntPtr, string> windows = List_Windows_By_PID(process.Id);
            foreach (KeyValuePair<IntPtr, string> pair in windows)
            {
                var placement = new WINDOWPLACEMENT();
                GetWindowPlacement(pair.Key, ref placement);

                if (showIfTrue)
                {
                    SetForegroundWindow(pair.Key);
                    if (processId == Process.GetCurrentProcess().Id)
                    {
                        if (placement.showCmd == SW_SHOWMINIMIZED)
                        {
                            ShowWindowAsync(pair.Key, SW_RESTORE);
                        }
                        else
                        {
                            ShowWindowAsync(pair.Key, SW_MINIMIZE);
                        }
   
                    }
                    else
                    {
                        ShowWindowAsync(pair.Key, SW_SHOWMAXIMIZED);
                    }
                          
                }
                else
                {
                    ShowWindowAsync(pair.Key, SW_SHOWMINIMIZED);
                }
                
            }
            
        }


        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWMAXIMIZED = 3;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;
        private const int SW_HIDE = 0;
        private const int SW_MINIMIZE = 6;

        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("USER32.DLL")]
        private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("USER32.DLL")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("USER32.DLL")]
        private static extern IntPtr GetShellWindow();

        [DllImport("USER32.DLL")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);


        public static IDictionary<IntPtr, string> List_Windows_By_PID(int processID)
        {
            IntPtr hShellWindow = GetShellWindow();
            Dictionary<IntPtr, string> dictWindows = new Dictionary<IntPtr, string>();

            EnumWindows(delegate (IntPtr hWnd, int lParam)
            {
                //ignore the shell window
                if (hWnd == hShellWindow)
                {
                    return true;
                }

                //ignore non-visible windows
                if (!IsWindowVisible(hWnd))
                {
                    return true;
                }

                //ignore windows with no text
                int length = GetWindowTextLength(hWnd);
                if (length == 0)
                {
                    return true;
                }

                uint windowPid;
                GetWindowThreadProcessId(hWnd, out windowPid);

                //ignore windows from a different process
                if (windowPid != processID)
                {
                    return true;
                }

                StringBuilder stringBuilder = new StringBuilder(length);
                GetWindowText(hWnd, stringBuilder, length + 1);
                dictWindows.Add(hWnd, stringBuilder.ToString());

                return true;

            }, 0);

            return dictWindows;
        }

        private void RefreshProcessesList_OnClick(object sender, RoutedEventArgs e)
        {
            UpdateProcessesList();
        }
        
    }
}
