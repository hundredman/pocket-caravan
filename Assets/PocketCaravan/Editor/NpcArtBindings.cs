using System;
using System.IO;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    /// <summary>Python map export and Unity scene builder share the C1 role path source.</summary>
    public static class NpcArtBindings
    {
        [Serializable] class Role { public string target, key, art, path; }
        [Serializable] class Document { public int schemaVersion; public Role[] roles; }
        public static string Get(string target)
        {
            var doc = JsonUtility.FromJson<Document>(File.ReadAllText("Tools/ArtGen/npc_roles.json"));
            if (doc == null || doc.schemaVersion != 1) throw new InvalidDataException("NPC binding version");
            var role = Array.Find(doc.roles, r => r.target == target);
            if (role == null || !role.path.StartsWith("NPC/", StringComparison.Ordinal))
                throw new InvalidDataException("NPC 역할 연결 누락: " + target);
            return role.path;
        }

        /// <summary>10/27 통합: 등록이 없으면 null (다른 지역 장터 상인용).</summary>
        public static string TryGet(string target)
        {
            try { return Get(target); } catch (InvalidDataException) { return null; }
        }
    }
}
