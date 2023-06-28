namespace WebApp.Models
{
    public enum AlertType { Danger, Info, Success, Warning }

    public class Alerts
    {
        public string Type { get; set; }
        public string Message { get; set; }
    }
}
