using MEROptimizer.Components;
using MEROptimizer.Helper;
using Mirror;
using UnityEngine;

namespace MEROptimizer.API.ClientSideObjects;

public class ClientSideCapybara : ClientObject
{
    public bool CollisionsEnabled { get; set; }
    public uint ParentNetId { get; set; }

    protected override uint AssetId => PrefabHelper.CapybaraAssetId;

    public ClientSideCapybara(Vector3 pos, Quaternion rot, Vector3 scale, bool collisions, uint parentId) : base(pos, rot, scale)
    {
        CollisionsEnabled = collisions;
        ParentNetId = parentId;
        GenerateNetworkMessages();
    }

    protected override void WritePayload(NetworkWriter writer)
    {
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteVector3(Scale);
        writer.WriteByte(0); // Smoothing
        writer.WriteBool(true); // IsStatic
        writer.WriteBool(CollisionsEnabled);
        writer.WriteUInt(ParentNetId);
    }
}