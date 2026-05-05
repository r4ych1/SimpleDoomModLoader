using Avalonia.Platform.Storage;

namespace ModLoader.App;

internal static class DropZonePickerOptionsFactory
{
    public static FilePickerOpenOptions Create(DropZoneKind kind)
    {
        return new FilePickerOpenOptions
        {
            AllowMultiple = true,
            Title = GetTitle(kind),
            FileTypeFilter = [CreateFileType(kind)]
        };
    }

    private static string GetTitle(DropZoneKind kind)
    {
        return kind switch
        {
            DropZoneKind.SourcePort => "Select Source Port Files",
            DropZoneKind.Iwad => "Select IWAD Files",
            DropZoneKind.Mod => "Select Mod Files",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    private static FilePickerFileType CreateFileType(DropZoneKind kind)
    {
        return kind switch
        {
            DropZoneKind.SourcePort => new FilePickerFileType("Source Port Files")
            {
                Patterns = ["*.exe"]
            },
            DropZoneKind.Iwad => new FilePickerFileType("IWAD Files")
            {
                Patterns = ["*.wad", "*.pk3", "*.iwad", "*.ipk3", "*.ipk7", "*.pk7"]
            },
            DropZoneKind.Mod => new FilePickerFileType("Mod Files")
            {
                Patterns = ["*.wad", "*.pwad", "*.pk3", "*.pk7", "*.ipk3", "*.ipk7", "*.pkz", "*.zip"]
            },
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}
