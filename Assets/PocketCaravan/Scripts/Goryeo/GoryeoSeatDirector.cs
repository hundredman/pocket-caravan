using System.Collections.Generic;
using UnityEngine;
namespace PocketCaravan
{
    /// <summary>Install approved seats after the destination player exists. No scene/save/other-region edits.</summary>
    public sealed class GoryeoSeatDirector:MonoBehaviour
    {
        static GoryeoSeat.Config config;static GoryeoSeat.Art[] art;static Sprite[] fire;
        MapRoot applied;PlayerController appliedPlayer;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){config=null;art=null;fire=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(FindFirstObjectByType<GoryeoSeatDirector>()!=null)return;var go=new GameObject("Goryeo approved seats");DontDestroyOnLoad(go);go.AddComponent<GoryeoSeatDirector>();}
        void Update()
        {
            var map=MapRoot.Current;var pc=PlayerController.Instance;
            if(map==null||(map==applied&&pc==appliedPlayer)||map.previewOnly||map.mapId!="transit_central_approach"||pc==null||pc.gameObject.scene!=map.gameObject.scene||GameManager.I==null||GameManager.I.IsTravelling)return;
            if(map.GetComponentsInChildren<GoryeoSeat>(true).Length>0){var field=map.GetComponent<GoryeoFireFootprint>();if(field!=null)field.AllowExistingActor(pc.Motor);applied=map;appliedPlayer=pc;return;}
            if(config==null){var text=Resources.Load<TextAsset>("Goryeo/Seats/Config");if(text==null)return;config=JsonUtility.FromJson<GoryeoSeat.Config>(text.text);}
            if(config==null||config.status!="approved"||config.poses==null)return;
            if(art==null)
            {
                var list=new List<GoryeoSeat.Art>();foreach(var p in config.poses){var sprite=Resources.Load<Sprite>(p.file);if(sprite==null)return;list.Add(new GoryeoSeat.Art{originId=p.originId,sprite=sprite});}art=list.ToArray();
            }
            if(fire==null){fire=new Sprite[4];for(int i=0;i<4;i++)fire[i]=Resources.Load<Sprite>("Goryeo/Seats/Fire/"+i);}
            if(GoryeoSeat.InstallApproved(config,art,fire)==3){applied=map;appliedPlayer=pc;}
        }
    }
}
