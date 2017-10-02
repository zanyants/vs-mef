﻿namespace Microsoft.VisualStudio.Composition
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Reflection.PortableExecutable;

    /// <summary>
    /// Metadata about a <see cref="Assembly"/> that is used to determine if
    /// two assemblies are equivalent.
    /// </summary>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    internal class StrongAssemblyIdentity : IEquatable<StrongAssemblyIdentity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StrongAssemblyIdentity"/> class.
        /// </summary>
        /// <param name="name">The assembly name. Cannot be null.</param>
        /// <param name="lastWriteTimeUtc">The LastWriteTimeUtc of the assembly manifest file.</param>
        /// <param name="mvid">The MVID of the ManifestModule of the assembly.</param>
        internal StrongAssemblyIdentity(AssemblyName name, DateTime lastWriteTimeUtc, Guid mvid)
        {
            Requires.NotNull(name, nameof(name));
            this.Name = name;
            this.LastWriteTimeUtc = lastWriteTimeUtc;
            this.Mvid = mvid;
        }

        /// <summary>
        /// Gets the assembly's full name.
        /// </summary>
        internal AssemblyName Name { get; }

        /// <summary>
        /// Gets the LastWriteTimeUtc for the assembly manifest file.
        /// </summary>
        internal DateTime LastWriteTimeUtc { get; }

        /// <summary>
        /// Gets the MVID for the assembly's manifest module. This is a unique identifier that represents individual
        /// builds of an assembly.
        /// </summary>
        internal Guid Mvid { get; }

        /// <summary>
        /// Gets the metadata from an assembly at the specified path.
        /// </summary>
        /// <param name="assemblyFile">The path to the assembly to read metadata from.</param>
        /// <param name="assemblyName">The assembly name, if already known.</param>
        /// <returns>The assembly metadata.</returns>
        /// <exception cref="FileNotFoundException">Thrown if <paramref name="assemblyFile"/> does not refer to an existing file.</exception>
        internal static StrongAssemblyIdentity CreateFrom(string assemblyFile, AssemblyName assemblyName = null)
        {
            Requires.NotNullOrEmpty(assemblyFile, nameof(assemblyFile));

            if (assemblyName == null)
            {
#if NET45
                assemblyName = AssemblyName.GetAssemblyName(assemblyFile);
#else
                throw new NotSupportedException($"{nameof(assemblyName)} must be specified on this platform.");
#endif
            }

            DateTime timestamp = File.GetLastWriteTimeUtc(assemblyFile);
            Guid mvid = GetMvid(assemblyFile);

            return new StrongAssemblyIdentity(assemblyName, timestamp, mvid);
        }

        /// <summary>
        /// Gets the metadata from an assembly.
        /// </summary>
        /// <param name="assembly">The assembly to read metadata from.</param>
        /// <param name="assemblyName">An optional <see cref="AssemblyName"/> that may be important for dynamic assemblies to find their CodeBase.</param>
        /// <returns>The assembly metadata.</returns>
        internal static StrongAssemblyIdentity CreateFrom(Assembly assembly, AssemblyName assemblyName)
        {
            Requires.NotNull(assembly, nameof(assembly));

            if (assemblyName == null)
            {
                assemblyName = assembly.GetName();
            }

            DateTime timestamp = assembly.IsDynamic ? DateTime.MaxValue : File.GetLastWriteTimeUtc(assembly.Location);
            return new StrongAssemblyIdentity(assemblyName, timestamp, assembly.ManifestModule.ModuleVersionId);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => this.Equals(obj as StrongAssemblyIdentity);

        /// <inheritdoc/>
        public bool Equals(StrongAssemblyIdentity other)
        {
            return other != null
                && ByValueEquality.AssemblyNameNoFastCheck.Equals(this.Name, other.Name)
                && this.LastWriteTimeUtc == other.LastWriteTimeUtc
                && this.Mvid == other.Mvid;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.Mvid.GetHashCode();
        }

        /// <summary>
        /// Gets the MVID for an assembly with the specified path.
        /// </summary>
        /// <param name="assemblyFile">The assembly to get the MVID from.</param>
        /// <returns>The MVID.</returns>
        private static Guid GetMvid(string assemblyFile)
        {
            using (var stream = File.OpenRead(assemblyFile))
            {
                using (var reader = new PEReader(stream))
                {
                    var metadataReader = reader.GetMetadataReader();
                    var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
                    return metadataReader.GetGuid(mvidHandle);
                }
            }
        }
    }
}