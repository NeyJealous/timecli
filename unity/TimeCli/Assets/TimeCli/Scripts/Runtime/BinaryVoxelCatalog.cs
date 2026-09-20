using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TimeClickers.PortCore;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    public static class BinaryVoxelCatalog
    {
        private const string ResourceName = "TimeCliVoxelCatalog";
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("TCLIVOX1");

        public static bool TryLoadFromResources(out IVoxelModelCatalog catalog)
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourceName);

            if (asset == null)
            {
                catalog = null;
                return false;
            }

            catalog = Load(asset.bytes);
            return true;
        }

        public static IVoxelModelCatalog Load(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            using var stream = new MemoryStream(bytes, writable: false);
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            byte[] magic = reader.ReadBytes(Magic.Length);
            if (magic.Length != Magic.Length)
                throw new InvalidDataException("Voxel catalog is truncated.");

            for (int i = 0; i < Magic.Length; i++)
            {
                if (magic[i] != Magic[i])
                    throw new InvalidDataException("Invalid TimeCli voxel catalog magic.");
            }

            int modelCount = reader.ReadInt32();
            if (modelCount < 0 || modelCount > 10000)
                throw new InvalidDataException($"Invalid model count: {modelCount}");

            var descriptors = new List<VoxelModelDescriptor>(modelCount);
            var layouts = new List<VoxelModelLayout>(modelCount);

            for (int i = 0; i < modelCount; i++)
            {
                string id = ReadString(reader);
                int minWave = reader.ReadInt32();
                int bossWaveRaw = reader.ReadInt32();

                var size = new VoxelPoint(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle());

                VoxelPoint[] red = ReadPoints(reader);
                VoxelPoint[] white = ReadPoints(reader);
                VoxelPoint[] yellow = ReadPoints(reader);
                VoxelPoint[] blue = ReadPoints(reader);

                var layout = new VoxelModelLayout(
                    id,
                    red,
                    white,
                    yellow,
                    blue,
                    size);

                layouts.Add(layout);
                descriptors.Add(new VoxelModelDescriptor(
                    id,
                    minWave,
                    layout.EnemyCount,
                    bossWaveRaw >= 0 ? bossWaveRaw : null));
            }

            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException(
                    $"Voxel catalog has {stream.Length - stream.Position} trailing bytes.");
            }

            return new InMemoryVoxelModelCatalog(descriptors, layouts);
        }

        private static string ReadString(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length < 0 || length > 1024 * 1024)
                throw new InvalidDataException($"Invalid string length: {length}");

            byte[] data = reader.ReadBytes(length);
            if (data.Length != length)
                throw new EndOfStreamException();

            return Encoding.UTF8.GetString(data);
        }

        private static VoxelPoint[] ReadPoints(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > 1_000_000)
                throw new InvalidDataException($"Invalid point count: {count}");

            var points = new VoxelPoint[count];

            for (int i = 0; i < count; i++)
            {
                points[i] = new VoxelPoint(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle());
            }

            return points;
        }
    }
}
