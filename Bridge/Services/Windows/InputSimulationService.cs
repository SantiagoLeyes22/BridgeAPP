using System.Runtime.InteropServices;
using Bridge.Services.Infrastructure;

namespace Bridge.Services.Windows;

public sealed class InputSimulationService : IInputSimulationService
{
    private const int ShortcutReleaseTimeoutMilliseconds = 2_000;
    private static readonly int[] ShortcutKeys = [0x10, 0x11, 0x12, 0x5B, 0x5C, 0x54, 0x0D];
    private const ushort VirtualKeyControl = 0x11;
    private const ushort VirtualKeyC = 0x43;
    private const ushort VirtualKeyV = 0x56;
    private readonly IDiagnosticLogger _logger;

    public InputSimulationService(IDiagnosticLogger logger)
    {
        _logger = logger;
    }

    public static int NativeInputSize => Marshal.SizeOf<NativeMethods.Input>();

    public bool SendCopy() => SendControlShortcut(VirtualKeyC);

    public bool SendPaste() => SendControlShortcut(VirtualKeyV);

    public async Task<bool> WaitForShortcutReleaseAsync(CancellationToken cancellationToken)
    {
        var deadline = Environment.TickCount64 + ShortcutReleaseTimeoutMilliseconds;
        while (Environment.TickCount64 < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ShortcutKeys.All(key => (NativeMethods.GetAsyncKeyState(key) & 0x8000) == 0))
            {
                return true;
            }

            await Task.Delay(15, cancellationToken);
        }

        return false;
    }

    private bool SendControlShortcut(ushort key)
    {
        NativeMethods.Input[] inputs =
        [
            CreateKeyboardInput(VirtualKeyControl, keyUp: false),
            CreateKeyboardInput(key, keyUp: false),
            CreateKeyboardInput(key, keyUp: true),
            CreateKeyboardInput(VirtualKeyControl, keyUp: true)
        ];

        var sent = NativeMethods.SendInput(
            (uint)inputs.Length,
            inputs,
            NativeInputSize);
        if (sent != inputs.Length)
        {
            _logger.Error($"SendInput sent {sent} of {inputs.Length} events. Win32={Marshal.GetLastWin32Error()}; InputSize={NativeInputSize}.");
            return false;
        }

        return true;
    }

    private static NativeMethods.Input CreateKeyboardInput(ushort virtualKey, bool keyUp) => new()
    {
        Type = NativeMethods.InputKeyboard,
        Data = new NativeMethods.InputUnion
        {
            Keyboard = new NativeMethods.KeyboardInput
            {
                VirtualKey = virtualKey,
                Flags = keyUp ? NativeMethods.KeyEventKeyUp : 0
            }
        }
    };
}
