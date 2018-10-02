using System;
using System.ComponentModel;
using System.Linq;

namespace Totem.IO
{
  /// <summary>
  /// A rooted reference to a file resource
  /// </summary>
  [TypeConverter(typeof(Converter))]
  public sealed class FileLink : IOLink, IEquatable<FileLink>
  {
    FileLink(FolderLink folder, FileName name)
    {
      Folder = folder;
      Name = name;
    }

    public readonly FolderLink Folder;
    public readonly FileName Name;

    public override bool IsTemplate =>
      Folder.IsTemplate || Name.IsTemplate;

    public override string ToString(bool altSlash = false) =>
      Folder.ToString(altSlash, trailing: true).ToText() + Name;

    public FileResource RelativeTo(FolderLink folder) =>
      FileResource.From(Folder.RelativeTo(folder), Name);

    public FileLink Up(int count = 1, bool strict = true) =>
      new FileLink(Folder.Up(count, strict), Name);

    //
    // Equality
    //

    public override bool Equals(object obj) =>
      Equals(obj as FileLink);

    public bool Equals(FileLink other) =>
      Eq.Values(this, other).Check(x => x.Folder).Check(x => x.Name);

    public override int GetHashCode() =>
      HashCode.Combine(Folder, Name);

    public static bool operator ==(FileLink x, FileLink y) => Eq.Op(x, y);
    public static bool operator !=(FileLink x, FileLink y) => Eq.OpNot(x, y);

    //
    // Factory
    //

    public static FileLink From(FolderLink folder, FileResource file) =>
      new FileLink(folder.Then(file.Folder), file.Name);

    public static FileLink From(FolderLink folder, string file, bool strict = true)
    {
      var parsedFile = FileResource.From(file, strict);

      return parsedFile == null ? null : From(folder, parsedFile);
    }

    public static FileLink From(string folder, FileResource file, bool strict = true)
    {
      var parsedFolder = FolderLink.From(folder, strict);

      return parsedFolder == null ? null : From(parsedFolder, file);
    }

    public static FileLink From(string folder, string file, bool strict = true)
    {
      var parsedFolder = FolderLink.From(folder, strict);

      return parsedFolder == null ? null : From(parsedFolder, file, strict);
    }

    public new static FileLink From(string value, bool strict = true, bool extensionOptional = false)
    {
      var parsedFolder = FolderLink.From(value, strict);

      if(parsedFolder != null)
      {
        var fileSegment = parsedFolder.Resource.Path.Segments.LastOrDefault();

        if(fileSegment != null)
        {
          var parsedName = FileName.From(fileSegment.ToString(), strict, extensionOptional);

          if(parsedName != null)
          {
            return new FileLink(parsedFolder.Up(strict: false), parsedName);
          }
        }
      }

      Expect.False(strict, "Cannot parse file link");

      return null;
    }

    public new sealed class Converter : TextConverter
    {
      protected override object ConvertFrom(TextValue value) =>
        From(value);
    }
  }
}