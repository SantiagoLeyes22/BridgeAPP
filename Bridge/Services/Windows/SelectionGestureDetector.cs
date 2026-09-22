namespace Bridge.Services.Windows;

public sealed class SelectionGestureDetector
{
    private const int DragDistance = 5;
    private const uint DoubleClickMilliseconds = 500;
    private bool _leftDown;
    private bool _dragged;
    private int _downX;
    private int _downY;
    private bool _hasLastClick;
    private uint _lastClickTime;
    private int _lastClickX;
    private int _lastClickY;
    private nint _lastClickWindow;
    private bool _shiftDown;
    private bool _controlDown;
    private bool _shiftNavigation;

    public void Reset()
    {
        _leftDown = false;
        _dragged = false;
        _hasLastClick = false;
        _shiftDown = false;
        _controlDown = false;
        _shiftNavigation = false;
    }

    public void MouseDown(int x, int y)
    {
        _leftDown = true;
        _dragged = false;
        _downX = x;
        _downY = y;
    }

    public void MouseMove(int x, int y)
    {
        if (_leftDown && Moved(x, y, _downX, _downY))
        {
            _dragged = true;
        }
    }

    public bool MouseUp(nint window, int x, int y, uint time)
    {
        var dragged = _leftDown && (_dragged || Moved(x, y, _downX, _downY));
        var doubleClick = _hasLastClick &&
                          _lastClickWindow == window &&
                          unchecked(time - _lastClickTime) <= DoubleClickMilliseconds &&
                          !Moved(x, y, _lastClickX, _lastClickY);
        _leftDown = false;
        _dragged = false;
        _hasLastClick = true;
        _lastClickTime = time;
        _lastClickX = x;
        _lastClickY = y;
        _lastClickWindow = window;
        return window != 0 && (dragged || doubleClick || _shiftDown);
    }

    public void KeyDown(uint key)
    {
        if (IsShift(key))
        {
            _shiftDown = true;
        }
        else if (IsControl(key))
        {
            _controlDown = true;
        }
        else if (_shiftDown && IsNavigation(key))
        {
            _shiftNavigation = true;
        }
    }

    public bool KeyUp(uint key)
    {
        if (IsShift(key))
        {
            var selected = _shiftNavigation;
            _shiftDown = false;
            _shiftNavigation = false;
            return selected;
        }

        if (IsControl(key))
        {
            _controlDown = false;
            return false;
        }

        return key == 0x41 && _controlDown && !_shiftDown; // Ctrl+A
    }

    private static bool Moved(int x, int y, int startX, int startY) =>
        Math.Abs(x - startX) >= DragDistance || Math.Abs(y - startY) >= DragDistance;

    private static bool IsShift(uint key) => key is 0x10 or 0xA0 or 0xA1;

    private static bool IsControl(uint key) => key is 0x11 or 0xA2 or 0xA3;

    private static bool IsNavigation(uint key) =>
        key is 0x25 or 0x26 or 0x27 or 0x28 or 0x21 or 0x22 or 0x23 or 0x24;
}