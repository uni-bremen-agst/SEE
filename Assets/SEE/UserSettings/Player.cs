using SEE.Utils.Config;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.User
{
    [Serializable]
    internal class Player
    {
        /// <summary>
        /// Name of the local player; used for the text chat and the avatar badge.
        /// </summary>
        [Tooltip("The name of the player."), ShowInInspector]
        public string PlayerName { get; set; } = "Me";

        /// <summary>
        /// The index of the player's avatar.
        /// </summary>
        [Tooltip("The index of the player's avatar"), ShowInInspector]
        public uint AvatarIndex { get; set; } = 0;

        /// <summary>
        /// Label of attribute <see cref="PlayerName"/> in the configuration file.
        /// </summary>
        private const string playernameLabel = "playername";
        /// <summary>
        /// Label of attribute <see cref="AvatarIndex"/> in the configuration file.
        /// </summary>
        private const string avatarIndexLabel = "avatarIndex";
        /// <summary>
        /// Saves the settings of this <see cref="Player"/> using <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">the writer to be used to save the settings</param>
        public virtual void Save(ConfigWriter writer)
        {
            writer.Save(PlayerName, playernameLabel);
            // The following cast from uint to int is necessary because otherwise the value
            // would be saved as a float.
            writer.Save((int)AvatarIndex, avatarIndexLabel);
        }

        /// <summary>
        /// Restores the settings from <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">the attributes from which to restore the settings</param>
        public virtual void Restore(Dictionary<string, object> attributes)
        {
            {
                string value = PlayerName;
                ConfigIO.Restore(attributes, playernameLabel, ref value);
                PlayerName = value;
            }
            {
                int value = (int)AvatarIndex;
                if (ConfigIO.Restore(attributes, avatarIndexLabel, ref value))
                {
                    AvatarIndex = (uint)value;
                }
            }
        }
    }
}
