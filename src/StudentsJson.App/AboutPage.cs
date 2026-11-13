namespace StudentsJson.App;

/// <summary>Форма «Про програму».</summary>
public class AboutPage : ContentPage
{
    public AboutPage()
    {
        Title = "Про програму";
        BackgroundColor = Colors.White;

        // П.І.П. автора зчитується з файлу author.txt поруч із програмою
        var authorFile = Path.Combine(AppContext.BaseDirectory, "author.txt");
        var author = File.Exists(authorFile) ? File.ReadAllText(authorFile).Trim() : "(вкажіть П.І.П. у файлі author.txt)";

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 10,
            Children =
            {
                new Label { Text = "Успішність студентів (JSON)", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.Black },
                Line("Лабораторна робота №3, варіант 6"),
                Line($"Виконав: {author}"),
                Line("Курс: 2, група: К-25"),
                Line("Рік: 2026"),
                new Label
                {
                    TextColor = Color.FromArgb("#333333"),
                    Text = "Програма відкриває JSON-файл зі списком студентів і їхніми оцінками, показує його у вигляді таблиці, " +
                           "дозволяє додавати, редагувати (в окремій формі) і видаляти записи, зберігати зміни в той самий файл " +
                           "або в новий. Пошук виконується за допомогою LINQ: за частиною П.І.П., факультетом, групою, курсом, " +
                           "оцінкою з дисципліни, середнім балом і наявністю заборгованостей."
                },
                new Button { Text = "Закрити", BackgroundColor = Color.FromArgb("#1F6FB2"), TextColor = Colors.White, HorizontalOptions = LayoutOptions.Start }
                    .OnClick(async b => await b.Navigation.PopAsync())
            }
        };
    }

    private static Label Line(string text) => new() { Text = text, FontSize = 15, TextColor = Colors.Black };
}

internal static class ButtonExtensions
{
    public static Button OnClick(this Button button, Action<Button> handler)
    {
        button.Clicked += (_, _) => handler(button);
        return button;
    }
}
