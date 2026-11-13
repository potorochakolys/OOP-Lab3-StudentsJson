using System.Globalization;
using StudentsJson.Core;

namespace StudentsJson.App;

/// <summary>Рядок таблиці (представлення студента для відображення).</summary>
public record StudentRow(Student Source)
{
    public string FullName => Source.FullName;
    public string Faculty => Source.Faculty;
    public string Department => Source.Department;
    public int Course => Source.Course;
    public string Group => Source.Group;
    public string AverageGrade => Source.AverageGrade.ToString("0.##", CultureInfo.InvariantCulture);
    public int Debts => Source.Grades.Count(g => g.Grade < StudentValidator.PassingGrade);
    public string GradesText => string.Join("; ", Source.Grades.Select(g => $"{g.Discipline}: {g.Grade}"));
}

public partial class MainPage : ContentPage
{
    private const string Any = "(усі)";

    private readonly StudentRepository _repository = new();
    private StudentFilter _filter = new();

    public MainPage()
    {
        InitializeComponent();
        _repository.Changed += (_, _) => Refresh();
        Refresh();
    }

    // ---------- Файли ----------

    private async void OnOpenClicked(object? sender, EventArgs e)
    {
        if (!await ConfirmDiscardAsync()) return;
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Виберіть JSON-файл",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.WinUI] = new[] { ".json" }
            })
        });
        if (result is not null) await RunSafeAsync(() => Open(result.FullPath));
    }

    private async void OnSampleClicked(object? sender, EventArgs e)
    {
        if (!await ConfirmDiscardAsync()) return;
        // Копія демо-файлу в Документи, щоб зміни зберігалися в той самий (незмінний) файл
        var target = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Lab3Json", "students.json");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        if (!File.Exists(target))
            File.Copy(Path.Combine(AppContext.BaseDirectory, "SampleData", "students.json"), target);
        await RunSafeAsync(() => Open(target));
    }

    private void Open(string path)
    {
        _repository.Load(path);
        _filter = new StudentFilter();
        FillPickers();
        StatusLabel.Text = $"Завантажено студентів: {_repository.Students.Count}";
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
        => await RunSafeAsync(async () =>
        {
            if (_repository.FilePath is null) { await SaveAsAsync(); return; }
            _repository.Save();
            StatusLabel.Text = $"Збережено у файл {_repository.FilePath}";
        });

    private async void OnSaveAsClicked(object? sender, EventArgs e) => await RunSafeAsync(SaveAsAsync);

    private async Task SaveAsAsync()
    {
        var name = await DisplayPromptAsync("Зберегти як", "Назва файлу (буде збережено в Документи\\Lab3Json):",
            "Зберегти", "Скасувати", initialValue: "students_copy");
        if (string.IsNullOrWhiteSpace(name)) return;
        if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) name += ".json";
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Lab3Json");
        Directory.CreateDirectory(folder);
        _repository.Save(Path.Combine(folder, name));
        StatusLabel.Text = $"Збережено у файл {_repository.FilePath}";
    }

    // ---------- Редагування (через окрему форму) ----------

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        var page = new StudentEditPage(new Student(), isNew: true);
        page.Saved += student => RunSafe(() => _repository.Add(student));
        await Navigation.PushAsync(page);
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (StudentsView.SelectedItem is not StudentRow row)
        {
            await DisplayAlert("Редагування", "Спочатку виберіть студента в таблиці.", "OK");
            return;
        }
        var page = new StudentEditPage(row.Source.Clone(), isNew: false);
        page.Saved += student => RunSafe(() => _repository.Update(student));
        await Navigation.PushAsync(page);
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (StudentsView.SelectedItem is not StudentRow row)
        {
            await DisplayAlert("Видалення", "Спочатку виберіть студента в таблиці.", "OK");
            return;
        }
        if (await DisplayAlert("Видалення", $"Видалити студента «{row.FullName}»?", "Так", "Ні"))
            _repository.Remove(row.Source.Id);
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is StudentRow row)
            StatusLabel.Text = $"Вибрано: {row.FullName}, середній бал {row.AverageGrade}";
    }

    // ---------- Пошук ----------

    private void FillPickers()
    {
        var s = _repository.Students;
        Fill(FacultyPicker, StudentSearch.Faculties(s));
        Fill(GroupPicker, StudentSearch.Groups(s));
        Fill(CoursePicker, StudentSearch.Courses(s).Select(c => c.ToString()).ToList());
        Fill(DisciplinePicker, StudentSearch.Disciplines(s));

        static void Fill(Picker picker, IReadOnlyList<string> values)
        {
            picker.Items.Clear();
            picker.Items.Add(Any);
            foreach (var v in values) picker.Items.Add(v);
            picker.SelectedIndex = 0;
        }
    }

    private static string? Selected(Picker p) => p.SelectedIndex > 0 ? (string)p.SelectedItem : null;

    private async void OnSearchClicked(object? sender, EventArgs e)
    {
        try
        {
            _filter = new StudentFilter
            {
                NamePart = NameEntry.Text,
                Faculty = Selected(FacultyPicker),
                Group = Selected(GroupPicker),
                Course = Selected(CoursePicker) is { } c ? int.Parse(c) : null,
                Discipline = Selected(DisciplinePicker),
                MinGrade = ParseInt(MinGradeEntry.Text, "Оцінка від"),
                MinAverage = ParseDouble(MinAverageEntry.Text, "Сер. бал від"),
                OnlyDebtors = DebtorsCheck.IsChecked
            };
            Refresh();
        }
        catch (FormatException ex)
        {
            await DisplayAlert("Пошук", ex.Message, "OK");
        }
    }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        NameEntry.Text = MinGradeEntry.Text = MinAverageEntry.Text = string.Empty;
        DebtorsCheck.IsChecked = false;
        foreach (var p in new[] { FacultyPicker, GroupPicker, CoursePicker, DisciplinePicker })
            if (p.Items.Count > 0) p.SelectedIndex = 0;
        _filter = new StudentFilter();
        Refresh();
    }

    private static int? ParseInt(string? text, string field)
        => string.IsNullOrWhiteSpace(text) ? null
           : int.TryParse(text, out var v) ? v
           : throw new FormatException($"Поле «{field}» має містити ціле число.");

    private static double? ParseDouble(string? text, string field)
        => string.IsNullOrWhiteSpace(text) ? null
           : double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v
           : throw new FormatException($"Поле «{field}» має містити число.");

    private void Refresh()
    {
        var rows = StudentSearch.Search(_repository.Students, _filter).Select(s => new StudentRow(s)).ToList();
        StudentsView.ItemsSource = rows;
        FileLabel.Text = _repository.FilePath is null
            ? "Файл не відкрито"
            : $"Файл: {_repository.FilePath}{(_repository.HasUnsavedChanges ? "  (є незбережені зміни)" : string.Empty)}";
        if (_repository.FilePath is not null)
            StatusLabel.Text = $"Показано {rows.Count} з {_repository.Students.Count} студентів";
    }

    // ---------- Про програму і вихід ----------

    private async void OnAboutClicked(object? sender, EventArgs e) => await Navigation.PushAsync(new AboutPage());

    private async void OnExitClicked(object? sender, EventArgs e)
    {
        if (await ConfirmExitAsync()) Application.Current?.Quit();
    }

    public Task<bool> ConfirmExitAsync()
        => DisplayAlert("Вихід",
            _repository.HasUnsavedChanges
                ? "Є незбережені зміни. Чи дійсно ви хочете завершити роботу з програмою?"
                : "Чи дійсно ви хочете завершити роботу з програмою?",
            "Так", "Ні");

    private async Task<bool> ConfirmDiscardAsync()
        => !_repository.HasUnsavedChanges
           || await DisplayAlert("Незбережені зміни", "Поточні зміни не збережено. Продовжити без збереження?", "Так", "Ні");

    private async void RunSafe(Action action)
    {
        try { action(); }
        catch (Exception ex) { await DisplayAlert("Помилка", ex.Message, "OK"); }
    }

    private async Task RunSafeAsync(Action action)
    {
        try { action(); }
        catch (Exception ex) { await DisplayAlert("Помилка", ex.Message, "OK"); }
    }

    private async Task RunSafeAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { await DisplayAlert("Помилка", ex.Message, "OK"); }
    }
}
