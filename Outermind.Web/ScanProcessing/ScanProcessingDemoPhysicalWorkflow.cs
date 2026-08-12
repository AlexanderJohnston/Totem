using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Options;

namespace Quantum.Web.ScanProcessing
{
  /// <summary>
  /// Disposable demo-only filesystem adapter. Production startup refuses to enable its demo surface.
  /// </summary>
  public sealed class ScanProcessingDemoPhysicalWorkflow
  {
    static readonly IReadOnlyDictionary<string, QpfSetting> QpfSettings =
      new Dictionary<string, QpfSetting>(StringComparer.OrdinalIgnoreCase)
      {
        ["contrast"] = new("ProcessSoftwareContrast", Integer(-255, 255)),
        ["brightness"] = new("ProcessSoftwareBrightness", Integer(-255, 255)),
        ["gamma"] = new("ProcessSoftwareGamma", Integer(-255, 255)),
        ["sharpen"] = new("ProcessSharpen", Integer(0, 10)),
        ["auto-crop"] = new("ProcessCrop", Boolean()),
        ["auto-deskew"] = new("ProcessDeskew", Boolean()),
        ["rotate"] = new("ProcessRotate", Enum("0", "90", "180", "270")),
        ["flip"] = new("ProcessFlip", Integer(0, 2)),
        ["save-grayscale"] = new("ProcessSaveGrayscale", Boolean()),
        ["save-bitonal"] = new("ProcessSaveBitonal", Boolean()),
        ["grayscale-format"] = new("ProcessGrayscaleFormat", Integer(0, 5)),
        ["bitonal-format"] = new("ProcessBitonalFormat", Integer(0, 4)),
        ["crop-border"] = new("ProcessCropBorderSize", Integer(0, 150)),
        ["crop-threshold"] = new("ProcessCropThreshold", Integer(0, 128)),
        ["deskew-quality"] = new("ProcessDeskewTransformQuality", Integer(0, 5))
      };

    readonly ScanProcessingDemoOptions _options;

    public ScanProcessingDemoPhysicalWorkflow(IOptions<ScanProcessingDemoOptions> options)
    {
      _options = options.Value;
    }

    public bool Enabled => _options.PhysicalWorkflowEnabled;

