using StudentsJson.Core;
using Xunit;

namespace StudentsJson.Core.Tests;

public class StudentsJsonTests
{
    private static readonly string SamplePath = Path.Combine(AppContext.BaseDirectory, "data", "students.json");

    private static StudentRepository LoadSample()
    {
        var repo = new StudentRepository();
        repo.Load(SamplePath);
        return repo;
    }

    private static string TempCopy()
    {
        var path = Path.Combine(Path.GetTempPath(), $"students-{Guid.NewGuid():N}.json");
        File.Copy(SamplePath, path);
        return path;
    }

    [Fact]
    public void Load_ReadsAllStudentsWithGrades()
    {
        var repo = LoadSample();
        Assert.Equal(12, repo.Students.Count);
        Assert.All(repo.Students, s => Assert.NotEmpty(s.Grades));
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip()
    {
        var repo = LoadSample();
        var json = StudentJsonSerializer.Serialize(repo.Students);
        var back = StudentJsonSerializer.Deserialize(json);

        Assert.Equal(repo.Students.Count, back.Count);
        Assert.Equal(repo.Students[0].FullName, back[0].FullName);
        Assert.Equal(repo.Students[0].Grades.Count, back[0].Grades.Count);
        Assert.DoesNotContain("averageGrade", json); // обчислювані властивості не серіалізуються
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsInvalidData()
        => Assert.Throws<InvalidDataException>(() => StudentJsonSerializer.Deserialize("{ not json"));

    [Fact]
    public void AddUpdateRemove_AndSaveToSameFile()
    {
        var path = TempCopy();
        var repo = new StudentRepository();
        repo.Load(path);

        var student = new Student { FullName = "Тестовий Студент", Faculty = "Тестовий", Group = "Т-1", Course = 1 };
        student.Grades.Add(new DisciplineGrade { Discipline = "ООП", Grade = 95, Semester = 1 });
        repo.Add(student);

        var edited = student.Clone();
        edited.Group = "Т-2";
        repo.Update(edited);
        Assert.True(repo.HasUnsavedChanges);

        repo.Save();
        var reloaded = new StudentRepository();
        reloaded.Load(path);
        Assert.Equal(13, reloaded.Students.Count);
        Assert.Equal("Т-2", reloaded.Find(student.Id)!.Group);

        Assert.True(reloaded.Remove(student.Id));
        Assert.Equal(12, reloaded.Students.Count);
    }

    [Fact]
    public void Add_InvalidStudent_Throws()
    {
        var repo = new StudentRepository();
        var bad = new Student { FullName = "", Course = 9 };
        bad.Grades.Add(new DisciplineGrade { Discipline = "X", Grade = 150 });
        var ex = Assert.Throws<ArgumentException>(() => repo.Add(bad));
        Assert.Contains("П.І.П.", ex.Message);
        Assert.Contains("від 0 до 100", ex.Message);
    }

    [Fact]
    public void Search_ByFacultyAndCourse()
    {
        var result = StudentSearch.Search(LoadSample().Students,
            new StudentFilter { Faculty = "Механіко-математичний", Course = 1 });
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Search_ByDisciplineWithMinGrade()
    {
        var result = StudentSearch.Search(LoadSample().Students,
            new StudentFilter { Discipline = "Об'єктно-орієнтоване програмування", MinGrade = 85 });
        Assert.Equal(new[] { "Бондар Марія Олегівна", "Коваленко Олена Петрівна", "Савченко Катерина Юріївна" },
            result.Select(s => s.FullName));
    }

    [Fact]
    public void Search_ByNamePartAndAverage()
    {
        var students = LoadSample().Students;
        Assert.Single(StudentSearch.Search(students, new StudentFilter { NamePart = "мельник" }));
        var excellent = StudentSearch.Search(students, new StudentFilter { MinAverage = 90 });
        Assert.All(excellent, s => Assert.True(s.AverageGrade >= 90));
        Assert.Contains(excellent, s => s.FullName == "Руденко Богдан Олександрович");
    }

    [Fact]
    public void Search_OnlyDebtors()
    {
        var result = StudentSearch.Search(LoadSample().Students, new StudentFilter { OnlyDebtors = true });
        Assert.Equal(new[] { "Гончаренко Тарас Васильович", "Поліщук Максим Романович" }, result.Select(s => s.FullName));
    }

    [Fact]
    public void Lookups_ContainOnlyExistingValues()
    {
        var students = LoadSample().Students;
        Assert.Equal(new[] { 1, 2, 3, 4 }, StudentSearch.Courses(students));
        Assert.Contains("К-25", StudentSearch.Groups(students));
        Assert.Contains("Бази даних", StudentSearch.Disciplines(students));
    }
}
