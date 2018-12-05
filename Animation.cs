using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WGP.SFDynamicObject
{
    /// <summary>
    /// An animation. contains all the key of the bones to animate.
    /// </summary>
    public class Animation : IBaseElement
    {
        #region Internal Constructors

        internal Animation()
        {
            ID = Guid.NewGuid();
            Name = null;
            Bones = new List<Couple<Bone, List<Key>>>();
            _triggers = new List<EventTrigger>();
        }

        #endregion Internal Constructors

        #region Public Properties

        /// <summary>
        /// A double array of all the keys of the animation, sorted by bones.
        /// </summary>
        public List<Couple<Bone, List<Key>>> Bones { get; set; }

        /// <summary>
        /// The total duration of the animation. Once the chronometer reach the duration, it will reset.
        /// </summary>
        public Time Duration { get; set; }

        /// <summary>
        /// Universal identifier of the animation.
        /// </summary>
        public Guid ID { get; internal set; }

        /// <summary>
        /// The name of the animation. Needed when loading an animation.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Dynamic Object containing this resource.
        /// </summary>
        public SFDynamicObject Owner { get; internal set; }

        /// <summary>
        /// Event triggers in the animation.
        /// </summary>
        public EventTrigger[] Triggers => _triggers.ToArray();

        #endregion Public Properties

        #region Internal Properties

        internal List<EventTrigger> _triggers { get; set; }

        #endregion Internal Properties

        #region Public Methods

        /// <summary>
        /// Creates a new event for this animation.
        /// </summary>
        /// <returns>New event.</returns>
        public EventTrigger CreateEvent()
        {
            EventTrigger result = new EventTrigger();
            result.Owner = Owner;
            result.Animation = this;
            _triggers.Add(result);
            return result;
        }

        /// <summary>
        /// Removes an event.
        /// </summary>
        /// <param name="ev">Event to remove.</param>
        /// <returns>True if successful.</returns>
        public bool RemoveEvent(EventTrigger ev)
        {
            if (ev.Animation == this)
            {
                ev.Owner = null;
                ev.Animation = null;
                _triggers.Remove(ev);
                return true;
            }
            else
                return false;
        }

        #endregion Public Methods

        #region Public Classes

        /// <summary>
        /// A key. a key is a transformation at the right moment in the timeline. The dynamic object
        /// will make interpolations between the keys.
        /// </summary>
        public class Key : IComparable<Key>, IComparable
        {
            #region Public Constructors

            /// <summary>
            /// Constructor.
            /// </summary>
            public Key()
            {
                OutlineColor = Color.White;
                OutlineThickness = 0;
                Color = Color.White;
                Transform = new Transformable();
                PosFunction = Fct.LINEAR;
                PosFctCoeff = 1;
                OriginFunction = Fct.LINEAR;
                OriginFctCoeff = 1;
                ScaleFunction = Fct.LINEAR;
                ScaleFctCoeff = 1;
                RotFunction = Fct.LINEAR;
                RotFctCoeff = 1;
                OpacityFunction = Fct.LINEAR;
                OpacityFctCoeff = 1;
                ColorFunction = Fct.LINEAR;
                ColorFctCoeff = 1;
                OutlineColorFunction = Fct.LINEAR;
                OutlineColorFctCoeff = 1;
                OutlineThicknessFunction = Fct.LINEAR;
                OutlineThicknessFctCoeff = 1;
                Opacity = 255;
            }

            #endregion Public Constructors

            #region Public Enums

            /// <summary>
            /// Type of the interpolation function
            /// </summary>
            public enum Fct
            {
                LINEAR,
                POWER,
                ROOT,
                GAUSS,
                BINARY
            }

            #endregion Public Enums

            #region Public Properties

            /// <summary>
            /// The color of the sprite.
            /// </summary>
            public Color Color { get; set; }

            /// <summary>
            /// Coefficient of the color function.
            /// </summary>
            public float ColorFctCoeff { get; set; }

            /// <summary>
            /// How the color will be calculated.
            /// </summary>
            public Fct ColorFunction { get; set; }

            /// <summary>
            /// The opacity of the bone. Doesn't inherit from its parent.
            /// </summary>
            public byte Opacity { get; set; }

            /// <summary>
            /// Coefficient of the opacity function.
            /// </summary>
            public float OpacityFctCoeff { get; set; }

            /// <summary>
            /// How the opacity will be calculated.
            /// </summary>
            public Fct OpacityFunction { get; set; }

            /// <summary>
            /// Coefficient of the origin function.
            /// </summary>
            public float OriginFctCoeff { get; set; }

            /// <summary>
            /// How the origin will be calculated.
            /// </summary>
            public Fct OriginFunction { get; set; }

            /// <summary>
            /// The outline color of the sprite.
            /// </summary>
            public Color OutlineColor { get; set; }

            /// <summary>
            /// Coefficient of the outline color function.
            /// </summary>
            public float OutlineColorFctCoeff { get; set; }

            /// <summary>
            /// How the outline color will be calculated.
            /// </summary>
            public Fct OutlineColorFunction { get; set; }

            /// <summary>
            /// The outline thickness of the sprite.
            /// </summary>
            public float OutlineThickness { get; set; }

            /// <summary>
            /// Coefficient of the outline thickness function.
            /// </summary>
            public float OutlineThicknessFctCoeff { get; set; }

            /// <summary>
            /// How the outline thickness will be calculated.
            /// </summary>
            public Fct OutlineThicknessFunction { get; set; }

            /// <summary>
            /// Coefficient of the position function.
            /// </summary>
            public float PosFctCoeff { get; set; }

            /// <summary>
            /// How the position will be calculated.
            /// </summary>
            public Fct PosFunction { get; set; }

            /// <summary>
            /// The position in time of the key in the timeline.
            /// </summary>
            public Time Position { get; set; }

            /// <summary>
            /// Coefficient of the rotation function.
            /// </summary>
            public float RotFctCoeff { get; set; }

            /// <summary>
            /// How the rotation will be calculated.
            /// </summary>
            public Fct RotFunction { get; set; }

            /// <summary>
            /// Coefficient of the scale function.
            /// </summary>
            public float ScaleFctCoeff { get; set; }

            /// <summary>
            /// How the scale will be calculated.
            /// </summary>
            public Fct ScaleFunction { get; set; }

            /// <summary>
            /// The transformations to add (or multiply in the case of scaling) to the bone.
            /// </summary>
            public Transformable Transform { get; set; }

            #endregion Public Properties

            #region Public Methods

            public int CompareTo(Key other)
            {
                return Position.AsMicroseconds().CompareTo(other.Position.AsMicroseconds());
            }

            public int CompareTo(object obj)
            {
                if (obj is Key)
                    return CompareTo((Key)obj);
                throw new InvalidOperationException("Invalid type :" + obj.GetType());
            }

            #endregion Public Methods
        }

        #endregion Public Classes
    }
}