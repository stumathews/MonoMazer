namespace MazerPlatformer
{
    public class Component
    {
        private object _value;

        /// <summary>
        /// Every component possibly has a unique identifier
        /// This is used to update specific components
        /// </summary>
        public string Id { get; }

        public enum ComponentType
        {
            Health, // overall health
            Position, // current position
            State, // state
            Name, // name
            Direction, //direction,
            HitPoints, // damaged taken on hits
            Points, // this component tracks points
            NpcType, // type such as a pickup
        }

        public ComponentType Type { get; set; }

        public object Value
        {
            get => _value;
            set => _value = value;
        }

        public Component( ComponentType type, object value, string id = null)
        {
            Id = id;
            Type = type;
            Value = value;
        }
    }
}