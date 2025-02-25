﻿using OpenNefia.Content.Skills;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Serialization.Manager;
using OpenNefia.Core.Serialization.Manager.Attributes;
using OpenNefia.Core.Serialization.Manager.Result;
using OpenNefia.Core.Serialization.Markdown;
using OpenNefia.Core.Serialization.Markdown.Validation;
using OpenNefia.Core.Serialization.Markdown.Value;
using OpenNefia.Core.Serialization.TypeSerializers.Interfaces;

namespace OpenNefia.Content.Spells
{
    [TypeSerializer]
    public class LevelPotentialAndStockSerializer : ITypeSerializer<LevelPotentialAndStock, ValueDataNode>
    {
        public DeserializationResult Read(
            ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies, bool skipHook,
            ISerializationContext? context = null)
        {
            var result = new LevelPotentialAndStock();
            result.Stats.Level = new(int.Parse(node.Value));

            return new DeserializedValue<LevelPotentialAndStock>(result);
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            return int.TryParse(node.Value, out _)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, $"Failed parsing {nameof(LevelAndPotential)}");
        }

        public DataNode Write(ISerializationManager serializationManager, LevelPotentialAndStock value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return serializationManager.GetDefinition<LevelPotentialAndStock>()!
                .Serialize(value, serializationManager, context, alwaysWrite);
        }

        public LevelPotentialAndStock Copy(ISerializationManager serializationManager, LevelPotentialAndStock source, LevelPotentialAndStock target,
            bool skipHook,
            ISerializationContext? context = null)
        {
            serializationManager.Copy(source.Stats, target.Stats, context, skipHook);
            target.SpellStock = source.SpellStock;
            return target;
        }

        public bool Compare(ISerializationManager serializationManager, LevelPotentialAndStock left, LevelPotentialAndStock right, bool skipHook,
            ISerializationContext? context = null)
        {
            return left.Equals(right);
        }
    }
}
