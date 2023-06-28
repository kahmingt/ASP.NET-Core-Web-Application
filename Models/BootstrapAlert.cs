namespace WebApp.Models
{
    public enum BootstrapAlertType { Danger, Info, Success, Warning }

    public class BootstrapAlert
    {
        public string Type { get; set; }
        public string Message { get; set; }
    }
}
