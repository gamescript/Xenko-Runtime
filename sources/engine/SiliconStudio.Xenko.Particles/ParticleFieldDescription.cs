// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using SiliconStudio.Core;

namespace SiliconStudio.Xenko.Particles
{
    public class ParticleFieldDescription
    {
        private readonly int hashCode;
        public override int GetHashCode() => hashCode;

        public int FieldSize { get; protected set; }

        private readonly string name;
        public string Name => name;

        protected ParticleFieldDescription(string name)
        {
            this.name = name;
            hashCode = name.GetHashCode();
            FieldSize = 0;
        }
    }

    public class ParticleFieldDescription<T> : ParticleFieldDescription where T : struct 
    {
        private readonly T defaultValue;
        public T DefaultValue => defaultValue;

        public ParticleFieldDescription(string name) : base(name)
        {
            FieldSize = ParticleUtilities.AlignedSize(Utilities.SizeOf<T>(), 4);
        }

        public ParticleFieldDescription(string name, T defaultValue) : this(name)
        {
            this.defaultValue = defaultValue;
            FieldSize = ParticleUtilities.AlignedSize(Utilities.SizeOf<T>(), 4);
        }
    }
}
