using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Totem;

namespace Outermind.Microfilm
{
  public static class MicrofilmTableColumnTypes
  {
    public const string Text = "text";
    public const string Number = "number";
    public const string Dropdown = "dropdown";
    public const string Checkbox = "checkbox";

    public static bool IsSupported(string type) =>
      type == Text || type == Number || type == Dropdown || type == Checkbox;

    public static string Normalize(string type) =>
      (type ?? "").Trim().ToLowerInvariant();
  }

  public static class MicrofilmTableRowOrigins
  {
    public const string Regular = "regular";
    public const string Custom = "custom";
  }

  public class MicrofilmTableColumn : IEquatable<MicrofilmTableColumn>
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public List<string> DropdownOptions { get; set; } = new();
    public double? Width { get; set; }

    public MicrofilmTableColumn()
    {
    }

    public MicrofilmTableColumn(string id, string name, string type, double? width = null, IEnumerable<string> dropdownOptions = null)
    {
      Id = id;
      Name = name;
      Type = type;
      Width = width;
      DropdownOptions = dropdownOptions?.ToList() ?? new List<string>();
    }

    public MicrofilmTableColumn Clone() =>
      new(Id, Name, Type, Width, DropdownOptions);

    public bool Equals(MicrofilmTableColumn other)
    {
      if(other is null) return false;
      if(ReferenceEquals(this, other)) return true;

      return Id == other.Id
        && Name == other.Name
        && Type == other.Type
        && Width == other.Width
        && DropdownOptions.SequenceEqual(other.DropdownOptions ?? new List<string>());
    }

