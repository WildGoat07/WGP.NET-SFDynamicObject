using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WGP.SFDynamicObject
{
    public class Couple<T, U> : IEquatable<Couple<T, U>> where T : IEquatable<T>
    {
        #region Public Constructors

        public Couple()
        {
        }

        public Couple(T key, U value)
        {
            Key = key;
            Value = value;
        }

        #endregion Public Constructors

        #region Public Properties

        public T Key { get; set; }
        public U Value { get; set; }

        #endregion Public Properties

        #region Public Methods

        public bool Equals(Couple<T, U> other)
        {
            return Key.Equals(other.Key);
        }

        #endregion Public Methods
    }
}