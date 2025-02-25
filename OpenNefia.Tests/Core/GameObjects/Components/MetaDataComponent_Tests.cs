﻿using System;
using System.IO;
using NUnit.Framework;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.Serialization.Manager;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace OpenNefia.Tests.Core.GameObjects
{
    [TestFixture]
    [TestOf(typeof(MetaDataComponent))]
    public class MetaDataComponent_Tests : OpenNefiaUnitTest
    {
        private const string Prototypes = @"
- type: Entity
  name: dummy
  id: dummy
  components:
  - type: MetaData
";

        [OneTimeSetUp]
        public void Setup()
        {
            var componentFactory = IoCManager.Resolve<IComponentFactory>();
            componentFactory.FinishRegistration();

            IoCManager.Resolve<ISerializationManager>().Initialize();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.RegisterType<EntityPrototype>();
            prototypeManager.LoadFromStream(new StringReader(Prototypes));
            prototypeManager.Resync();
        }

        [Test]
        public void MetaDataComponentIsAliveTest()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            var dummy = entityManager.CreateEntityUninitialized(new("dummy"));

            Assert.That(dummy, Is.Not.Null);
            Assert.That(entityManager.IsAlive(dummy), Is.True);
            Assert.That(entityManager.IsDeadAndBuried(dummy), Is.False);

            var metaDataComp = entityManager.GetComponent<MetaDataComponent>(dummy);

            Assert.That(metaDataComp.Liveness, Is.EqualTo(EntityGameLiveness.Alive));
            Assert.That(metaDataComp.IsAlive, Is.EqualTo(true));
            Assert.That(metaDataComp.IsDeadAndBuried, Is.EqualTo(false));

            metaDataComp.Liveness = EntityGameLiveness.Hidden;

            Assert.That(metaDataComp.IsAlive, Is.EqualTo(false));
            Assert.That(metaDataComp.IsDeadAndBuried, Is.EqualTo(false));

            metaDataComp.Liveness = EntityGameLiveness.DeadAndBuried;

            Assert.That(metaDataComp.IsAlive, Is.EqualTo(false));
            Assert.That(metaDataComp.IsDeadAndBuried, Is.EqualTo(true));
        }
    }
}