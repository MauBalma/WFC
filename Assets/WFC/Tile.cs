using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Balma.WFC
{
    //TODO Scriptable terrain types?
    public enum TerrainType
    {
        None, Air, Grass
    }

    public class Tile : MonoBehaviour
    {
        private const int CONNECTIONS_COUNT = 8;
        
        [Serializable]
        public struct Connection
        {
            public TerrainType key;
        }

        public bool rotable = true;
        public Connection[] connections = new Connection[CONNECTIONS_COUNT];

        private void OnDrawGizmosSelected()
        {
            var v = 0.25f;
            Handles.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one);
            Draw(new Vector3(v,-v,v),   connections[0]);
            Draw(new Vector3(-v,-v,v),  connections[1]);
            Draw(new Vector3(-v,-v,-v), connections[2]);
            Draw(new Vector3(v,-v,-v),  connections[3]);
            Draw(new Vector3(v,v,v),   connections[0+4]);
            Draw(new Vector3(-v,v,v),  connections[1+4]);
            Draw(new Vector3(-v,v,-v), connections[2+4]);
            Draw(new Vector3(v,v,-v),  connections[3+4]);
            Handles.matrix = Matrix4x4.identity;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one*.9f);
        }

        private void Draw(Vector3 v, Connection connection)
        {
            Handles.Label(v, connection.key.ToString());
        }

        private void OnValidate()
        {
            if(connections.Length != CONNECTIONS_COUNT) Array.Resize(ref connections, CONNECTIONS_COUNT);
        }

        public TileData ToTileData()
        {
            var data = new TileData();
            for (int i = 0; i < CONNECTIONS_COUNT; i++)
            {
                data[i] = new ConnectionData() {key = connections[i].key};
            }
            data.prefab = this.gameObject;
            data.prefabRotation = this.transform.rotation;
            return data;
        }
    }
}


