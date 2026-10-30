namespace Core
{
    public record ClientConfiguration
    {
        public string HostName { get; init; } = "localhost";
        public int Port { get; init; } = 11000;
        public string UserName { get; init; } = "";

        public string Password { get; init; } = "";
    }
}