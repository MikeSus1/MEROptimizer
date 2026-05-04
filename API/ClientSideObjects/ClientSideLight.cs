using MEROptimizer.Helper;
using Mirror;
using UnityEngine;

namespace MEROptimizer.Components.ClientSideObjects;

public class ClientSideLight : ClientObject
{
    public float Intensity { get; set; }
    public float Range { get; set; }
    public Color Color { get; set; }
    public int Shadows { get; set; }
    public float ShadowStrength { get; set; }
    public int Type { get; set; }
    public LightShape Shape { get; set; }
    public float SpotAngle { get; set; }
    public float InnerSpotAngle { get; set; }
    public uint ParentNetId { get; set; }

    protected override uint AssetId => PrefabHelper.LightAssetId;

    public ClientSideLight(Vector3 pos, Quaternion rot, Vector3 scale, float intensity, float range, Color color, LightShadows shadows, float shadowStrength, LightShape shape, LightType type, float spotAngle, float innerSpotAngle, uint parentNetId) : base(pos, rot, scale)
    {
        Intensity = intensity;
        Range = range;
        Color = color;
        Shadows = (int)shadows;
        ShadowStrength = shadowStrength;
        Type = (int)type;
        Shape = shape;
        SpotAngle = spotAngle;
        InnerSpotAngle = innerSpotAngle;
        ParentNetId = parentNetId;

        GenerateNetworkMessages();
    }

    protected override void WritePayload(NetworkWriter writer)
    {
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteVector3(Scale);
        writer.WriteByte(0);    // MovementSmoothing
        writer.WriteBool(true); // IsStatic
        writer.WriteFloat(Intensity);
        writer.WriteFloat(Range);
        writer.WriteColor(Color);
        writer.WriteInt(Shadows);
        writer.WriteFloat(ShadowStrength);
        writer.WriteInt(Type);
        writer.WriteInt((int)Shape);
        writer.WriteFloat(SpotAngle);
        writer.WriteFloat(InnerSpotAngle);
        writer.WriteUInt(ParentNetId);
    }
}