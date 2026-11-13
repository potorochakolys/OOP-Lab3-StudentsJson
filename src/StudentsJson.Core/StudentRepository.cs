using System.Text.Encodings.Web;
using System.Text.Json;

namespace StudentsJson.Core;

/// <summary>Серіалізація і десеріалізація списку студентів у форматі JSON.</summary>
public static class StudentJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Serialize(IEnumerable<Student> students)
        => JsonSerializer.Serialize(students.ToList(), Options);

    public static List<Student> Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<Student>>(json, Options) ?? new List<Student>();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Файл не є коректним JSON зі списком студентів: {ex.Message}", ex);
        }
    }
}

/// <summary>Список студентів, відкритий з JSON-файлу: додавання, редагування, видалення, збереження.</summary>
public class StudentRepository
{
    private readonly List<Student> _students = new();

    public IReadOnlyList<Student> Students => _students;

    /// <summary>Шлях до відкритого файлу (зберігається в той самий файл).</summary>
    public string? FilePath { get; private set; }

    public bool HasUnsavedChanges { get; private set; }

    public event EventHandler? Changed;

    public void Load(string path)
    {
        var loaded = StudentJsonSerializer.Deserialize(File.ReadAllText(path));
        foreach (var s in loaded.Where(s => s.Id == Guid.Empty)) s.Id = Guid.NewGuid();
        _students.Clear();
        _students.AddRange(loaded);
        FilePath = path;
        HasUnsavedChanges = false;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Зберігає у відкритий файл (або у вказаний, якщо передано шлях).</summary>
    public void Save(string? path = null)
    {
        var target = path ?? FilePath ?? throw new InvalidOperationException("Файл не вибрано");
        File.WriteAllText(target, StudentJsonSerializer.Serialize(_students));
        FilePath = target;
        HasUnsavedChanges = false;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Add(Student student)
    {
        EnsureValid(student);
        _students.Add(student);
        MarkChanged();
    }

    public void Update(Student student)
    {
        EnsureValid(student);
        var index = _students.FindIndex(s => s.Id == student.Id);
        if (index < 0) throw new KeyNotFoundException("Студента не знайдено");
        _students[index] = student;
        MarkChanged();
    }

    public bool Remove(Guid id)
    {
        var removed = _students.RemoveAll(s => s.Id == id) > 0;
        if (removed) MarkChanged();
        return removed;
    }

    public Student? Find(Guid id) => _students.FirstOrDefault(s => s.Id == id);

    private static void EnsureValid(Student student)
    {
        var errors = StudentValidator.Validate(student);
        if (errors.Count > 0) throw new ArgumentException(string.Join(Environment.NewLine, errors));
    }

    private void MarkChanged()
    {
        HasUnsavedChanges = true;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
