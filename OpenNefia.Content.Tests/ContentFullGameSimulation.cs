﻿using OpenNefia.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.Tests
{
    /// <summary>
    /// Game simulation that will load all content classes/resources with reflection.
    /// </summary>
    public class ContentFullGameSimulation
    {
        public static IFullSimulationFactory NewSimulation()
        {
            return FullGameSimulation
                .NewSimulation()
                .RegisterContentAssemblies(assemblies =>
                {
                    assemblies.Add(typeof(OpenNefia.Content.EntryPoint).Assembly);
                    assemblies.Add(typeof(ContentFullGameSimulation).Assembly);
                })
                .RegisterDependencies(deps => ContentUnitTest.RegisterIoC());
        }
    }
}
