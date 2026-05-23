namespace DestinoPeruBlazor.Services;

public enum ToastLevel { Success, Error, Warning, Info }

public record ToastMessage(Guid Id, string Text, ToastLevel Level, DateTime CreatedAt);

public class ToastService
{
    private readonly List<ToastMessage> _messages = [];
    public IReadOnlyList<ToastMessage> Messages => _messages;
    public event Action? OnChange;

    public void Show(string text, ToastLevel level = ToastLevel.Info)
    {
        _messages.Add(new ToastMessage(Guid.NewGuid(), text, level, DateTime.UtcNow));
        Notify();
        _ = AutoRemoveAsync(_messages[^1].Id);
    }

    public void ShowSuccess(string text) => Show(text, ToastLevel.Success);
    public void ShowError(string text) => Show(text, ToastLevel.Error);
    public void ShowWarning(string text) => Show(text, ToastLevel.Warning);

    public void Remove(Guid id)
    {
        _messages.RemoveAll(m => m.Id == id);
        Notify();
    }

    private async Task AutoRemoveAsync(Guid id)
    {
        await Task.Delay(5000);
        Remove(id);
    }

    private void Notify() => OnChange?.Invoke();
}
