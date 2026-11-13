namespace StudentsJson.Core;

/// <summary>Критерії пошуку. Порожній критерій не обмежує вибірку.</summary>
public class StudentFilter
{
    /// <summary>Частина П.І.П. (без урахування регістру).</summary>
    public string? NamePart { get; set; }
    public string? Faculty { get; set; }
    public string? Group { get; set; }
    public int? Course { get; set; }

    /// <summary>Дисципліна, з якої має бути оцінка.</summary>
    public string? Discipline { get; set; }

    /// <summary>Мінімальна оцінка з указаної дисципліни (або з будь-якої, якщо дисципліну не вказано).</summary>
    public int? MinGrade { get; set; }

    /// <summary>Мінімальний середній бал.</summary>
    public double? MinAverage { get; set; }

    /// <summary>Лише студенти із заборгованостями (оцінка менше 60).</summary>
    public bool OnlyDebtors { get; set; }
}

/// <summary>Пошук студентів за допомогою LINQ.</summary>
public static class StudentSearch
{
    public static IReadOnlyList<Student> Search(IEnumerable<Student> students, StudentFilter f)
    {
        var query = students.AsQueryable();

        if (!string.IsNullOrWhiteSpace(f.NamePart))
            query = query.Where(s => s.FullName.Contains(f.NamePart.Trim(), StringComparison.CurrentCultureIgnoreCase));

        if (!string.IsNullOrWhiteSpace(f.Faculty))
            query = query.Where(s => s.Faculty == f.Faculty);

        if (!string.IsNullOrWhiteSpace(f.Group))
            query = query.Where(s => s.Group == f.Group);

        if (f.Course is not null)
            query = query.Where(s => s.Course == f.Course);

        if (!string.IsNullOrWhiteSpace(f.Discipline))
            query = query.Where(s => s.Grades.Any(g => g.Discipline == f.Discipline
                                                       && (f.MinGrade == null || g.Grade >= f.MinGrade)));
        else if (f.MinGrade is not null)
            query = query.Where(s => s.Grades.Any(g => g.Grade >= f.MinGrade));

        if (f.MinAverage is not null)
            query = query.Where(s => s.AverageGrade >= f.MinAverage);

        if (f.OnlyDebtors)
            query = query.Where(s => s.Grades.Any(g => g.Grade < StudentValidator.PassingGrade));

        return query.OrderBy(s => s.FullName).ToList();
    }

    // Довідники для списків вибору (лише значення, що є в даних)
    public static IReadOnlyList<string> Faculties(IEnumerable<Student> s) => s.Select(x => x.Faculty).Distinct().OrderBy(x => x).ToList();
    public static IReadOnlyList<string> Groups(IEnumerable<Student> s) => s.Select(x => x.Group).Distinct().OrderBy(x => x).ToList();
    public static IReadOnlyList<int> Courses(IEnumerable<Student> s) => s.Select(x => x.Course).Distinct().OrderBy(x => x).ToList();
    public static IReadOnlyList<string> Disciplines(IEnumerable<Student> s)
        => s.SelectMany(x => x.Grades).Select(g => g.Discipline).Distinct().OrderBy(x => x).ToList();

    /// <summary>Середній бал по кожній групі (для рядка статистики).</summary>
    public static IReadOnlyList<(string Group, double Average, int Count)> GroupStatistics(IEnumerable<Student> s)
        => s.GroupBy(x => x.Group)
            .Select(g => (g.Key, Math.Round(g.SelectMany(x => x.Grades).Select(x => (double)x.Grade).DefaultIfEmpty(0).Average(), 2), g.Count()))
            .OrderBy(x => x.Key)
            .ToList();
}
