using System.IO;
using Unity.Collections;
using Unity.Entities;

namespace Code_01.CombatPrototype.Networking
{
    // Added to the server player at admission; this identity is not a Ghost field.
    public struct CombatPrototypePlayerIdentity : IComponentData
    {
        public FixedString64Bytes PlayerId;

        public static FixedString64Bytes Parse(string playerId)
        {
            if (string.IsNullOrEmpty(playerId) || playerId.Length > 32)
                throw new InvalidDataException("PlayerId must contain 1 to 32 lowercase ASCII letters, digits, '_' or '-'.");
            foreach (var character in playerId)
            {
                if ((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') ||
                    character == '_' || character == '-')
                    continue;
                throw new InvalidDataException("PlayerId must contain 1 to 32 lowercase ASCII letters, digits, '_' or '-'.");
            }
            if (playerId == "con" || playerId == "prn" || playerId == "aux" || playerId == "nul" ||
                (playerId.Length == 4 && (playerId.StartsWith("com") || playerId.StartsWith("lpt")) &&
                 playerId[3] >= '1' && playerId[3] <= '9'))
                throw new InvalidDataException("PlayerId cannot be a reserved device filename.");
            return new FixedString64Bytes(playerId);
        }
    }
}
