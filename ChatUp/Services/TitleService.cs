public class TitleService
{
    public string Title { get; private set; } = "Default Title";
    public event Action<string>? OnTitleChanged;

    public void SetTitle(string newTitle)
    {
        Title = newTitle;
        OnTitleChanged?.Invoke(newTitle);
    }
}