    public override bool Equals(object obj) => Equals(obj as MicrofilmTableColumn);
    public override int GetHashCode() => (Id ?? "").GetHashCode();
  }

  public enum MicrofilmCellValueKind
  {
    Null,
    Text,
    Number,
    Checkbox,
    Unsupported
  }

  [JsonConverter(typeof(MicrofilmCellValueJsonConverter))]
  public class MicrofilmCellValue : IEquatable<MicrofilmCellValue>
  {
    public MicrofilmCellValueKind Kind { get; set; }
    public string Text { get; set; }
    public double Number { get; set; }
    public bool Checkbox { get; set; }

    public static MicrofilmCellValue Null() => new() { Kind = MicrofilmCellValueKind.Null };
    public static MicrofilmCellValue FromText(string value) => value == null ? Null() : new() { Kind = MicrofilmCellValueKind.Text, Text = value };
    public static MicrofilmCellValue FromNumber(double value) => new() { Kind = MicrofilmCellValueKind.Number, Number = value };
    public static MicrofilmCellValue FromCheckbox(bool value) => new() { Kind = MicrofilmCellValueKind.Checkbox, Checkbox = value };
    public static MicrofilmCellValue Unsupported() => new() { Kind = MicrofilmCellValueKind.Unsupported };

    public MicrofilmCellValue Clone() =>
      new()
      {
        Kind = Kind,
        Text = Text,
        Number = Number,
        Checkbox = Checkbox
      };

    public bool Equals(MicrofilmCellValue other)
    {
      if(other is null) return false;
      if(ReferenceEquals(this, other)) return true;

      return Kind == other.Kind
        && Text == other.Text
        && Number.Equals(other.Number)
        && Checkbox == other.Checkbox;
    }

    public override bool Equals(object obj) => Equals(obj as MicrofilmCellValue);
    public override int GetHashCode() => System.HashCode.Combine(Kind, Text, Number, Checkbox);
  }

  public class MicrofilmCellValueJsonConverter : JsonConverter<MicrofilmCellValue>
  {
    public override bool HandleNull => true;

    public override MicrofilmCellValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      switch(reader.TokenType)
      {
        case JsonTokenType.Null:
          return MicrofilmCellValue.Null();

        case JsonTokenType.String:
          return MicrofilmCellValue.FromText(reader.GetString());

        case JsonTokenType.Number:
          return reader.TryGetDouble(out var value) && double.IsFinite(value)
            ? MicrofilmCellValue.FromNumber(value)
            : MicrofilmCellValue.Unsupported();

        case JsonTokenType.True:
          return MicrofilmCellValue.FromCheckbox(true);

        case JsonTokenType.False:
          return MicrofilmCellValue.FromCheckbox(false);

        default:
          using(JsonDocument.ParseValue(ref reader))
          {
            return MicrofilmCellValue.Unsupported();
          }
      }
    }

    public override void Write(Utf8JsonWriter writer, MicrofilmCellValue value, JsonSerializerOptions options)
    {
      if(value == null || value.Kind == MicrofilmCellValueKind.Null || value.Kind == MicrofilmCellValueKind.Unsupported)
      {
        writer.WriteNullValue();
        return;
      }

      if(value.Kind == MicrofilmCellValueKind.Text)
      {
        writer.WriteStringValue(value.Text);
      }
      else if(value.Kind == MicrofilmCellValueKind.Number)
      {
        writer.WriteNumberValue(value.Number);
      }
      else if(value.Kind == MicrofilmCellValueKind.Checkbox)
      {
        writer.WriteBooleanValue(value.Checkbox);
      }
      else
      {
        writer.WriteNullValue();
      }
    }
  }

  public class MicrofilmTableRow : IEquatable<MicrofilmTableRow>
  {
    public string Id { get; set; }
    public string Origin { get; set; }
    public Dictionary<string, MicrofilmCellValue> Cells { get; set; } = new();

    public MicrofilmTableRow()
    {
    }

    public MicrofilmTableRow(string id, string origin, Dictionary<string, MicrofilmCellValue> cells)
    {
      Id = id;
      Origin = origin;
      Cells = cells ?? new Dictionary<string, MicrofilmCellValue>();
    }

    public MicrofilmTableRow Clone(string origin = null) =>
      new(
        Id,
        origin ?? Origin,
        Cells.ToDictionary(cell => cell.Key, cell => cell.Value?.Clone() ?? MicrofilmCellValue.Null()));

    public bool Equals(MicrofilmTableRow other)
    {
      if(other is null) return false;
      if(ReferenceEquals(this, other)) return true;

      return Id == other.Id
        && Origin == other.Origin
        && Cells.Count == other.Cells.Count
        && Cells.All(cell => other.Cells.TryGetValue(cell.Key, out var value) && Equals(cell.Value, value));
    }

    public override bool Equals(object obj) => Equals(obj as MicrofilmTableRow);
    public override int GetHashCode() => (Id ?? "").GetHashCode();
  }

  public class MicrofilmClientProfile : IEquatable<MicrofilmClientProfile>
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<MicrofilmTableColumn> Columns { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public MicrofilmClientProfile()
    {
    }

    public MicrofilmClientProfile(
      string id,
      string name,
      string description,
      List<MicrofilmTableColumn> columns,
      DateTimeOffset createdAt,
      DateTimeOffset updatedAt)
    {
      Id = id;
      Name = name;
      Description = description;
      Columns = columns ?? new List<MicrofilmTableColumn>();
      CreatedAt = createdAt;
      UpdatedAt = updatedAt;
    }

    public MicrofilmClientProfile Clone() =>
      new(
        Id,
        Name,
        Description,
        (Columns ?? new List<MicrofilmTableColumn>()).Select(column => column.Clone()).ToList(),
        CreatedAt,
        UpdatedAt);

    public bool Equals(MicrofilmClientProfile other)
    {
      if(other is null) return false;
      if(ReferenceEquals(this, other)) return true;

      var columns = Columns ?? new List<MicrofilmTableColumn>();
      var otherColumns = other.Columns ?? new List<MicrofilmTableColumn>();

      return Id == other.Id
        && Name == other.Name
        && Description == other.Description
        && CreatedAt == other.CreatedAt
        && UpdatedAt == other.UpdatedAt
        && columns.Count == otherColumns.Count
        && columns.SequenceEqual(otherColumns);
    }

    public override bool Equals(object obj) => Equals(obj as MicrofilmClientProfile);
    public override int GetHashCode() => (Id ?? "").GetHashCode();
  }

  public static class MicrofilmTableRules
  {
    public static bool TryNormalizeColumns(
      IEnumerable<MicrofilmTableColumn> input,
      out List<MicrofilmTableColumn> columns,
      out string code,
      out string message,
      out string columnId)
    {
      columns = new List<MicrofilmTableColumn>();
      code = null;
      message = null;
      columnId = null;

      var seen = new HashSet<string>();

      foreach(var source in input ?? Enumerable.Empty<MicrofilmTableColumn>())
      {
        if(source == null || string.IsNullOrWhiteSpace(source.Id))
        {
          code = "INVALID_COLUMN_ID";
          message = "Column IDs are required.";
          return false;
        }

        var id = source.Id.Trim();
        if(!seen.Add(id))
        {
          code = "DUPLICATE_COLUMN_ID";
          message = $"Column ID '{id}' is duplicated.";
          columnId = id;
          return false;
        }

        var type = MicrofilmTableColumnTypes.Normalize(source.Type);
        if(!MicrofilmTableColumnTypes.IsSupported(type))
        {
          code = "UNSUPPORTED_COLUMN_TYPE";
          message = $"Column '{id}' has unsupported type '{source.Type}'.";
          columnId = id;
          return false;
        }

        if(source.Width.HasValue && (!double.IsFinite(source.Width.Value) || source.Width.Value <= 0))
        {
          code = "INVALID_COLUMN_WIDTH";
          message = $"Column '{id}' has an invalid width.";
          columnId = id;
          return false;
        }

        columns.Add(new MicrofilmTableColumn(
          id,
          string.IsNullOrWhiteSpace(source.Name) ? id : source.Name.Trim(),
          type,
          source.Width,
          type == MicrofilmTableColumnTypes.Dropdown
            ? source.DropdownOptions ?? new List<string>()
            : new List<string>()));
      }

      return true;
    }

    public static bool TryNormalizeRow(
      string rowId,
      string origin,
      IEnumerable<MicrofilmTableColumn> columns,
      IDictionary<string, MicrofilmCellValue> sourceCells,
      out MicrofilmTableRow row,
      out string code,
      out string message,
      out string columnId)
    {
      row = null;
      code = null;
      message = null;
      columnId = null;

      var columnList = columns?.ToList() ?? new List<MicrofilmTableColumn>();
      var cells = CreateDefaultCells(columnList);

      foreach(var sourceCell in sourceCells ?? new Dictionary<string, MicrofilmCellValue>())
      {
        var column = columnList.FirstOrDefault(c => c.Id == sourceCell.Key);
        if(column == null)
        {
          code = "UNKNOWN_COLUMN";
          message = $"Column '{sourceCell.Key}' is not recognized.";
          columnId = sourceCell.Key;
          return false;
        }

        if(!TryNormalizeCell(column, sourceCell.Value, out var value, out message))
        {
          code = "INVALID_CELL_VALUE";
          columnId = sourceCell.Key;
          return false;
        }

        cells[sourceCell.Key] = value;
      }

      row = new MicrofilmTableRow(rowId, origin, cells);
      return true;
    }

    public static Dictionary<string, MicrofilmCellValue> CreateDefaultCells(IEnumerable<MicrofilmTableColumn> columns) =>
      (columns ?? Enumerable.Empty<MicrofilmTableColumn>())
        .ToDictionary(column => column.Id, CreateDefaultCell);

    public static MicrofilmCellValue CreateDefaultCell(MicrofilmTableColumn column) =>
      column?.Type == MicrofilmTableColumnTypes.Checkbox
        ? MicrofilmCellValue.FromCheckbox(false)
        : MicrofilmCellValue.Null();

    public static Dictionary<string, MicrofilmCellValue> ReconcileCells(
      IEnumerable<MicrofilmTableColumn> columns,
      IDictionary<string, MicrofilmCellValue> existingCells)
    {
      var reconciled = CreateDefaultCells(columns);

      foreach(var column in columns ?? Enumerable.Empty<MicrofilmTableColumn>())
      {
        if(existingCells != null
          && existingCells.TryGetValue(column.Id, out var existing)
          && TryNormalizeCell(column, existing, out var normalized, out var _))
        {
          reconciled[column.Id] = normalized;
        }
      }

      return reconciled;
    }

    public static bool TryNormalizeCell(MicrofilmTableColumn column, MicrofilmCellValue value, out MicrofilmCellValue normalized, out string message)
    {
      normalized = null;
      message = null;
      value ??= MicrofilmCellValue.Null();

      if(value.Kind == MicrofilmCellValueKind.Unsupported)
      {
        message = $"Column '{column.Id}' received an unsupported JSON value.";
        return false;
      }

      switch(column.Type)
      {
        case MicrofilmTableColumnTypes.Text:
          if(value.Kind == MicrofilmCellValueKind.Null || value.Kind == MicrofilmCellValueKind.Text)
          {
            normalized = value.Clone();
            return true;
          }
          break;

        case MicrofilmTableColumnTypes.Number:
          if(value.Kind == MicrofilmCellValueKind.Null || value.Kind == MicrofilmCellValueKind.Number)
          {
            normalized = value.Clone();
            return true;
          }
          break;

        case MicrofilmTableColumnTypes.Dropdown:
          if(value.Kind == MicrofilmCellValueKind.Null)
          {
            normalized = value.Clone();
            return true;
          }

          if(value.Kind == MicrofilmCellValueKind.Text && (column.DropdownOptions ?? new List<string>()).Contains(value.Text))
          {
            normalized = value.Clone();
            return true;
          }
          break;

        case MicrofilmTableColumnTypes.Checkbox:
          if(value.Kind == MicrofilmCellValueKind.Checkbox)
          {
            normalized = value.Clone();
            return true;
          }
          break;
      }

      message = $"Column '{column.Id}' received a value that does not match type '{column.Type}'.";
      return false;
    }

    public static MicrofilmCellValue FromSeedText(MicrofilmTableColumn column, string value)
    {
      if(value == null)
      {
        return CreateDefaultCell(column);
      }

      if(column.Type == MicrofilmTableColumnTypes.Number)
      {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number)
          ? MicrofilmCellValue.FromNumber(number)
          : MicrofilmCellValue.Unsupported();
      }

      if(column.Type == MicrofilmTableColumnTypes.Checkbox)
      {
        return bool.TryParse(value, out var checkbox)
          ? MicrofilmCellValue.FromCheckbox(checkbox)
          : MicrofilmCellValue.Unsupported();
      }

      return string.IsNullOrEmpty(value)
        ? MicrofilmCellValue.Null()
        : MicrofilmCellValue.FromText(value);
    }
  }

  public class ReplaceMicrofilmTableColumnsRequest
  {
    public List<MicrofilmTableColumn> Columns { get; set; } = new();
  }

  public class SaveMicrofilmClientProfileRequest
  {
    public string Name { get; set; }
    public string Description { get; set; }
    public List<MicrofilmTableColumn> Columns { get; set; } = new();
  }

  public class UpdateMicrofilmTableCellRequest
  {
    public string ColumnId { get; set; }
    public MicrofilmCellValue Value { get; set; }
  }

  public class CreateMicrofilmRegularRowRequest
  {
    public string RowId { get; set; }
    public Dictionary<string, MicrofilmCellValue> Cells { get; set; } = new();
  }

  public class CreateMicrofilmCustomRowRequest
  {
    public Dictionary<string, MicrofilmCellValue> Cells { get; set; } = new();
  }

  public class MicrofilmTableErrorEnvelope
  {
    public MicrofilmTableError Error { get; set; }

    public MicrofilmTableErrorEnvelope(string code, string message, Dictionary<string, string> details = null)
    {
      Error = new MicrofilmTableError(code, message, details);
    }
  }

  public class MicrofilmTableError
  {
    public string Code { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string> Details { get; set; } = new();

    public MicrofilmTableError(string code, string message, Dictionary<string, string> details = null)
    {
      Code = code;
      Message = message;
      Details = details ?? new Dictionary<string, string>();
    }
  }
}
