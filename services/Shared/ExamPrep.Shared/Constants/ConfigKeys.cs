namespace ExamPrep.Shared.Constants;

public static class ConfigKeys
{
    public static class Email
    {
        public const string FromAddress = "Email:FromAddress";
        public const string SmtpHost = "Email:SmtpHost";
        public const string SmtpPort = "Email:SmtpPort";
        public const string Username = "Email:Username";
        public const string Password = "Email:Password";
    }

    public static class Kafka
    {
        public const string BootstrapServers = "Kafka:BootstrapServers";
        public const string GroupId = "Kafka:GroupId";
    }

    public static class Jwt
    {
        public const string Issuer = "Jwt:Issuer";
        public const string Audience = "Jwt:Audience";
        public const string PrivateKeyPath = "Jwt:PrivateKeyPath";
        public const string PublicKeyPath = "Jwt:PublicKeyPath";
    }
}
