using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    public static class GoryeoWorkshopTrialReview
    {
        public const string Scene="Assets/PocketCaravan/Editor/B2Preview/GoryeoWorkshop20261104/Dakmok.unity";
        [MenuItem("Pocket Caravan/Tests/고려 한지 공방 검토 만들기")]
        public static void Build()
        {
            if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play/save edits first");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene("Assets/PocketCaravan/Scenes/Goryeo/Dakmok.unity");var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();map.previewOnly=true;
                foreach(var e in UnityEngine.Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
                foreach(var existing in UnityEngine.Object.FindObjectsByType<GoryeoWorkshopSpot>(FindObjectsSortMode.None))existing.enabled=false;
                var cell=new Vector3Int(37,26,0);if(!map.IsWalkable(cell)||Interactable.At(cell)!=null)throw new InvalidOperationException("Workshop entrance is occupied");
                var spot=new GameObject("한지 공방 검토 자리").AddComponent<GoryeoWorkshopTrialSpot>();spot.recipeId=GoryeoWorkshopTrialSpot.Id;spot.blocksMovement=false;spot.transform.position=map.CellCenter(cell);
                Directory.CreateDirectory(Path.GetDirectoryName(Scene));EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Scene);
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        public static void Run(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");A7dUIReview.SetSize(960,540);EditorSceneManager.OpenScene(Scene);Application.runInBackground=true;EditorApplication.isPlaying=true;}
        [Serializable] class PriceRow{public string town;public int directBuy,orderCost,destinationSell,tradeProfit,orderProfit;}
        [Serializable] class PriceReport{public string scope="Initial stock, neutral reputation, normal day, no origin/haggle bonuses; equal destination sale state. Order fee=max(18,current Dakmok paper buy+5). No future-price guarantee.";public PriceRow[] rows;}
        public static void Prices()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            var db=Resources.Load<GameDatabase>("GameDatabase");var paper=db.items.Find(i=>i.id=="paper");var source=db.towns.Find(t=>t.id=="dakmok");int buy=new MarketState(source,db).BaseBuyPrice(paper);int fee=Mathf.Max(18,buy+5);
            var rows=new System.Collections.Generic.List<PriceRow>();foreach(var id in new[]{"dakmok","gaegyeong","transit_woodtown","transit_ferry","transit_southport","transit_crossroads"})
            {var t=db.towns.Find(v=>v.id==id);int sell=new MarketState(t,db).BaseSellPrice(paper);if(sell-fee>sell-buy)throw new Exception("Commission exceeds direct trade");rows.Add(new PriceRow{town=id,directBuy=buy,orderCost=fee,destinationSell=sell,tradeProfit=sell-buy,orderProfit=sell-fee});}
            Directory.CreateDirectory("ArtReview/GoryeoWorkshop-Task3-20261104");File.WriteAllText("ArtReview/GoryeoWorkshop-Task3-20261104/prices.json",JsonUtility.ToJson(new PriceReport{rows=rows.ToArray()},true));
        }
    }
}
