﻿using OpenNefia.Core.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.CharaMake
{
    public class CharaMakeData
    {
        public Dictionary<Type, Dictionary<string, object>> CharaData { get; set; }
        public CharaMakeStep LastStep { get; set; }
        public IReadOnlyList<ICharaMakeLayer> AllSteps { get; } = new List<ICharaMakeLayer>();

        public CharaMakeData(IEnumerable<ICharaMakeLayer> allSteps)
        {
            CharaData = new Dictionary<Type, Dictionary<string, object>>();
            AllSteps = allSteps.ToList();
        }

        public bool TryGetCharaMakeResults(string key, out IEnumerable<object> vals)
        {
            vals = CharaData.Values.Where(x => x.TryGetValue(key, out _)).Select(x => x[key]);
            return vals.Any();
        }

        /// <summary>
        /// Returns the first object with the correct type in the data
        /// </summary>
        public bool TryGetCharaMakeResult<T>(string key, [NotNullWhen(true)] out T? val)
        {
            val = default!;
            if (TryGetCharaMakeResults(key, out var vals))
            {
                foreach (var obj in vals)
                {
                    if (obj is T tObj)
                    {
                        val = tObj;
                        return true;
                    }
                }
            }

            Logger.ErrorS("charamake", $"No charamake result with name '{key}' and type {typeof(T)} in {nameof(CharaMakeData)}!");
            return false;
        }
    }
}
