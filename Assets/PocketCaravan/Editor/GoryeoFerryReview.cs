using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    public static class GoryeoFerryReview
    {
        [Serializable]class Approval{public string status,sourceSha256;public int fare;public float hours;}
        public static void PromoteApproved()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");const string review="ArtReview/Goryeo-Ferry-20261105/";
            var a=JsonUtility.FromJson<Approval>(File.ReadAllText(review+"approval.json"));string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(review+"source.json"))).Replace("-", "").ToLowerInvariant();
            var config=JsonUtility.FromJson<GoryeoFerry.Config>(File.ReadAllText(review+"source.json"));if(a.status!="approved"||a.sourceSha256!=hash||a.fare!=config.fare||a.hours!=config.hours)throw new InvalidDataException("Ferry approval mismatch");
            config.status="approved";const string target="Assets/PocketCaravan/Resources/Goryeo/Ferry/";Directory.CreateDirectory(target);File.WriteAllText(target+"Config.json",JsonUtility.ToJson(config,true)+"\n");AssetDatabase.Refresh();
        }
    }
}
