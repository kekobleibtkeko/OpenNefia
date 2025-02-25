﻿using System.IO;
using BenchmarkDotNet.Attributes;
using OpenNefia.Benchmarks.Serialization.Definitions;
using OpenNefia.Core.Serialization.Manager.Result;
using OpenNefia.Core.Serialization.Markdown;
using OpenNefia.Core.Serialization.Markdown.Mapping;
using OpenNefia.Core.Serialization.Markdown.Sequence;
using OpenNefia.Core.Serialization.Markdown.Value;
using OpenNefia.Core.Serialization.TypeSerializers.Implementations.Custom;
using YamlDotNet.RepresentationModel;

namespace OpenNefia.Benchmarks.Serialization.Read
{
    [MemoryDiagnoser]
    public class SerializationReadBenchmark : SerializationBenchmark
    {
        public SerializationReadBenchmark()
        {
            InitializeSerialization();

            StringDataDefNode = new MappingDataNode();
            StringDataDefNode.Add(new ValueDataNode("string"), new ValueDataNode("ABC"));

            var yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(SeedDataDefinition.Prototype));

            SeedNode = yamlStream.Documents[0].RootNode.ToDataNodeCast<SequenceDataNode>().Cast<MappingDataNode>(0);
        }

        private ValueDataNode StringNode { get; } = new("ABC");

        private ValueDataNode IntNode { get; } = new("1");

        private MappingDataNode StringDataDefNode { get; }

        private MappingDataNode SeedNode { get; }

        private ValueDataNode FlagZero { get; } = new("Zero");

        private ValueDataNode FlagThirtyOne { get; } = new("ThirtyOne");

        [Benchmark]
        public string? ReadString()
        {
            return SerializationManager.ReadValue<string>(StringNode);
        }

        [Benchmark]
        public int? ReadInteger()
        {
            return SerializationManager.ReadValue<int>(IntNode);
        }

        [Benchmark]
        public DataDefinitionWithString? ReadDataDefinitionWithString()
        {
            return SerializationManager.ReadValue<DataDefinitionWithString>(StringDataDefNode);
        }

        [Benchmark]
        public SeedDataDefinition? ReadSeedDataDefinition()
        {
            return SerializationManager.ReadValue<SeedDataDefinition>(SeedNode);
        }

        [Benchmark]
        [BenchmarkCategory("flag")]
        public DeserializationResult ReadFlagZero()
        {
            return SerializationManager.ReadWithTypeSerializer(
                typeof(int),
                typeof(FlagSerializer<BenchmarkFlags>),
                FlagZero);
        }

        [Benchmark]
        [BenchmarkCategory("flag")]
        public DeserializationResult ReadThirtyOne()
        {
            return SerializationManager.ReadWithTypeSerializer(
                typeof(int),
                typeof(FlagSerializer<BenchmarkFlags>),
                FlagThirtyOne);
        }

        [Benchmark]
        [BenchmarkCategory("customTypeSerializer")]
        public DeserializationResult ReadIntegerCustomSerializer()
        {
            return SerializationManager.ReadWithTypeSerializer(
                typeof(int),
                typeof(BenchmarkIntSerializer),
                IntNode);
        }
    }
}
