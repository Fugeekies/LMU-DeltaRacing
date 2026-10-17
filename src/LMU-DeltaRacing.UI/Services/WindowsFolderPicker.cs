using System.Runtime.InteropServices;
using System.Text;

namespace LMU_DeltaRacing.UI.Services;

/// <summary>
/// Opens the native Windows folder picker for the local dashboard.
/// </summary>
public sealed class WindowsFolderPicker
{
    private const uint BrowseForFolderFlags = 0x0001 | 0x0010 | 0x0040;
    private const uint BrowseInitialized = 1;
    private const uint SetInitialSelection = 0x0467;

    /// <summary>
    /// Shows a native folder picker on an STA thread.
    /// </summary>
    /// <param name="initialPath">The folder initially selected, if it exists.</param>
    /// <returns>The selected absolute path, or <see langword="null"/> if the dialog was cancelled.</returns>
    public Task<string?> PickFolderAsync(string? initialPath = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("The native results-folder picker is available only on Windows.");
        }

        var completion = new TaskCompletionSource<string?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                completion.SetResult(PickFolder(initialPath));
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    private static string? PickFolder(string? initialPath)
    {
        const int displayNameCapacity = 32768;
        var displayName = Marshal.AllocHGlobal(displayNameCapacity * sizeof(char));
        var initialPathPointer = string.IsNullOrWhiteSpace(initialPath)
            ? IntPtr.Zero
            : Marshal.StringToHGlobalUni(initialPath);
        BrowseCallback? callback = initialPathPointer == IntPtr.Zero
            ? null
            : (window, message, _, _) =>
            {
                if (message == BrowseInitialized)
                {
                    _ = SendMessage(window, SetInitialSelection, new IntPtr(1), initialPathPointer);
                }

                return 0;
            };

        try
        {
            var info = new BrowseInfo
            {
                DisplayName = displayName,
                Title = "Sélectionnez le dossier des résultats LMU",
                Flags = BrowseForFolderFlags,
                Callback = callback
            };
            var item = SHBrowseForFolder(ref info);
            if (item == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var path = new StringBuilder(displayNameCapacity);
                if (!SHGetPathFromIDListEx(item, path, (uint)path.Capacity, 0))
                {
                    throw new IOException("Le dossier sélectionné ne possède pas de chemin de fichiers valide.");
                }

                return path.ToString();
            }
            finally
            {
                ILFree(item);
            }
        }
        finally
        {
            GC.KeepAlive(callback);
            Marshal.FreeHGlobal(displayName);
            if (initialPathPointer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(initialPathPointer);
            }
        }
    }

    [DllImport("shell32.dll", EntryPoint = "SHBrowseForFolderW", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHBrowseForFolder(ref BrowseInfo browseInfo);

    [DllImport("shell32.dll", EntryPoint = "SHGetPathFromIDListEx", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SHGetPathFromIDListEx(
        IntPtr item,
        StringBuilder path,
        uint pathCapacity,
        uint flags);

    [DllImport("shell32.dll", EntryPoint = "ILFree")]
    private static extern void ILFree(IntPtr item);

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int BrowseCallback(IntPtr window, uint message, IntPtr lParam, IntPtr parameter);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct BrowseInfo
    {
        public IntPtr Owner;
        public IntPtr Root;
        public IntPtr DisplayName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Title;

        public uint Flags;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public BrowseCallback? Callback;

        public IntPtr Parameter;
        public int Image;
    }
}
