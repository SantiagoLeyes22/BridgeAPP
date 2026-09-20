using System.Runtime.InteropServices;
using Bridge.Models;
using Bridge.Services.Infrastructure;

namespace Bridge.Services.Windows;

public sealed class ClipboardService : IClipboardService
{
    private const int ClipboardRetries = 8;
    private const int ClipboardRetryDelayMilliseconds = 25;
    private const int CopyTimeoutMilliseconds = 1_200;
    private readonly IInputSimulationService _input;
    private readonly IForegroundWindowService _foregroundWindow;
    private readonly IDiagnosticLogger _logger;

    public ClipboardService(
        IInputSimulationService input,
        IForegroundWindowService foregroundWindow,
        IDiagnosticLogger logger)
    {
        _input = input;
        _foregroundWindow = foregroundWindow;
        _logger = logger;
    }

    public async Task<SelectedTextCapture?> CaptureSelectedTextAsync(
        nint sourceWindow,
        CancellationToken cancellationToken)
    {
        sourceWindow = sourceWindow != 0 ? sourceWindow : _foregroundWindow.GetForegroundWindow();
        if (sourceWindow == 0)
        {
            _logger.Error("No source window was available for selection capture.");
            return null;
        }

        if (!await _input.WaitForShortcutReleaseAsync(cancellationToken))
        {
            _logger.Error("Selection capture timed out while waiting for shortcut keys to be released.");
            return null;
        }

        if (_foregroundWindow.GetForegroundWindow() != sourceWindow)
        {
            if (!_foregroundWindow.RestoreForegroundWindow(sourceWindow))
            {
                _logger.Error("The source window could not be restored before selection capture.");
                return null;
            }

            await Task.Delay(150, cancellationToken);
        }

        var previous = await CaptureSnapshotAsync(cancellationToken);
        var sequence = NativeMethods.GetClipboardSequenceNumber();

        if (!_input.SendCopy())
        {
            _logger.Error("Copy input simulation failed.");
            return null;
        }

        var changed = await WaitForSequenceChangeAsync(sequence, cancellationToken);
        if (!changed)
        {
            return null;
        }

        var selectedText = await ReadTextAsync(cancellationToken);
        await RestoreTextSnapshotAsync(previous, cancellationToken);

        if (string.IsNullOrWhiteSpace(selectedText.Text))
        {
            return null;
        }

        return new SelectedTextCapture(selectedText.Text, sourceWindow);
    }

    public Task CopyTextAsync(string text, CancellationToken cancellationToken) =>
        WriteTextAsync(text, cancellationToken);

    public async Task<bool> ReplaceSelectionAsync(
        nint sourceWindow,
        string translatedText,
        CancellationToken cancellationToken)
    {
        var previous = await CaptureSnapshotAsync(cancellationToken);
        await WriteTextAsync(translatedText, cancellationToken);

        if (!_foregroundWindow.RestoreForegroundWindow(sourceWindow))
        {
            // Keep the translation on the clipboard as the safe fallback.
            return false;
        }

        await Task.Delay(120, cancellationToken);
        if (!_input.SendPaste())
        {
            return false;
        }

        await Task.Delay(180, cancellationToken);
        await RestoreTextSnapshotAsync(previous, cancellationToken);
        return true;
    }

    private static async Task<bool> WaitForSequenceChangeAsync(
        uint originalSequence,
        CancellationToken cancellationToken)
    {
        var deadline = Environment.TickCount64 + CopyTimeoutMilliseconds;
        while (Environment.TickCount64 < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (NativeMethods.GetClipboardSequenceNumber() != originalSequence)
            {
                return true;
            }

            await Task.Delay(30, cancellationToken);
        }

        return false;
    }

