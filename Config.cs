using System.Collections.Generic;

namespace MEROptimizer
{
  public class Config
  {
    public bool IsEnabled { get; set; } = true;
    
    public bool Debug { get; set; }
    
    public bool OptimizeOnlyNonCollidable { get; set; } = false;
    
    public List<string> excludeObjects { get; set; } = new List<string>();
    
    public bool ClusterizeSchematic { get; set; } = true;
    
    public List<string> excludeUnspawningDistantObjects { get; set; } = new List<string>();
    
    public float SpawnDistance { get; set; } = 50;
    
    public Dictionary<string, float> CustomSchematicSpawnDistance { get; set; } = new Dictionary<string, float>();
    
    public bool ShouldSpectatorBeAffectedByDistanceSpawning { get; set; } = false;
    
    public bool ShouldTutorialsBeAffectedByDistanceSpawning { get; set; } = true;
    
    public float MinimumSizeBeforeBeingBigPrimitive { get; set; } = 10f;
    
    public float numberOfPrimitivePerSpawn { get; set; } = .1f;
    
    public float MaxDistanceForPrimitiveCluster { get; set; } = 2.5f;
    
    public int MaxPrimitivesPerCluster { get; set; } = 100;
  }
}
