using System;
using System.IO;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    public sealed class CombatPrototypeJsonMapConfigSource : ICombatMapConfigSource
    {
        private readonly TextAsset definition;
        private readonly TextAsset biomes;
        private readonly TextAsset grounds;
        private readonly TextAsset objects;

        public CombatPrototypeJsonMapConfigSource(TextAsset definition, TextAsset biomes, TextAsset grounds, TextAsset objects)
        {
            this.definition = definition;
            this.biomes = biomes;
            this.grounds = grounds;
            this.objects = objects;
        }

        public CombatMapConfigSet LoadValidated(string mapDefinitionId)
        {
            var config = new CombatMapConfigSet
            {
                map = CombatPrototypeMapJsonReader.Read<MapDefinitionConfig>(definition, "selected map definition"),
                biomes = CombatPrototypeMapJsonReader.Read<BiomeDefinitionConfig[]>(biomes, "BiomesJson"),
                grounds = CombatPrototypeMapJsonReader.Read<GroundDefinitionConfig[]>(grounds, "GroundsJson"),
                objects = CombatPrototypeMapJsonReader.Read<MapObjectDefinitionConfig[]>(objects, "ObjectsJson")
            };
            if (!string.Equals(config.map.mapDefinitionId, mapDefinitionId, StringComparison.Ordinal))
                throw new InvalidDataException("Map JSON ID mismatch; resource=" +
                    CombatPrototypeMapJsonReader.ResourcePath(definition) + "; path=$.mapDefinitionId; expected=" + mapDefinitionId);
            try
            {
                CombatPrototypeMapConfigValidator.Validate(config);
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is OverflowException)
            {
                throw new InvalidDataException("Map JSON semantic validation failed; map=" +
                    CombatPrototypeMapJsonReader.ResourcePath(definition) + "; biomes=" +
                    CombatPrototypeMapJsonReader.ResourcePath(biomes) + "; grounds=" +
                    CombatPrototypeMapJsonReader.ResourcePath(grounds) + "; objects=" +
                    CombatPrototypeMapJsonReader.ResourcePath(objects) + "; " + exception.Message, exception);
            }
            return config;
        }
    }
}
