using System;
using LabApi.Features.Wrappers;
using Mirror;
using ProjectMER.Commands.Modifying.Position;
using ProjectMER.Commands.Modifying.Rotation;
using ProjectMER.Commands.Modifying.Scale;
using UnityEngine;

namespace MEROptimizer.Application.Components
{
    public class ClientSideLight
    {
        public SpawnMessage spawnMessage { get; set; }
        public ObjectDestroyMessage destroyMessage { get; set; }
        
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 scale { get; set; }
        public float Intensity { get; set; }
        public float Range { get; set; }
        public byte MovementSmoothing { get; set; }
        public bool IsStatic { get; set; }
        public float ShadowStrength { get; set; }
        public int Type { get; set; }
        public int Shape { get; set; }
        public float SpotAngle { get; set; }
        public float InnerSpotAngle { get; set; }
        public uint ParentNetId { get; set; }
        public int Shadows { get; set; }
        public Color Color { get; set; }
        public uint netId { get; set; }
        
        public ClientSideLight(
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            float intensity,
            float range,
            Color color,
            LightShadows shadows,
            float shadowStrength,
            LightType type,
            LightShape shape,
            float spotAngle,
            float innerSpotAngle,
            uint parentNetId)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;

            Intensity = intensity;
            Range = range;
            Color = color;
            Shadows = (int)shadows;
            ShadowStrength = shadowStrength;
            Type = (int)type;
            Shape = (int)shape;
            SpotAngle = spotAngle;
            InnerSpotAngle = innerSpotAngle;
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
            writer.WriteFloat(Intensity);
            writer.WriteFloat(Range);
            writer.WriteColor(Color);
            writer.WriteInt(Shadows);
            writer.WriteFloat(ShadowStrength);
            writer.WriteInt(Type);
            writer.WriteInt(Shape);
            writer.WriteFloat(SpotAngle);
            writer.WriteFloat(InnerSpotAngle);
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
                assetId = MEROptimizer.LightAssetId,
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
        
        public void DestroyClientLight(Player target)
        {
            if (target == null || target.IsHost) return; // DO NOT SEND THIS TO THE DEDICATED OTHERWISE EVERYTHING WILL BROKE TRUST ME I LOST 3 MONTHS OF MY LIFE BECAUSE OF THIS

            target.Connection?.Send(destroyMessage);
        }

        public void SpawnClientLight(Player target)
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
}