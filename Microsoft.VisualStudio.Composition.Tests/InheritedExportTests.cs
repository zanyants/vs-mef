﻿namespace Microsoft.VisualStudio.Composition.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;
    using MefV1 = System.ComponentModel.Composition;

    public class InheritedExportTests
    {
        #region Abstract base class tests 

        [MefFact(CompositionEngines.V1Compat, typeof(AbstractBaseClass), typeof(DerivedOfAbstractClass))]
        public void InheritedExportDoesNotApplyToAbstractBaseClasses(IContainer container)
        {
            var derived = container.GetExportedValue<AbstractBaseClass>();
            Assert.IsType<DerivedOfAbstractClass>(derived);
        }

        [MefFact(CompositionEngines.V1Compat, typeof(AbstractBaseClass))]
        public void MetadataOnAbstractClassExportIsInaccessible(IContainer container)
        {
            Assert.Throws<CompositionFailedException>(() => container.GetExport<AbstractBaseClass, IDictionary<string, object>>());
        }

        [MefV1.InheritedExport]
        [MefV1.ExportMetadata("a", 1)]
        public abstract class AbstractBaseClass { }

        public class DerivedOfAbstractClass : AbstractBaseClass { }

        #endregion

        #region Concrete base class tests 

        [MefFact(CompositionEngines.V1Compat, typeof(BaseClass), typeof(DerivedClass))]
        public void InheritedExportAppliesToConcreteBaseClasses(IContainer container)
        {
            var exports = container.GetExportedValues<BaseClass>();
            Assert.Equal(2, exports.Count());
            Assert.Equal(1, exports.OfType<DerivedClass>().Count());
        }

        [MefV1.InheritedExport]
        public class BaseClass { }

        public class DerivedClass : BaseClass { }

        #endregion

        #region ExportAttribute does not inherit

        [MefFact(CompositionEngines.V1Compat, typeof(BaseClassWithExport), typeof(DerivedTypeOfExportedClass))]
        public void ExportDoesNotInherit(IContainer container)
        {
            Assert.IsType<BaseClassWithExport>(container.GetExportedValue<BaseClassWithExport>());
        }

        [MefV1.Export]
        public class BaseClassWithExport { }

        public class DerivedTypeOfExportedClass : BaseClassWithExport { }

        #endregion
    }
}
