namespace FistAidQueue
{
    public class Patient
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public EmergencyLevel EmergencyLevel { get; set; }

        public Patient(string name, int age, EmergencyLevel emergencyLevel)
        {
            Name = name;
            Age = age;
            EmergencyLevel = emergencyLevel;
        }
    }
}