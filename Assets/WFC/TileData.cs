using System;
using UnityEngine;

namespace Balma.WFC
{
    public struct TileData
    {
        public ConnectionData cd0;
        public ConnectionData cd1;
        public ConnectionData cd2;
        public ConnectionData cd3;
        public ConnectionData cd4;
        public ConnectionData cd5;
        public ConnectionData cd6;
        public ConnectionData cd7;

        public Quaternion prefabRotation;
        public GameObject prefab;

        //TODO access memory directly avoiding branching (like float3)
        public ConnectionData this[int index]
        {
            get => index switch
            {
                0 => cd0,
                1 => cd1,
                2 => cd2,
                3 => cd3,
                4 => cd4,
                5 => cd5,
                6 => cd6,
                7 => cd7,
                _ => throw new IndexOutOfRangeException()
            };
            set
            {
                switch (index)
                {
                    case 0: cd0 = value; break;
                    case 1: cd1 = value; break;
                    case 2: cd2 = value; break;
                    case 3: cd3 = value; break;
                    case 4: cd4 = value; break;
                    case 5: cd5 = value; break;
                    case 6: cd6 = value; break;
                    case 7: cd7 = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }
        
        public struct ConnectionData
        {
            public TerrainType key;

            public bool Equals(ConnectionData other)
            {
                return key == other.key;
            }

            public override bool Equals(object obj)
            {
                return obj is ConnectionData other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (int) key;
            }

            public static bool operator ==(ConnectionData lhs, ConnectionData rhs)
            {
                return lhs.key == rhs.key;
            }

            public static bool operator !=(ConnectionData lhs, ConnectionData rhs)
            {
                return !(lhs == rhs);
            }
        }
    }
}