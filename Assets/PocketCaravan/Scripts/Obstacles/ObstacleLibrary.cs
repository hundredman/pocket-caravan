using System.Collections.Generic;
using UnityEngine;
namespace PocketCaravan
{
    [CreateAssetMenu(menuName="Pocket Caravan/Obstacles/Library")]
    public class ObstacleLibrary : ScriptableObject
    {
        public List<ObstacleDefinition> definitions=new List<ObstacleDefinition>();
        public ObstacleDefinition Find(string id)=>definitions.Find(d=>d!=null && d.id==id);
    }
}
