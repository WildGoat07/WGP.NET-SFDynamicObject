using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WGP.SFDynamicObject
{
    /// <summary>
    /// An event that calls a function when triggered.
    /// </summary>
    public class EventTrigger : IBaseElement
    {
        #region Internal Fields

        internal bool triggered;

        #endregion Internal Fields

        #region Public Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        public EventTrigger()
        {
            ID = Guid.NewGuid();
            Name = null;
            Trigger = null;
            Area = default;
            Time = default;
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Area triggered by the event.
        /// </summary>
        public FloatRect Area { get; set; }

        /// <summary>
        /// Universal identifier of the event.
        /// </summary>
        public Guid ID { get; internal set; }

        /// <summary>
        /// Name describing the event.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Time at which is triggered the event.
        /// </summary>
        public Time Time { get; set; }

        /// <summary>
        /// Function called by the event when triggered.
        /// </summary>
        public Action Trigger { get; set; }

        #endregion Public Properties
    }
}