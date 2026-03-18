namespace SmartShift.Blazor.Services;

public interface IRunAsService
{
    bool IsEnabled { get; set; }
    string RunAsLogin { get; set; }
    string GetEffectiveLogin(string realLogin);
    event Action? OnRunAsChanged;
    void TriggerChange();
}

public class RunAsService : IRunAsService
{
    public bool IsEnabled { get; set; } = false;
    public string RunAsLogin { get; set; } = string.Empty;
    public event Action? OnRunAsChanged;

    public string GetEffectiveLogin(string realLogin)
    {
        if (IsEnabled && !string.IsNullOrWhiteSpace(RunAsLogin))
            return RunAsLogin.Trim();
        return realLogin;
    }

    public void TriggerChange() => OnRunAsChanged?.Invoke();
}
