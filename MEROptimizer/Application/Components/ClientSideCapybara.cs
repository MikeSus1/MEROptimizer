using System;
using LabApi.Features.Wrappers;
using Mirror;
using UnityEngine;

namespace MEROptimizer.Application.Components;

public class ClientSideCapybara
{
    public Vector3 position { get; set; }
    public Quaternion rotation { get; set; }
    public Vector3 scale { get; set; }
    public byte MovementSmoothing { get; set; }
    public bool IsStatic { get; set; }
    public bool CollisionsEnabled { get; set; }
    public uint ParentNetId { get; set; }
    
    public SpawnMessage spawnMessage { get; set; }
    public ObjectDestroyMessage destroyMessage { get; set; }
    public uint netId { get; set; }

    public ClientSideCapybara(Vector3 position, Quaternion rotation, Vector3 scale, bool collisionsEnabled, uint parentNetId)
    {
        this.position = position;
        this.rotation = rotation;
        this.scale = scale;
        CollisionsEnabled = collisionsEnabled;
        ParentNetId = parentNetId;
        
        MovementSmoothing = 0;
        IsStatic = true;
        
        netId = NetworkIdentity.GetNextNetworkId();

        GenerateNetworkMessages();
    }

    private void GenerateNetworkMessages()
    {
        NetworkWriterPooled writer = NetworkWriterPool.Get();

        writer.WriteByte(1);

        int sizePos = writer.Position;
        writer.WriteByte(0);
        int start = writer.Position;
        
        writer.WriteVector3(position);
        writer.WriteQuaternion(rotation);
        writer.WriteVector3(scale);
        writer.WriteByte(MovementSmoothing);
        writer.WriteBool(IsStatic);
        writer.WriteBool(CollisionsEnabled);
        writer.WriteUInt(ParentNetId);
        
        int end = writer.Position;
        writer.Position = sizePos;
        writer.WriteByte((byte)(end - start));
        writer.Position = end;

        byte[] payloadCopy = writer.ToArray();
        
        spawnMessage = new SpawnMessage
        {
            netId = netId,
            isLocalPlayer = false,
            isOwner = false,
            sceneId = 0,
            assetId = MEROptimizer.CapybaraAssetId,
            position = position,
            rotation = rotation,
            scale = scale,
            payload = new ArraySegment<byte>(payloadCopy)
        };

        destroyMessage = new ObjectDestroyMessage
        {
            netId = netId
        };
    }

    public void DestroyClienCapybara(Player target)
    {
        if (target == null || target.IsHost) return; // DO NOT SEND THIS TO THE DEDICATED OTHERWISE EVERYTHING WILL BROKE TRUST ME I LOST 3 MONTHS OF MY LIFE BECAUSE OF THIS

        target.Connection?.Send(destroyMessage);
    }

    public void SpawnClienCapybara(Player target)
    {
        if (target == null || target.IsHost) return; // DO NOT SEND THIS TO THE DEDICATED OTHERWISE EVERYTHING WILL BROKE TRUST ME I LOST 3 MONTHS OF MY LIFE BECAUSE OF THIS

        target.Connection?.Send(spawnMessage);
    }
        
    public void SpawnForEveryone()
    {
        foreach (Player player in Player.List)
        {
            if (player == null || player.IsHost || player.IsNpc || player.IsDummy) continue;
        
            player.Connection?.Send(spawnMessage);
        }
    }

    public void DestroyForEveryone()
    {
        foreach (Player player in Player.List)
        {
            if (player == null || player.IsHost || player.IsNpc || player.IsDummy) continue;
        
            player.Connection?.Send(destroyMessage);
        }
    }
}