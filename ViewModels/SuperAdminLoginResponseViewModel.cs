namespace CentralPress.API.ViewModels;

public class SuperAdminLoginResponseViewModel
{
    public string Token { get; init; } = string.Empty;
    public UserProfileViewModel User { get; init; } = null!;
}
