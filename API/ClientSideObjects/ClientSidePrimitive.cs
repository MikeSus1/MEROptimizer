using AdminToys;
using MEROptimizer.Helper;
using Mirror;
using UnityEngine;

namespace MEROptimizer.Components.ClientSideObjects;

public class ClientSidePrimitive : ClientObject
{
    public PrimitiveType PrimitiveType { get; set; }
    public Color Color { get; set; }
    public PrimitiveFlags PrimitiveFlags { get; set; }

    protected override uint AssetId => PrefabHelper.PrimitiveAssetId;

    public ClientSidePrimitive(Vector3 pos, Quaternion rot, Vector3 scale, PrimitiveType type, Color color, PrimitiveFlags flags) 
        : base(pos, rot, scale)
    {
        PrimitiveType = type;
        Color = color;
        PrimitiveFlags = flags;

        GenerateNetworkMessages();
    }

    protected override void WritePayload(NetworkWriter writer)
    {
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteVector3(Scale);
        writer.WriteByte(0);     // MovementSmoothing
        writer.WriteBool(false); // IsStatic
        writer.WriteInt((int)PrimitiveType);
        writer.WriteColor(Color);
        writer.WriteByte((byte)PrimitiveFlags);
        writer.WriteUInt(0);     // ParentNetId (ou autre selon ton besoin)
    }
}