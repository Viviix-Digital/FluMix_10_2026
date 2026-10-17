using System.IO;
using UnityEngine;

namespace FluMix.Data
{
    public class LocalJsonPlayerRepository : MonoBehaviour, IPlayerRepository
    {
        PlayerRecord[] cache;

        public bool TryLogin(string username, string password, out PlayerRecord player)
        {
            player = null;
            if (string.IsNullOrEmpty(username) || password == null)
                return false;

            string name = username.Trim();
            PlayerRecord[] players = Load();
            for (int i = 0; i < players.Length; i++)
            {
                PlayerRecord candidate = players[i];
                if (candidate == null || candidate.username != name)
                    continue;
                if (candidate.password != password)
                    return false;

                player = candidate;
                return true;
            }

            return false;
        }

        public void UpsertLocal(PlayerRecord player)
        {
            if (player == null || string.IsNullOrEmpty(player.username))
                return;

            PlayerRecord[] current = Load();
            bool replaced = false;
            for (int i = 0; i < current.Length; i++)
            {
                if (current[i] != null && current[i].username == player.username)
                {
                    current[i] = player;
                    replaced = true;
                    break;
                }
            }

            if (!replaced)
            {
                var grown = new PlayerRecord[current.Length + 1];
                for (int i = 0; i < current.Length; i++)
                    grown[i] = current[i];
                grown[current.Length] = player;
                current = grown;
            }

            cache = current;
            Write(current);
        }

        public void Reload()
        {
            cache = null;
            Load();
        }

        PlayerRecord[] Load()
        {
            if (cache != null)
                return cache;

            FluMixPaths.EnsureCopied();
            string json = File.ReadAllText(FluMixPaths.Persistent(FluMixPaths.PlayersFile));
            PlayerFile file = JsonUtility.FromJson<PlayerFile>(json);
            cache = file != null && file.players != null ? file.players : new PlayerRecord[0];
            return cache;
        }

        static void Write(PlayerRecord[] players)
        {
            FluMixPaths.EnsureCopied();
            var file = new PlayerFile { players = players };
            File.WriteAllText(FluMixPaths.Persistent(FluMixPaths.PlayersFile), JsonUtility.ToJson(file, true));
        }
    }
}
