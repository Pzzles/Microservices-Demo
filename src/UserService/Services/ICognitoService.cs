namespace UserService.Services
{
    public interface ICognitoService
    {
        Task<string> RegisterAsync(string email, string password, string name);
        Task<string> AuthenticateAsync(string email, string password);
        Task<string> ConfirmAsync(string email, string code);
    }
}