    private static async Task<ClipboardTextSnapshot> ReadTextAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < ClipboardRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (NativeMethods.OpenClipboard(0))
            {
                try
                {
                    var hasText = NativeMethods.IsClipboardFormatAvailable(NativeMethods.CfUnicodeText);
                    if (!hasText)
                    {
                        return new ClipboardTextSnapshot(false, string.Empty);
                    }

                    var memory = NativeMethods.GetClipboardData(NativeMethods.CfUnicodeText);
                    if (memory == 0)
                    {
                        return new ClipboardTextSnapshot(true, string.Empty);
                    }

                    var pointer = NativeMethods.GlobalLock(memory);
                    if (pointer == 0)
                    {
                        return new ClipboardTextSnapshot(true, string.Empty);
                    }

                    try
                    {
                        return new ClipboardTextSnapshot(true, Marshal.PtrToStringUni(pointer) ?? string.Empty);
                    }
                    finally
                    {
                        NativeMethods.GlobalUnlock(memory);
                    }
                }
                finally
                {
                    NativeMethods.CloseClipboard();
                }
            }

            await Task.Delay(ClipboardRetryDelayMilliseconds, cancellationToken);
        }

        throw new ExternalException("The clipboard is temporarily unavailable.");
    }

    private static async Task WriteTextAsync(string text, CancellationToken cancellationToken)
    {
        var bytes = checked((text.Length + 1) * sizeof(char));
        for (var attempt = 0; attempt < ClipboardRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!NativeMethods.OpenClipboard(0))
            {
                await Task.Delay(ClipboardRetryDelayMilliseconds, cancellationToken);
                continue;
            }

            nint memory = 0;
            try
            {
                if (!NativeMethods.EmptyClipboard())
                {
                    throw new ExternalException("Unable to clear the clipboard.");
                }

                memory = NativeMethods.GlobalAlloc(NativeMethods.GmemMoveable, (nuint)bytes);
                if (memory == 0)
                {
                    throw new OutOfMemoryException("Unable to allocate clipboard memory.");
                }

                var pointer = NativeMethods.GlobalLock(memory);
                if (pointer == 0)
                {
                    throw new ExternalException("Unable to lock clipboard memory.");
                }

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, pointer, text.Length);
                    Marshal.WriteInt16(pointer, text.Length * sizeof(char), 0);
                }
                finally
                {
                    NativeMethods.GlobalUnlock(memory);
                }

                if (NativeMethods.SetClipboardData(NativeMethods.CfUnicodeText, memory) == 0)
                {
                    throw new ExternalException("Unable to set clipboard text.");
                }

                memory = 0; // The clipboard owns the memory after SetClipboardData succeeds.
                return;
            }
            finally
            {
                if (memory != 0)
                {
                    NativeMethods.GlobalFree(memory);
                }

                NativeMethods.CloseClipboard();
            }
        }

        throw new ExternalException("The clipboard is temporarily unavailable.");
    }

    private static async Task<ClipboardSnapshot> CaptureSnapshotAsync(CancellationToken cancellationToken)
    {
        System.Runtime.InteropServices.ComTypes.IDataObject? dataObject = null;
        if (NativeMethods.OleGetClipboard(out var captured) >= 0)
        {
            dataObject = captured;
        }

        var text = await ReadTextAsync(cancellationToken);
        return new ClipboardSnapshot(dataObject, text);
    }

    private static async Task RestoreTextSnapshotAsync(
        ClipboardSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (snapshot.DataObject is not null && NativeMethods.OleSetClipboard(snapshot.DataObject) >= 0)
        {
            NativeMethods.OleFlushClipboard();
            return;
        }

        if (snapshot.Text.HasUnicodeText)
        {
            await WriteTextAsync(snapshot.Text.Text, cancellationToken);
            return;
        }

        for (var attempt = 0; attempt < ClipboardRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (NativeMethods.OpenClipboard(0))
            {
                try
                {
                    NativeMethods.EmptyClipboard();
                    return;
                }
                finally
                {
                    NativeMethods.CloseClipboard();
                }
            }

            await Task.Delay(ClipboardRetryDelayMilliseconds, cancellationToken);
        }
    }

    private sealed record ClipboardTextSnapshot(bool HasUnicodeText, string Text);

    private sealed record ClipboardSnapshot(
        System.Runtime.InteropServices.ComTypes.IDataObject? DataObject,
        ClipboardTextSnapshot Text);
}
