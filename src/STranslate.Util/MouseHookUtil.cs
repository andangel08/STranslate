using System.Diagnostics;
using System.Runtime.InteropServices;

namespace STranslate.Util;

public class MouseHookUtil : GlobalHook
{
    // 构造函数，设置钩子类型为14，即鼠标钩子
    public MouseHookUtil()
    {
        _hookType = 14;
    }
    // 鼠标按下事件
    public event MouseEventHandler? MouseDown;
    // 鼠标抬起事件
    public event MouseEventHandler? MouseUp;
    // 鼠标移动事件
    public event MouseEventHandler? MouseMove;
    // 鼠标滚轮事件
    public event MouseEventHandler? MouseWheel;
    // 鼠标单击事件
    public event EventHandler? Click;
    // 鼠标双击事件
    public event EventHandler? DoubleClick;
    // 钩子回调函数
    protected override int HookCallbackProcedure(int nCode, int wParam, IntPtr lParam)
    {
        // 如果nCode大于-1且MouseDown、MouseUp、MouseMove事件不为空
        if (nCode > -1 && (MouseDown != null || MouseUp != null || MouseMove != null))
        {
            // 将lParam转换为MouseLLHookStruct结构体
            var mouseLLHookStruct = (MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseLLHookStruct))!;
            // 获取鼠标按键
            var button = GetButton(wParam);
            // 获取鼠标事件类型
            var mouseEventType = GetEventType(wParam);
            // 创建鼠标事件参数
            var e = new MouseEventArgs(
                button,
                mouseEventType != MouseEventType.DoubleClick ? 1 : 2,
                mouseLLHookStruct.pt.x,
                mouseLLHookStruct.pt.y,
                mouseEventType == MouseEventType.MouseWheel ? (short)((mouseLLHookStruct.mouseData >> 16) & 0xFFFF) : 0
            );
            // 如果鼠标右键按下且flags不为0，则鼠标事件类型为None
            if (button == MouseButtons.Right && mouseLLHookStruct.flags != 0) mouseEventType = MouseEventType.None;
            // 根据鼠标事件类型触发相应的事件
            switch (mouseEventType)
            {
                case MouseEventType.MouseDown:
                    MouseDown?.Invoke(this, e);
                    break;

                case MouseEventType.MouseUp:
                    Click?.Invoke(this, new EventArgs());
                    MouseUp?.Invoke(this, e);
                    break;

                case MouseEventType.DoubleClick:
                    DoubleClick?.Invoke(this, new EventArgs());
                    break;

                case MouseEventType.MouseWheel:
                    MouseWheel?.Invoke(this, e);
                    break;

                case MouseEventType.MouseMove:
                    MouseMove?.Invoke(this, e);
                    break;
            }
        }
        // 调用下一个钩子
        return CommonUtil.CallNextHookEx(_handleToHook, nCode, wParam, lParam);
    }
    // 获取鼠标按键
    private MouseButtons GetButton(int wParam)
    {
        return wParam switch
        {
            513 or 514 or 515 => MouseButtons.Left,
            516 or 517 or 518 => MouseButtons.Right,
            519 or 520 or 521 => MouseButtons.Middle,
            _ => MouseButtons.None
        };
    }
    // 获取鼠标事件类型
    private MouseEventType GetEventType(int wParam)
    {
        return wParam switch
        {
            513 or 516 or 519 => MouseEventType.MouseDown,
            514 or 517 or 520 => MouseEventType.MouseUp,
            515 or 518 or 521 => MouseEventType.DoubleClick,
            522 => MouseEventType.MouseWheel,
            512 => MouseEventType.MouseMove,
            _ => MouseEventType.None
        };
    }
    // 鼠标事件类型枚举
    private enum MouseEventType
    {
        None,
        MouseDown,
        MouseUp,
        DoubleClick,
        MouseWheel,
        MouseMove
    }
}
// 全局钩子抽象类
public abstract class GlobalHook
{
    // 鼠标钩子类型
    protected const int WH_MOUSE_LL = 14;
    // 键盘钩子类型
    protected const int WH_KEYBOARD_LL = 13;
    // 鼠标钩子类型
    protected const int WH_MOUSE = 7;
    // 键盘钩子类型
    protected const int WH_KEYBOARD = 2;
    // 鼠标移动消息
    protected const int WM_MOUSEMOVE = 512;
    // 鼠标左键按下消息
    protected const int WM_LBUTTONDOWN = 513;
    // 鼠标右键按下消息
    protected const int WM_RBUTTONDOWN = 516;
    // 鼠标中键按下消息
    protected const int WM_MBUTTONDOWN = 519;
    // 鼠标左键抬起消息
    protected const int WM_LBUTTONUP = 514;
    // 鼠标右键抬起消息
    protected const int WM_RBUTTONUP = 517;
    // 鼠标中键抬起消息
    protected const int WM_MBUTTONUP = 520;
    // 鼠标左键双击消息
    protected const int WM_LBUTTONDBLCLK = 515;
    // 鼠标右键双击消息
    protected const int WM_RBUTTONDBLCLK = 518;
    // 鼠标中键双击消息
    protected const int WM_MBUTTONDBLCLK = 521;
    // 鼠标滚轮消息
    protected const int WM_MOUSEWHEEL = 522;
    // 键盘按下消息
    protected const int WM_KEYDOWN = 256;
    // 键盘抬起消息
    protected const int WM_KEYUP = 257;
    // 系统键盘按下消息
    protected const int WM_SYSKEYDOWN = 260;
    // 系统键盘抬起消息
    protected const int WM_SYSKEYUP = 261;
    // Shift键
    protected const byte VK_SHIFT = 16;
    // Caps Lock键
    protected const byte VK_CAPITAL = 20;
    // Num Lock键
    protected const byte VK_NUMLOCK = 144;
    // 左Shift键
    protected const byte VK_LSHIFT = 160;
    // 右Shift键
    protected const byte VK_RSHIFT = 161;
    // 左Ctrl键
    protected const byte VK_LCONTROL = 162;
    // 右Ctrl键
    protected const byte VK_RCONTROL = 3;
    // 左Alt键
    protected const byte VK_LALT = 164;
    // 右Alt键
    protected const byte VK_RALT = 165;
    // Alt键按下标志
    protected const byte LLKHF_ALTDOWN = 32;
    // 钩子句柄
    protected int _handleToHook;
    // 钩子回调函数
    protected HookProc? _hookCallback;
    // 钩子类型
    protected int _hookType;
    // 钩子是否已启动
    public bool _isStarted;
    // 构造函数，注册应用程序退出事件
    public GlobalHook()
    {
        Application.ApplicationExit += Application_ApplicationExit;
    }
    // 钩子是否已启动
    public bool IsStarted => _isStarted;

