namespace UsersAPI.Application.Interface;

public interface IPasswordService
{
    string EncryptPassword(string password);
    bool IsPasswordMatch(string password, string hash);
}
