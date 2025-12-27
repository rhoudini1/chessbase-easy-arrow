using System.Configuration;
using System.Data;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;

namespace ChessEasyDraw;

/// <summary>
/// WPF utility that intercepts mouse right-clicks
/// and adds Alt+Shift modifiers automatically.
/// </summary>
public partial class App : System.Windows.Application
{
    private static LowLevelMouseProc _mouseProc = MouseHookCallback;

    private static IntPtr _mouseHookID = IntPtr.Zero;

    private static bool _rightButtonDown = false;

    private NotifyIcon _notifyIcon;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // IMPORTANT: hides main window (no visible interface)
        // But we need a window for WPF dispatcher to work
        MainWindow = new MainWindow();
        MainWindow.Visibility = Visibility.Hidden;
        MainWindow.ShowInTaskbar = false;

        // Installs mouse global hook
        _mouseHookID = SetHook(_mouseProc);

        SetupNotifyIcon();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        ReleaseKeys();

        if (_mouseHookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookID);
        }

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }

    #region Mouse Hook Logic

    private static IntPtr SetHook(LowLevelMouseProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(
                WH_MOUSE_LL,                    // Low level mouse hook
                proc,                            // Callback function
                GetModuleHandle(curModule.ModuleName), // Module handle
                0                                // Thread ID 0 = global
            );
        }
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Callback for all system mouse events.
    /// Intercepts right click and adds modifiers.
    /// </summary>
    private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        bool shouldProcessEvent = nCode >= 0;
        if (shouldProcessEvent)
        {
            // Converts pointer to struct
            MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

            // ===== RIGHT BUTTON PRESSED =====
            if (wParam == (IntPtr)WM_RBUTTONDOWN)
            {
                if (!_rightButtonDown)
                {
                    _rightButtonDown = true;

                    // Presses Alt + Shift before the click
                    keybd_event(VK_MENU, 0, 0, 0);
                    keybd_event(VK_SHIFT, 0, 0, 0);

                    // Allows right click to continue
                    return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
                }
            }
            // ===== RIGHT BUTTON RELEASED =====
            else if (wParam == (IntPtr)WM_RBUTTONUP)
            {
                if (_rightButtonDown)
                {
                    _rightButtonDown = false;

                    // Allows release to continue
                    IntPtr result = CallNextHookEx(_mouseHookID, nCode, wParam, lParam);

                    ReleaseKeys();

                    return result;
                }
            }
        }

        // Passes the event to the next hook in the chain
        return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
    }

    private static void ReleaseKeys()
    {
        // F24 trick: avoids the Alt menu to show up
        keybd_event(VK_F24, 0, 0, 0);
        keybd_event(VK_F24, 0, KEYEVENTF_KEYUP, 0);

        keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, 0);
        keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, 0);
    }

    #endregion

    #region Configuração do NotifyIcon

    private void SetupNotifyIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = LoadIconFromResource("ChessEasyDraw.Resources.ChessEasyClickIcon.ico"),
            Visible = true,
            Text = "Right Click Modifier.\nCtrl+Shift active for right-click.\nRight-click to exit."
        };

        var contextMenu = new ContextMenuStrip();

        // Item: Status
        var statusItem = new ToolStripMenuItem("✓ Active modifier");
        statusItem.Enabled = false;
        contextMenu.Items.Add(statusItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        // Item: Exit
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) =>
        {
            Current.Shutdown();
        };
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;

        // Close on double click too
        _notifyIcon.DoubleClick += (s, e) => Current.Shutdown();
    }

    /// <summary>
    /// Loads icon from assembly embedded resource.
    /// </summary>
    /// <param name="resourceName">Full resource name (namespace.folder.file)</param>
    /// <returns>Loaded icon or standard icon if there's error</returns>
    private System.Drawing.Icon LoadIconFromResource(string resourceName)
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    return new System.Drawing.Icon(stream);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Icon not found: {resourceName}");
                    return System.Drawing.SystemIcons.Application;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load icon: {ex.Message}");
            return System.Drawing.SystemIcons.Application;
        }
    }

    #endregion

    #region Windows API

    // === CONSTANTS ===

    private const int WH_MOUSE_LL = 14;           // Low level mouse hook
    private const int WM_RBUTTONDOWN = 0x0204;    // Message: right button pressed
    private const int WM_RBUTTONUP = 0x0205;      // Message: right button released

    private const byte VK_SHIFT = 0x10;           // Virtual Key: Shift
    private const byte VK_MENU = 0x12;            // Virtual Key: Alt (Menu)
    private const byte VK_F24 = 0x87;             // Virtual Key: F24
    private const uint KEYEVENTF_KEYUP = 0x0002;  // Flag: key being released

    // === STRUCTS ===

    /// <summary>
    /// Info about low level mouse event.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;              // x,y mouse coordinates
        public uint mouseData;        // Aditional data (scroll, extra buttons)
        public uint flags;            // Event Flags
        public uint time;             // Event Timestamp
        public IntPtr dwExtraInfo;    // Extra info
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    // === WIN32 API FUNCTIONS ===

    /// <summary>
    /// Installs an application hook in the system hook chain.
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,                // Hook type
        LowLevelMouseProc lpfn,    // Callback function
        IntPtr hMod,               // Module Handle
        uint dwThreadId            // Thread ID (0 = global)
    );

    /// <summary>
    /// Removes an installed hook.
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    /// <summary>
    /// Passes information to the next hook in the chain.
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(
        IntPtr hhk,       // Hook handle
        int nCode,        // Hook code
        IntPtr wParam,    // Aditional parameter
        IntPtr lParam     // Aditional parameter
    );

    /// <summary>
    /// Obtains a handle to the specified module.
    /// </summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    /// <summary>
    /// Synthesizes a keyboard event (key press or release).
    /// </summary>
    [DllImport("user32.dll")]
    private static extern void keybd_event(
        byte bVk,          // Key virtual code
        byte bScan,        // Scan code (usually 0)
        uint dwFlags,      // Flags (0 = press, KEYEVENTF_KEYUP = release)
        int dwExtraInfo    // Extra info (usually 0)
    );

    #endregion
}
