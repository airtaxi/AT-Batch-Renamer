using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinRT.Interop;

namespace BatchRenamerAT.Helpers;

// Win32 modal-window support: a modal window takes the owner window as its Win32 owner so it
// stays above it in z-order, and the owner window stops receiving input until the modal closes.
public static partial class WindowHelper
{
    // GWLP_HWNDPARENT: the owner window slot of a top-level window.
    private const int OwnerWindowLongIndex = -8;

    public static void ActivateModal(this Window window, Window ownerWindow)
    {
        window.MakeModal(ownerWindow);
        window.Activate();
    }

    public static void MakeModal(this Window window, Window ownerWindow)
    {
        var windowHandle = WindowNative.GetWindowHandle(window);
        var ownerWindowHandle = WindowNative.GetWindowHandle(ownerWindow);

        SetWindowOwner(windowHandle, ownerWindowHandle);
        PInvoke.EnableWindow((HWND)ownerWindowHandle, false);

        window.Closed += (_, __) =>
        {
            PInvoke.EnableWindow((HWND)ownerWindowHandle, true);
            PInvoke.SetForegroundWindow((HWND)ownerWindowHandle);
        };
    }

    // user32 exports SetWindowLongPtrW only in 64-bit builds; 32-bit builds use SetWindowLongW,
    // where window handles are 32 bits wide. The entry point is resolved on first call, so the
    // missing export is never touched on x86.
    private static void SetWindowOwner(nint windowHandle, nint ownerWindowHandle)
    {
        if (Environment.Is64BitProcess) SetWindowLongPtr(windowHandle, OwnerWindowLongIndex, ownerWindowHandle);
        else SetWindowLong(windowHandle, OwnerWindowLongIndex, (int)ownerWindowHandle);
    }

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static partial nint SetWindowLongPtr(nint windowHandle, int index, nint newValue);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static partial int SetWindowLong(nint windowHandle, int index, int newValue);
}
