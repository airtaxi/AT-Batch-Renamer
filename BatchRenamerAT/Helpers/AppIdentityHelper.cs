using System.Runtime.InteropServices;

namespace BatchRenamerAT.Helpers;

public static partial class AppIdentityHelper
{
    private const long APPMODEL_ERROR_NO_PACKAGE = 15700L;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial int GetCurrentPackageId(ref int packageBufferSize, [In, Out] byte[]? packageBuffer);

    public static bool IsPackaged()
    {
        int bufferSize = 0;

        // Call with a 0 buffer size to check for package existence
        int result = GetCurrentPackageId(ref bufferSize, null);

        // If it returns NO_PACKAGE, it's unpackaged
        if (result == APPMODEL_ERROR_NO_PACKAGE) return false;

        return true;
    }
}