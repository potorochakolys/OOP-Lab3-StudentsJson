using System.Collections.ObjectModel;
using StudentsJson.Core;

namespace StudentsJson.App;

/// <summary>Окрема форма для додавання і редагування студента та його оцінок.</summary>
public class StudentEditPage : ContentPage
{
    private readonly Student _student;
    private readonly ObservableCollection<DisciplineGrade> _grades;

    private readonly Entry _name = NewEntry("Прізвище Ім'я По батькові");
    private readonly Entry _faculty = NewEntry("Факультет");
    private readonly Entry _department = NewEntry("Кафедра");
    private readonly Entry _course = NewEntry("Курс (1-6)", Keyboard.Numeric);
    private readonly Entry _group = NewEntry("Група");

    private readonly Entry _discipline = NewEntry("Дисципліна");
    private readonly Entry _semester = NewEntry("Семестр", Keyboard.Numeric);
    private readonly Entry _grade = NewEntry("Оцінка (0-100)", Keyboard.Numeric);
    private readonly Picker _controlType = new() { ItemsSource = new[] { "Іспит", "Залік" }, SelectedIndex = 0, TextColor = Colors.Black };
    private readonly Label _errors = new() { TextColor = Colors.Firebrick };

    public event Action<Student>? Saved;

    public StudentEditPage(Student student, bool isNew)
    {
        _student = student;
        _grades = new ObservableCollection<DisciplineGrade>(student.Grades);
        Title = isNew ? "Новий студент" : $"Редагування: {student.FullName}";
        BackgroundColor = Colors.White;

        _name.Text = student.FullName;
        _faculty.Text = student.Faculty;
        _department.Text = student.Department;
        _course.Text = student.Course.ToString();
        _group.Text = student.Group;

        var gradesView = new CollectionView
        {
            ItemsSource = _grades,
            HeightRequest = 220,
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label { TextColor = Colors.Black, VerticalOptions = LayoutOptions.Center };
                label.SetBinding(Label.TextProperty, ".");
                var remove = new Microsoft.Maui.Controls.Button { Text = "✕", BackgroundColor = Color.FromArgb("#C0392B"), TextColor = Colors.White, Padding = new Thickness(8, 0) };
                remove.SetBinding(Microsoft.Maui.Controls.Button.CommandParameterProperty, ".");
                remove.Clicked += (s, _) => _grades.Remove((DisciplineGrade)((Microsoft.Maui.Controls.Button)s!).CommandParameter);
                return new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                    Padding = new Thickness(0, 2),
                    Children = { label, remove.Column(1) }
                };
            })
        };

        var addGrade = MakeButton("Додати оцінку", "#188038", OnAddGrade);
        var save = MakeButton("Зберегти", "#1F6FB2", OnSave);
        var cancel = MakeButton("Скасувати", "#7F8C8D", async () => await Navigation.PopAsync());

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 8,
                MaximumWidthRequest = 720,
                HorizontalOptions = LayoutOptions.Start,
                Children =
                {
                    Header("Дані студента"),
                    Row("П.І.П.", _name), Row("Факультет", _faculty), Row("Кафедра", _department),
                    Row("Курс", _course), Row("Група", _group),
                    Header("Оцінки"),
                    gradesView,
                    new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition(new GridLength(3, GridUnitType.Star)), new ColumnDefinition(GridLength.Star),
                            new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)
                        },
                        ColumnSpacing = 6,
                        Children = { _discipline, _semester.Column(1), _grade.Column(2), _controlType.Column(3), addGrade.Column(4) }
                    },
                    _errors,
                    new HorizontalStackLayout { Spacing = 8, Children = { save, cancel } }
                }
            }
        };
    }

    private void OnAddGrade()
    {
        if (string.IsNullOrWhiteSpace(_discipline.Text)
            || !int.TryParse(_semester.Text, out var semester)
            || !int.TryParse(_grade.Text, out var grade))
        {
            _errors.Text = "Для оцінки вкажіть дисципліну, семестр і оцінку (цілі числа).";
            return;
        }
        _grades.Add(new DisciplineGrade
        {
            Discipline = _discipline.Text.Trim(),
            Semester = semester,
            Grade = grade,
            ControlType = (string)_controlType.SelectedItem
        });
        _discipline.Text = _semester.Text = _grade.Text = string.Empty;
        _errors.Text = string.Empty;
    }

    private async void OnSave()
    {
        _student.FullName = _name.Text?.Trim() ?? string.Empty;
        _student.Faculty = _faculty.Text?.Trim() ?? string.Empty;
        _student.Department = _department.Text?.Trim() ?? string.Empty;
        _student.Group = _group.Text?.Trim() ?? string.Empty;
        _student.Course = int.TryParse(_course.Text, out var c) ? c : 0;
        _student.Grades = _grades.ToList();

        var errors = StudentValidator.Validate(_student);
        if (errors.Count > 0)
        {
            _errors.Text = string.Join(Environment.NewLine, errors);
            return;
        }
        Saved?.Invoke(_student);
        await Navigation.PopAsync();
    }

    private static Entry NewEntry(string placeholder, Keyboard? keyboard = null)
        => new() { Placeholder = placeholder, TextColor = Colors.Black, Keyboard = keyboard ?? Keyboard.Default };

    private static Label Header(string text)
        => new() { Text = text, FontSize = 17, FontAttributes = FontAttributes.Bold, TextColor = Colors.Black, Margin = new Thickness(0, 8, 0, 0) };

    private static Grid Row(string caption, View input) => new()
    {
        ColumnDefinitions = { new ColumnDefinition(new GridLength(110)), new ColumnDefinition(GridLength.Star) },
        Children = { new Label { Text = caption, VerticalOptions = LayoutOptions.Center, TextColor = Colors.Black }, input.Column(1) }
    };

    private static Microsoft.Maui.Controls.Button MakeButton(string text, string color, Action onClick)
    {
        var b = new Button { Text = text, BackgroundColor = Color.FromArgb(color), TextColor = Colors.White };
        b.Clicked += (_, _) => onClick();
        return b;
    }
}

internal static class ViewExtensions
{
    public static T Column<T>(this T view, int column) where T : View
    {
        Grid.SetColumn(view, column);
        return view;
    }
}
