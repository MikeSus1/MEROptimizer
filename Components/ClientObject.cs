using System;
using LabApi.Features.Wrappers;
using Mirror;
using UnityEngine;

namespace MEROptimizer.Components;

public abstract class ClientObject
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public Vector3 Scale { get; set; }
    public uint NetId { get; protected set; }
    public SpawnMessage SpawnMessage { get; protected set; }
    public ObjectDestroyMessage DestroyMessage { get; protected set; }
    protected abstract uint AssetId { get; }

    protected ClientObject(Vector3 pos, Quaternion rot, Vector3 scale)
    {
        Position = pos;
        Rotation = rot;
        Scale = scale;
        NetId = NetworkIdentity.GetNextNetworkId();
    }
    
    protected void GenerateNetworkMessages()
    {
        using NetworkWriterPooled writer = NetworkWriterPool.Get();
        
        writer.WriteByte(1);
        int sizePos = writer.Position;
        writer.WriteByte(0);
        int start = writer.Position;
        
        WritePayload(writer);
        
        int end = writer.Position;
        writer.Position = sizePos;
        writer.WriteByte((byte)(end - start));
        writer.Position = end;

        byte[] payloadData = writer.ToArray();

        SpawnMessage = new SpawnMessage
        {
            netId = NetId,
            isLocalPlayer = false,
            isOwner = false,
            sceneId = 0,
            assetId = AssetId,
            position = Position,
            rotation = Rotation,
            scale = Scale,
            payload = new ArraySegment<byte>(payloadData)
        };

        DestroyMessage = new ObjectDestroyMessage { netId = NetId };
    }
    
    protected abstract void WritePayload(NetworkWriter writer);
    
    public void SpawnForPlayer(Player target)
    {
        if (target == null || target.IsHost) 
            return;
        
        target.Connection?.Send(SpawnMessage);
    }

    public void DestroyForPlayer(Player target)
    {
        if (target == null || target.IsHost) 
            return;
        
        target.Connection?.Send(DestroyMessage);
    }

    public void SpawnForEveryone()
    {
        foreach (Player player in Player.List)
        {
            if (player == null || player.IsHost || player.IsNpc || player.IsDummy) 
                continue;
            
            player.Connection?.Send(SpawnMessage);
        }
    }

    public void DestroyForEveryone()
    {
        foreach (Player player in Player.List)
        {
            if (player == null || player.IsHost || player.IsNpc || player.IsDummy) 
                continue;
            
            player.Connection?.Send(DestroyMessage);
        }
    }
}