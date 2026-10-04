using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Code_01.CombatPrototype.Map
{
    public static class CombatPrototypeMapResourceLayoutSignature
    {
        public static string Compute(CombatMapConfigSet config, CombatPrototypeMapLayout layout)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, new UTF8Encoding(false), true))
            {
                writer.Write("combat-map-resources-v1");
                writer.Write(config.map.mapDefinitionId);
                writer.Write(layout.Data.Seed);
                writer.Write(layout.Data.Origin.x); writer.Write(layout.Data.Origin.y);
                writer.Write(layout.Data.Size.x); writer.Write(layout.Data.Size.y);
                writer.Write(layout.Data.CellSize); writer.Write(layout.Data.CellsPerChunk);
                writer.Write(layout.Data.BaseHeight);
                writer.Write(layout.Data.PlayerRadius); writer.Write(layout.Data.EnemyRadius);
                writer.Write(layout.Data.CollisionSkin);
                for (var index = 0; index < layout.Decorations.Count; index++)
                {
                    var placement = layout.Decorations[index];
                    var definition = config.objects[placement.ObjectIndex];
                    var kind = definition.gatherable ? 1 :
                        config.map.treeHarvest.enabled && definition.objectId == config.map.treeHarvest.treeObjectId ? 2 :
                        config.map.mining.enabled && definition.objectId == config.map.mining.mineObjectId ? 3 : 0;
                    if (kind == 0) continue;
                    writer.Write(kind); writer.Write(index); writer.Write(definition.objectId);
                    writer.Write(placement.Position.x); writer.Write(placement.Position.y); writer.Write(placement.Position.z);
                    writer.Write(placement.YawRadians);
                    writer.Write(definition.footprintRadiusMeters); writer.Write(definition.blocksMovement);
                    writer.Write(definition.regrowEnabled); writer.Write(definition.regrowSeconds);
                }
            }
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(stream.ToArray());
            var result = new StringBuilder(hash.Length * 2);
            foreach (var value in hash) result.Append(value.ToString("x2"));
            return result.ToString();
        }
    }
}
