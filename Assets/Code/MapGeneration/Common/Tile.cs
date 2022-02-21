using System;
using UnityEditor;
using UnityEngine;

namespace Balma.WFC
{
    public class Tile : MonoBehaviour
    {
        public enum ConnectionDirection
        {
            Symmetrical, Forward, Backwards
        }
        
        public enum ConnectionRotation
        {
            Indistinct = 0,
            R0 = 1,
            R1 = 2,
            R2 = 3,
            R3 = 4
        }
        
        [Serializable]
        public struct ConnectionData
        {
            public ConnectionType type;
            public ConnectionDirection direction;
            public ConnectionRotation rotation;
        }
        
        public struct ConnectionDataProxy
        {
            public int type;
            public ConnectionDirection direction;
            public ConnectionRotation rotation;
        }
        
        private const int CONNECTIONS_COUNT = 6;

        public bool generateRotations = true;
        public float weight = 1f;
        public ConnectionData[] connections = new ConnectionData[CONNECTIONS_COUNT];

        private void OnDrawGizmos()
        {
            var v = 0.45f;
            Handles.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one);
            Draw(new Vector3(v,0,0),   connections[0]);
            Draw(new Vector3(0,0,v),  connections[1]);
            Draw(new Vector3(-v,0,0), connections[2]);
            Draw(new Vector3(0,0,-v),  connections[3]);
            Draw(new Vector3(0,v,0),   connections[4]);
            Draw(new Vector3(0,-v,0),  connections[5]);
            Handles.matrix = Matrix4x4.identity;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one*.99f);
        }

        private void Draw(Vector3 v, ConnectionData connection)
        {
            if (connection.type is { })
            {
                var str = connection.type.label;
                str += connection.direction == ConnectionDirection.Symmetrical ? " (s)" :
                    connection.direction == ConnectionDirection.Forward ? " (!)" : " (?)";

                if (connection.rotation != ConnectionRotation.Indistinct)
                    str += $"({connection.rotation.ToString()})";

                Handles.Label(v, str);
            }
        }

        private void OnValidate()
        {
            if(connections.Length != CONNECTIONS_COUNT) Array.Resize(ref connections, CONNECTIONS_COUNT);
        }
    }
}