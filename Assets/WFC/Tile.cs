using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Balma.WFC
{
    public class Tile : MonoBehaviour
    {
        [Serializable]
        public struct Connection
        {
            public string key;
            public ConnectionData.Type connectionType;
        }
    
        public Connection[] connections = new Connection[4];
        
        private void OnDrawGizmos()
        {
            var v = 0.25f;
            //Handles.matrix = transform.localToWorldMatrix;
            Handles.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one);
            Draw(new Vector3(v,v,0),  connections[0]);
            Draw(new Vector3(0,v,v),  connections[1]);
            Draw(new Vector3(-v,v,0), connections[2]);
            Draw(new Vector3(0,v,-v), connections[3]);
            Handles.matrix = Matrix4x4.identity;
        }

        private void Draw(Vector3 v, Connection connection)
        {
            var type = "";
            if (connection.connectionType == ConnectionData.Type.Forward) type = " ???";
            if (connection.connectionType == ConnectionData.Type.Reverse) type = " !!!";
            
            Handles.Label(v, connection.key + type);
        }

        private void OnValidate()
        {
            if(connections.Length != 4) connections = new Connection[4];
        }

        public TileData ToTileData()
        {
            var data = new TileData();
            data.cd0 = new ConnectionData() {key = connections[0].key, type = connections[0].connectionType};
            data.cd1 = new ConnectionData() {key = connections[1].key, type = connections[1].connectionType};
            data.cd2 = new ConnectionData() {key = connections[2].key, type = connections[2].connectionType};
            data.cd3 = new ConnectionData() {key = connections[3].key, type = connections[3].connectionType};
            data.prefab = this.gameObject;
            data.prefabRotation = this.transform.rotation;
            return data;
        }
    }
}