    // 设置Windows钩子
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookExW(int idHook, HookProc lpfn, IntPtr hmod, uint dwThreadID);

    // 启动钩子
    public void Start()
    {
        // 如果钩子已启动或钩子类型为0，则返回
        if (_isStarted || _hookType == 0) return;
        // 设置钩子回调函数
        _hookCallback = HookCallbackProcedure;
        // 获取当前进程
        using (var process = Process.GetCurrentProcess())
        {
            // 获取当前进程的主模块
            using var processModule = process.MainModule!;
            // 设置钩子
            _handleToHook = (int)SetWindowsHookExW(_hookType, _hookCallback,
                CommonUtil.GetModuleHandle(processModule.ModuleName), 0u);
        }
        // 如果钩子设置成功，则设置钩子已启动
        if (_handleToHook != 0) _isStarted = true;
    }
    // 停止钩子
    public void Stop()
    {
        // 如果钩子已启动，则停止钩子
        if (_isStarted)
        {
            CommonUtil.UnhookWindowsHookEx(_handleToHook);
            _isStarted = false;
        }
    }
    // 钩子回调函数
    protected virtual int HookCallbackProcedure(int nCode, int wParam, IntPtr lParam)
    {
        return 0;
    }
    // 应用程序退出事件
    protected void Application_ApplicationExit(object? sender, EventArgs e)
    {
        // 如果钩子已启动，则停止钩子
        if (_isStarted) Stop();
    }
    // POINT结构体
    [StructLayout(LayoutKind.Sequential)]
    protected class POINT
    {
        public int x;

        public int y;
    }
    // MouseHookStruct结构体
    [StructLayout(LayoutKind.Sequential)]
    protected class MouseHookStruct
    {
        public POINT pt = new();

        public int hwnd;

        public int wHitTestCode;

        public int dwExtraInfo;
    }
    // MouseLLHookStruct结构体
    [StructLayout(LayoutKind.Sequential)]
    protected class MouseLLHookStruct
    {
        public POINT pt = new();

        public int mouseData;

        public int flags;

        public int time;

        public int dwExtraInfo;
    }
    // KeyboardHookStruct结构体
    [StructLayout(LayoutKind.Sequential)]
    protected class KeyboardHookStruct
    {
        public int vkCode;

        public int scanCode;

        public int flags;

        public int time;

        public int dwExtraInfo;
    }
    // 钩子回调函数委托
    protected delegate int HookProc(int nCode, int wParam, IntPtr lParam);
}