namespace DataMonitor2.Models;

public class EmailSettings
{
    public required string SmtpServer { get; set; }
    public int Port { get; set; }
    public bool EnableSsl { get; set; }
    public required string SenderName { get; set; }
    public required string SenderEmail { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }

    public override string ToString() => 
        $"SmtpServer: {SmtpServer}" +
        $"Port: {Port}, " +
        $"EnableSsl: {EnableSsl}, " +
        $"SenderName: {SenderName}, " +
        $"SenderEmail: {SenderEmail}" +
        $"Password: {Password}";

}
