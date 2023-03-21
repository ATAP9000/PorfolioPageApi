using MailKit.Net.Smtp;
using MimeKit;
using PorfolioPageApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.AllowAnyOrigin().SetPreflightMaxAge(TimeSpan.FromMinutes(5)).AllowAnyHeader().AllowAnyMethod();
        });
});

var app = builder.Build();
app.MapHealthChecks("/healthz");
app.UseCors();

app.MapPost("/Email", (MailRequest request) =>
{
    if(!InternetAddress.TryParse(request.Email, out InternetAddress formatedAddress))
        return Results.BadRequest("Correo invalido");
    try
    {
        var address = Environment.GetEnvironmentVariable("MailAddress");
        var appPass = Environment.GetEnvironmentVariable("AppPass");
        var mailProvider = Environment.GetEnvironmentVariable("MailProvider");
        var mailPort = Environment.GetEnvironmentVariable("MailPort");
        int.TryParse(mailPort, out int port);
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(request.Email));
        email.To.Add(MailboxAddress.Parse(address));
        email.Subject = "Has sido contactado!";
        string body = $"Has recibido un mensaje por:<br/>Email: {request.Email}<br/>Name: {request.Name}<br/>Message: {request.Message}";
        email.Body = new TextPart("html") { Text = body };
        using var smtp = new SmtpClient();
        smtp.Connect(mailProvider, port, MailKit.Security.SecureSocketOptions.Auto);
        smtp.Authenticate(address, appPass);
        smtp.Send(email);
        smtp.Disconnect(true);
        return Results.Ok("Correo enviado exitosamente");
    }
    catch (Exception)
    {
        return Results.Problem(detail: "El servidor ha fallado" ,statusCode: 500);
    }

});

app.Run();