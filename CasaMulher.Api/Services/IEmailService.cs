namespace CasaMulher.Api.Services;

public interface IEmailService
{
    Task EnviarAsync(string destinatario, string assunto, string corpoHtml, string tipo);
}
