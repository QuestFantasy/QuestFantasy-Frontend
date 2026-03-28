namespace QuestFantasy.Core.Base
{
    /// <summary>
    /// Base class for named game objects with descriptions.
    /// Provides common properties for items, skills, jobs, and other named entities.
    /// </summary>
    public class NameAndDescription
    {
        private string _name = "Unnamed";
        private string _description = "";

        public string Name
        {
            get => _name;
            set => _name = value ?? "Unnamed";
        }

        public string Description
        {
            get => _description;
            set => _description = value ?? "";
        }

        /// <summary>
        /// Get a formatted display string for this object
        /// </summary>
        public override string ToString()
        {
            return string.IsNullOrEmpty(Description)
                ? Name
                : $"{Name}: {Description}";
        }
    }
}