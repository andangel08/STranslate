using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace STranslate.Helper;

public class WindowHelper
{
    //定义窗口类名常量
    private const string WINDOW_CLASS_CONSOLE = "ConsoleWindowClass";
    private const string WINDOW_CLASS_WINTAB = "Flip3D";
    private const string WINDOW_CLASS_PROGMAN = "Progman";
    private const string WINDOW_CLASS_WORKERW = "WorkerW";

    //定义静态变量，用于存储shell和desktop的句柄
    private static IntPtr _hwnd_shell;
    private static IntPtr _hwnd_desktop;

    //获取shell句柄
    //访问器用于 shell 和桌面处理程序
    //将设置变量一次，然后返回它们
    private static IntPtr HWND_SHELL => _hwnd_shell != IntPtr.Zero ? _hwnd_shell : _hwnd_shell = GetShellWindow();

    private static IntPtr HWND_DESKTOP =>
        _hwnd_desktop != IntPtr.Zero ? _hwnd_desktop : _hwnd_desktop = GetDesktopWindow();

    /// <summary>
    ///     窗口是否可见
    /// </summary>
    /// <param name="window"></param>
    /// <returns></returns>
    public static bool IsWindowVisible(Window window)
    {
        return IsWindowVisible(new WindowInteropHelper(window).Handle);
    }

    /// <summary>
    ///     窗口是否在最上层
    /// </summary>
    /// <param name="window"></param>
    /// <returns></returns>
    public static bool IsWindowInForeground(Window window)
    {
        return new WindowInteropHelper(window).Handle == GetForegroundWindow();
    }

    /// <summary>
    ///     设置窗口在最上层
    /// </summary>
    /// <param name="window"></param>
    /// <returns></returns>
    public static bool SetWindowInForeground(Window window)
    {
        return SetForegroundWindow(new WindowInteropHelper(window).Handle);
    }

    /// <summary>
    ///     是否全屏
    /// </summary>
    /// <returns></returns>
    public static bool IsWindowFullscreen()
    {
        //获取当前活动窗口
        var hWnd = GetForegroundWindow();

        if (hWnd.Equals(IntPtr.Zero)) return false;

        //如果当前活动窗口是 Desktop 或 shell，请提前退出
        if (hWnd.Equals(HWND_DESKTOP) || hWnd.Equals(HWND_SHELL)) return false;

        var sb = new StringBuilder(256);
        GetClassName(hWnd, sb, sb.Capacity);
        var windowClass = sb.ToString();

        //用于 Win+Tab （Flip3D）
        if (windowClass == WINDOW_CLASS_WINTAB) return false;

        RECT appBounds;
        GetWindowRect(hWnd, out appBounds);

        //对于控制台 （ConsoleWindowClass），我们必须检查负维度
        if (windowClass == WINDOW_CLASS_CONSOLE) return appBounds.Top < 0 && appBounds.Bottom < 0;

        //对于桌面（Progman 或 WorkerW，取决于系统），我们必须检查
        if (windowClass is WINDOW_CLASS_PROGMAN or WINDOW_CLASS_WORKERW)
        {
            var hWndDesktop = FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
            hWndDesktop = FindWindowEx(hWndDesktop, IntPtr.Zero, "SysListView32", "FolderView");
            if (!hWndDesktop.Equals(IntPtr.Zero)) return false;
        }

        var screenBounds = Screen.FromHandle(hWnd).Bounds;
        return appBounds.Bottom - appBounds.Top == screenBounds.Height &&
               appBounds.Right - appBounds.Left == screenBounds.Width;
    }

    [DllImport("user32.dll")]
    internal static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    internal static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int GetWindowRect(IntPtr hwnd, out RECT rc);

    [DllImport("user32.DLL")]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
        string? lpszWindow);

    [DllImport("user32.dll")]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("User32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }


    #region Alt Tab 页面
    //定义窗口扩展样式常量
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern int IntSetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
    private static extern void SetLastError(int dwErrorCode);
    //设置窗口扩展样式
    private static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        SetLastError(0); // 清除任何现有错误

        if (IntPtr.Size == 4) return new IntPtr(IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong)));

        return IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
    }
    //将IntPtr转换为int
    private static int IntPtrToInt32(IntPtr intPtr)
    {
        return unchecked((int)intPtr.ToInt64());
    }

    /// <summary>
    ///     从Alt+Tab窗口列表中隐藏窗口
    /// </summary>
    /// <param name="window">要隐藏的窗口</param>
    public static void HideFromAltTab(Window window)
    {
        var helper = new WindowInteropHelper(window);
        var exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE).ToInt32();

        // 添加 TOOLWINDOW 样式，移除 APPWINDOW 样式
        exStyle = (exStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW;

        SetWindowLong(helper.Handle, GWL_EXSTYLE, new IntPtr(exStyle));
    }

    /// <summary>
    ///     恢复窗口在Alt+Tab窗口列表中的显示
    /// </summary>
    /// <param name="window">要恢复显示的窗口</param>
    public static void ShowInAltTab(Window window)
    {
        var helper = new WindowInteropHelper(window);
        var exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE).ToInt32();

        // 移除 TOOLWINDOW 样式，添加 APPWINDOW 样式
        exStyle = (exStyle & ~WS_EX_TOOLWINDOW) | WS_EX_APPWINDOW;

        SetWindowLong(helper.Handle, GWL_EXSTYLE, new IntPtr(exStyle));
    }

    /// <summary>
    ///     获取窗口当前的扩展样式
    /// </summary>
    /// <param name="window">要获取样式的窗口</param>
    /// <returns>当前的扩展样式值</returns>
    public static int GetCurrentWindowStyle(Window window)
    {
        var helper = new WindowInteropHelper(window);
        return GetWindowLong(helper.Handle, GWL_EXSTYLE).ToInt32();
    }

    #endregion
}