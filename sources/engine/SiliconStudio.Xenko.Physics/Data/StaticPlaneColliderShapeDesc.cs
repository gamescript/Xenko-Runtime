// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization.Contents;

namespace SiliconStudio.Xenko.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<StaticPlaneColliderShapeDesc>))]
    [DataContract("StaticPlaneColliderShapeDesc")]
    [Display(50, "Infinite Plane")]
    public class StaticPlaneColliderShapeDesc : IInlineColliderShapeDesc
    {
        /// <userdoc>
        /// The normal of the infinite plane.
        /// </userdoc>
        [DataMember(10)]
        public Vector3 Normal = Vector3.UnitY;

        /// <userdoc>
        /// The distance offset.
        /// </userdoc>
        [DataMember(20)]
        public float Offset;

        public bool Match(object obj)
        {
            var other = obj as StaticPlaneColliderShapeDesc;
            if (other == null) return false;
            return other.Normal == Normal && Math.Abs(other.Offset - Offset) < float.Epsilon;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Normal.GetHashCode()*397) ^ Offset.GetHashCode();
            }
        }
    }
}
