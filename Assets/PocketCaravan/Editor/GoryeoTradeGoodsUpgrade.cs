using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    /// <summary>Task6 승인 아이콘·E2 초안 신규 품목만 더한다. 기존 상품/재고/ID는 보존.</summary>
    public static class GoryeoTradeGoodsUpgrade
    {
        const string Root="Assets/PocketCaravan/",Art=Root+"Art/Goryeo/Items/",Data=Root+"Data/Transit/";
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            if(!File.ReadAllText("ArtReview/ItemIcons-v3-20261104/approval.json").Contains("\"status\": \"approved\""))throw new InvalidDataException("Approval required");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var n in new[]{"grain","transit_spice","transit_wool"})
            {
                var im=(TextureImporter)AssetImporter.GetAtPath(Art+n+".png");im.textureType=TextureImporterType.Sprite;im.spriteImportMode=SpriteImportMode.Single;im.spritePixelsPerUnit=16;im.filterMode=FilterMode.Point;im.mipmapEnabled=false;im.textureCompression=TextureImporterCompression.Uncompressed;im.SaveAndReimport();
            }
            var db=AssetDatabase.LoadAssetAtPath<GameDatabase>(Root+"Resources/GameDatabase.asset");
            TownData Town(string id)=>db.towns.Find(t=>t.id==id)??throw new InvalidDataException(id);
            ItemData Good(string file,string id,string label,int price,float weight,TownData origin)
            {
                string path=Data+file+".asset";var item=AssetDatabase.LoadAssetAtPath<ItemData>(path);var registered=db.items.Find(i=>i.id==id);
                if(registered!=null&&registered!=item)throw new InvalidDataException("ID belongs to another asset: "+id);
                if(item==null){item=ScriptableObject.CreateInstance<ItemData>();AssetDatabase.CreateAsset(item,path);}
                item.id=id;item.displayName=label;item.basePrice=price;item.weight=weight;item.originTown=origin;
                item.pricePerMap=.25f; // ItemData 기본값 유지. 새 거리식/추가 조정 없음.
                item.questItem=false;item.icon=AssetDatabase.LoadAssetAtPath<Sprite>(Art+id+".png");
                if(!db.items.Contains(item))db.items.Add(item);EditorUtility.SetDirty(item);return item;
            }
            void Stock(TownData town,ItemData item,float multiplier,float? optimal=null)
            {
                var row=town.stock.Find(s=>s.item==item);
                if(row==null){row=new TownData.StockEntry{item=item};town.stock.Add(row);}
                row.optimal=optimal??town.defaultOptimal;row.localPriceMultiplier=multiplier;EditorUtility.SetDirty(town);
            }
            var south=Town("transit_southport");var cross=Town("transit_crossroads");var capital=Town("gaegyeong");
            var spice=Good("Item_Spice","transit_spice","향신료 꾸러미",22,1,south);var wool=Good("Item_Wool","transit_wool","양모 뭉치",14,2,cross);
            Stock(south,spice,.65f,40);Stock(cross,wool,.7f,48);Stock(cross,spice,1.4f);Stock(capital,spice,1.5f);Stock(capital,wool,1.3f);
            var grain=db.items.Find(i=>i.id=="grain");grain.icon=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"grain.png");EditorUtility.SetDirty(grain);
            EditorUtility.SetDirty(db);AssetDatabase.SaveAssets();
        }
    }
}
