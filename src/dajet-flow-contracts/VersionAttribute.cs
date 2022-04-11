﻿namespace DaJet.Flow.Contracts
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class VersionAttribute : Attribute
    {
        internal VersionAttribute(int version)
        {
            Version = version;
        }
        internal int Version { get; }
    }
}