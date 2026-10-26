namespace Infrastructure.Configurations
{
    /// <summary>
    /// Configuration class for Firebase database settings.
    /// Contains the connection URL required to initialize the Firebase repository.
    /// </summary>
    public class FirebaseSettings
    {
        /// <summary>
        /// Gets or sets the Firebase Realtime Database URL.
        /// </summary>
        public string DatabaseUrl { get; set; }
    }
}