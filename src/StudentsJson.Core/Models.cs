using System.Text.Json.Serialization;

namespace StudentsJson.Core;

/// <summary>Оцінка студента з однієї дисципліни.</summary>
public class DisciplineGrade
{
    public string Discipline { get; set; } = string.Empty;
    public int Semester { get; set; } = 1;
    public int Grade { get; set; }
    public string ControlType { get; set; } = "Іспит";

    public DisciplineGrade Clone() => (DisciplineGrade)MemberwiseClone();

    public override string ToString() => $"{Discipline} ({ControlType}, сем. {Semester}): {Grade}";
}

/// <summary>Студент і його успішність (варіант 6: «Успішність студентів»).</summary>
public class Student
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Faculty { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int Course { get; set; } = 1;
    public string Group { get; set; } = string.Empty;
    public List<DisciplineGrade> Grades { get; set; } = new();

    /// <summary>Середній бал (не серіалізується, обчислюється).</summary>
    [JsonIgnore]
    public double AverageGrade => Grades.Count == 0 ? 0 : Math.Round(Grades.Average(g => g.Grade), 2);

    [JsonIgnore]
    public bool HasDebts => Grades.Any(g => g.Grade < StudentValidator.PassingGrade);

    public Student Clone()
    {
        var copy = (Student)MemberwiseClone();
        copy.Grades = Grades.Select(g => g.Clone()).ToList();
        return copy;
    }
}

/// <summary>Перевірка коректності даних перед збереженням.</summary>
public static class StudentValidator
{
    public const int PassingGrade = 60;

    public static IReadOnlyList<string> Validate(Student s)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(s.FullName)) errors.Add("Вкажіть П.І.П.");
        if (string.IsNullOrWhiteSpace(s.Faculty)) errors.Add("Вкажіть факультет");
        if (string.IsNullOrWhiteSpace(s.Group)) errors.Add("Вкажіть групу");
        if (s.Course is < 1 or > 6) errors.Add("Курс має бути від 1 до 6");
        foreach (var g in s.Grades)
        {
            if (string.IsNullOrWhiteSpace(g.Discipline)) errors.Add("У кожної оцінки має бути дисципліна");
            if (g.Grade is < 0 or > 100) errors.Add($"Оцінка з «{g.Discipline}» має бути від 0 до 100");
            if (g.Semester is < 1 or > 12) errors.Add($"Семестр для «{g.Discipline}» має бути від 1 до 12");
        }
        return errors;
    }
}