    public ScanProcessingDemoResult<string> CreateScanFolder(string folderName)
    {
      var rootResult = ResolveRoot();
      if(!rootResult.Succeeded)
      {
        return ConvertFailure<string>(rootResult);
      }

      try
      {
        var root = rootResult.Value;
        var target = Path.GetFullPath(Path.Combine(root, folderName));
        var relative = Path.GetRelativePath(root, target);

        if(Path.IsPathRooted(relative)
          || relative == ".."
          || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
          || relative.Contains(Path.DirectorySeparatorChar)
          || relative.Contains(Path.AltDirectorySeparatorChar))
        {
          return ScanProcessingDemoResult<string>.Failure(
            ScanProcessingDemoIssueCodes.ScanFolderUnavailable,
            "The configured demo scan target is not one direct child of the demo root.",
            "folderName");
        }

        if(Directory.Exists(target) || File.Exists(target))
        {
          return ScanProcessingDemoResult<string>.Failure(
            ScanProcessingDemoIssueCodes.ScanFolderCollision,
            "The demo scan folder already exists. Reset with a new folder name or move the previous rehearsal data.",
            "folderName",
            409);
        }

        Directory.CreateDirectory(target);
        return ScanProcessingDemoResult<string>.Success(target, 201);
      }
      catch(Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
      {
        return ScanProcessingDemoResult<string>.Failure(
          ScanProcessingDemoIssueCodes.ScanFolderUnavailable,
          "The demo scan folder could not be created. Verify the configured root and Web process permissions.",
          "folderName",
          503);
      }
    }

    public ScanProcessingDemoResult<ScanProcessingDemoIdfInspection> InspectIdf(string folderPath)
    {
      var folder = ValidateKnownFolder(folderPath);
      if(!folder.Succeeded)
      {
        return ConvertFailure<ScanProcessingDemoIdfInspection>(folder);
      }

      var files = TopLevelFiles(folder.Value, ".idf");
      if(files.Count == 0)
      {
        return ScanProcessingDemoResult<ScanProcessingDemoIdfInspection>.Failure(
          ScanProcessingDemoIssueCodes.IdfNotFound,
          "Finish requires exactly one top-level IDF in the demo scan folder.",
          "idf",
          409);
      }

      if(files.Count > 1)
      {
        return ScanProcessingDemoResult<ScanProcessingDemoIdfInspection>.Failure(
          ScanProcessingDemoIssueCodes.IdfAmbiguous,
          "Finish found more than one top-level IDF. Leave exactly one IDF in the demo scan folder.",
          "idf",
          409);
      }

      try
      {
        var document = LoadXml(files[0]);
        if(document.Root?.Name.LocalName != "Roll")
        {
          throw new XmlException("The document root must be Roll.");
        }

        var count = 0;
        foreach(var detection in DirectDetectionSettings(document))
        {
          var blipDetection = ParseInteger(detection.Attribute("BlipDetection")?.Value, 0);
          count += detection.Elements()
            .Count(element => element.Name.LocalName == "Frame"
              && (blipDetection <= 0 || !ParseBoolean(element.Attribute("IsBlip")?.Value)));
        }

        return ScanProcessingDemoResult<ScanProcessingDemoIdfInspection>.Success(
          new ScanProcessingDemoIdfInspection(count, Path.GetFileName(files[0])));
      }
      catch(Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException or FormatException)
      {
        return ScanProcessingDemoResult<ScanProcessingDemoIdfInspection>.Failure(
          ScanProcessingDemoIssueCodes.IdfInvalid,
          "The demo IDF could not be parsed and counted.",
          "idf",
          422);
      }
    }

    public ScanProcessingDemoResult<ScanProcessingDemoQpfApplyResult> ApplyQpfSettings(
      ScanProcessingDemoPlanExecution plan)
    {
      if(plan == null || !string.Equals(plan.Purpose, "qpf-settings", StringComparison.Ordinal))
      {
        return ScanProcessingDemoResult<ScanProcessingDemoQpfApplyResult>.Failure(
          ScanProcessingDemoIssueCodes.InvalidRequest,
          "This demo physical apply supports only qpf-settings plans.",
          "purpose");
      }

      var folder = ValidateKnownFolder(plan.FolderPath);
      if(!folder.Succeeded)
      {
        return ConvertFailure<ScanProcessingDemoQpfApplyResult>(folder);
      }

      var normalized = NormalizeSettings(plan.Settings);
      if(!normalized.Succeeded)
      {
        return ConvertFailure<Dictionary<string, string>, ScanProcessingDemoQpfApplyResult>(normalized);
      }

      var files = TopLevelFiles(folder.Value, ".qpf");
      if(files.Count == 0)
      {
        return ScanProcessingDemoResult<ScanProcessingDemoQpfApplyResult>.Failure(
          ScanProcessingDemoIssueCodes.QpfNotFound,
          "Apply requires exactly one top-level QPF in the demo scan folder.",
          "qpf",
          409);
      }

      if(files.Count > 1)
      {
        return ScanProcessingDemoResult<ScanProcessingDemoQpfApplyResult>.Failure(
          ScanProcessingDemoIssueCodes.QpfAmbiguous,
          "Apply found more than one top-level QPF. Leave exactly one QPF in the demo scan folder.",
          "qpf",
          409);
      }

      var qpfPath = files[0];
      var stagePath = $"{qpfPath}.demo-stage-{Guid.NewGuid():N}.tmp";
      var backupPath = $"{qpfPath}.demo-backup-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}";

      try
      {
        var sourceHash = Hash(qpfPath);
        var document = LoadXml(qpfPath);
        if(document.Root?.Name.LocalName != "Roll")
        {
          throw new XmlException("The document root must be Roll.");
        }

        var targets = DirectDetectionSettings(document).ToList();
        if(targets.Count == 0)
        {
          throw new XmlException("No direct ScannerSettings/DetectionSettings targets were found.");
        }

        foreach(var target in targets)
        {
          target.SetAttributeValue("ProcessSettingsExist", "1");
          foreach(var setting in normalized.Value)
          {
            target.SetAttributeValue(QpfSettings[setting.Key].AttributeName, setting.Value);
          }
        }

        SaveXml(document, stagePath);
        var staged = LoadXml(stagePath);
        if(DirectDetectionSettings(staged).Count() != targets.Count)
        {
          throw new XmlException("The staged QPF did not preserve its detection-settings structure.");
        }

        if(!sourceHash.SequenceEqual(Hash(qpfPath)))
        {
          throw new IOException("The QPF changed while the demo plan was being applied.");
        }

        File.Copy(qpfPath, backupPath, false);
        if(!sourceHash.SequenceEqual(Hash(backupPath)))
        {
          throw new IOException("The demo QPF backup could not be verified.");
        }

        File.Move(stagePath, qpfPath, true);
        _ = LoadXml(qpfPath);

        return ScanProcessingDemoResult<ScanProcessingDemoQpfApplyResult>.Success(
          new ScanProcessingDemoQpfApplyResult(
            Path.GetFileName(qpfPath),
            Path.GetFileName(backupPath),
            targets.Count));
      }
      catch(Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException or ArgumentException)
      {
        TryDeleteStage(stagePath);
        return ScanProcessingDemoResult<ScanProcessingDemoQpfApplyResult>.Failure(
          exception is XmlException ? ScanProcessingDemoIssueCodes.QpfInvalid : ScanProcessingDemoIssueCodes.QpfMutationFailed,
          "The demo QPF was not updated. Inspect the file shape, locks, and Web process permissions.",
          "qpf",
          422);
      }
    }

    ScanProcessingDemoResult<string> ResolveRoot()
    {
      if(!_options.PhysicalWorkflowEnabled || string.IsNullOrWhiteSpace(_options.PhysicalScanRoot))
      {
        return ScanProcessingDemoResult<string>.Failure(
          ScanProcessingDemoIssueCodes.PhysicalConfigurationRequired,
          "Set ScanProcessingDemo:PhysicalWorkflowEnabled and ScanProcessingDemo:PhysicalScanRoot for the physical demo.",
          "ScanProcessingDemo:PhysicalScanRoot",
          503);
      }

      try
      {
        var root = Path.GetFullPath(_options.PhysicalScanRoot.Trim());
        if(!Directory.Exists(root))
        {
          Directory.CreateDirectory(root);
        }

        return ScanProcessingDemoResult<string>.Success(root);
      }
      catch(Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
      {
        return ScanProcessingDemoResult<string>.Failure(
          ScanProcessingDemoIssueCodes.PhysicalConfigurationRequired,
          "The configured demo scan root is unavailable.",
          "ScanProcessingDemo:PhysicalScanRoot",
          503);
      }
    }

    ScanProcessingDemoResult<string> ValidateKnownFolder(string folderPath)
    {
      var rootResult = ResolveRoot();
      if(!rootResult.Succeeded)
      {
        return rootResult;
      }

      try
      {
        var root = rootResult.Value;
        var folder = Path.GetFullPath(folderPath ?? string.Empty);
        var relative = Path.GetRelativePath(root, folder);
        if(Path.IsPathRooted(relative)
          || relative == ".."
          || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
          || relative.Contains(Path.DirectorySeparatorChar)
          || relative.Contains(Path.AltDirectorySeparatorChar)
          || !Directory.Exists(folder))
        {
          return ScanProcessingDemoResult<string>.Failure(
            ScanProcessingDemoIssueCodes.ScanFolderUnavailable,
            "The remembered demo scan folder is unavailable or outside the configured root.",
            "scanFolder",
            409);
        }

        return ScanProcessingDemoResult<string>.Success(folder);
      }
      catch(Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
      {
        return ScanProcessingDemoResult<string>.Failure(
          ScanProcessingDemoIssueCodes.ScanFolderUnavailable,
          "The remembered demo scan folder is unavailable.",
          "scanFolder",
          409);
      }
    }

    static ScanProcessingDemoResult<Dictionary<string, string>> NormalizeSettings(
      IReadOnlyDictionary<string, string> settings)
    {
      if(settings == null || settings.Count == 0)
      {
        return ScanProcessingDemoResult<Dictionary<string, string>>.Failure(
          ScanProcessingDemoIssueCodes.InvalidRequest,
          "At least one QPF setting is required.",
          "settings");
      }

      var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      foreach(var setting in settings)
      {
        if(!QpfSettings.TryGetValue(setting.Key, out var schema))
        {
          return ScanProcessingDemoResult<Dictionary<string, string>>.Failure(
            ScanProcessingDemoIssueCodes.InvalidRequest,
            $"'{setting.Key}' is not a supported demo QPF setting.",
            $"settings.{setting.Key}");
        }

        if(!schema.Normalize(setting.Value, out var value))
        {
          return ScanProcessingDemoResult<Dictionary<string, string>>.Failure(
            ScanProcessingDemoIssueCodes.InvalidRequest,
            $"'{setting.Value}' is not valid for demo QPF setting '{setting.Key}'.",
            $"settings.{setting.Key}");
        }

        normalized[setting.Key] = value;
      }

      return ScanProcessingDemoResult<Dictionary<string, string>>.Success(normalized);
    }

    static List<string> TopLevelFiles(string folder, string extension) =>
      Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)
        .Where(path => string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase))
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ThenBy(path => path, StringComparer.Ordinal)
        .ToList();

    static XDocument LoadXml(string path)
    {
      var settings = new XmlReaderSettings
      {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreWhitespace = false
      };
      using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
      using var reader = XmlReader.Create(stream, settings);
      return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
    }

    static void SaveXml(XDocument document, string path)
    {
      var settings = new XmlWriterSettings
      {
        Encoding = new System.Text.UTF8Encoding(false),
        Indent = false,
        OmitXmlDeclaration = document.Declaration == null
      };
      using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
      using var writer = XmlWriter.Create(stream, settings);
      document.Save(writer);
      writer.Flush();
      stream.Flush(true);
    }

    static IEnumerable<XElement> DirectDetectionSettings(XDocument document) =>
      document.Root == null
        ? Enumerable.Empty<XElement>()
        : document.Root.Elements()
          .Where(element => element.Name.LocalName == "ScannerSettings")
          .SelectMany(scanner => scanner.Elements().Where(element => element.Name.LocalName == "DetectionSettings"));

    static byte[] Hash(string path)
    {
      using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
      return SHA256.HashData(stream);
    }

    static void TryDeleteStage(string path)
    {
      try
      {
        if(File.Exists(path)) File.Delete(path);
      }
      catch
      {
        // Best-effort cleanup on a disposable demo branch.
      }
    }

    static int ParseInteger(string value, int fallback) =>
      int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

    static bool ParseBoolean(string value) =>
      string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";

    static Func<string, (bool Valid, string Value)> Integer(int minimum, int maximum) => value =>
    {
      var valid = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
        && parsed >= minimum
        && parsed <= maximum;
      return (valid, valid ? parsed.ToString(CultureInfo.InvariantCulture) : null);
    };

    static Func<string, (bool Valid, string Value)> Boolean() => value =>
    {
      if(bool.TryParse(value, out var parsed)) return (true, parsed ? "1" : "0");
      return value is "0" or "1" ? (true, value) : (false, null);
    };

    static Func<string, (bool Valid, string Value)> Enum(params string[] values) => value =>
      values.Contains(value, StringComparer.Ordinal) ? (true, value) : (false, null);

    static ScanProcessingDemoResult<T> ConvertFailure<TSource, T>(ScanProcessingDemoResult<TSource> source) =>
      ScanProcessingDemoResult<T>.Failure(
        source.Issue.Code,
        source.Issue.Message,
        source.Issue.Field,
        source.StatusCode);

    static ScanProcessingDemoResult<T> ConvertFailure<T>(ScanProcessingDemoResult<string> source) =>
      ConvertFailure<string, T>(source);

    sealed record QpfSetting(
      string AttributeName,
      Func<string, (bool Valid, string Value)> Validator)
    {
      public bool Normalize(string input, out string value)
      {
        var result = Validator(input);
        value = result.Value;
        return result.Valid;
      }
    }
  }
}
