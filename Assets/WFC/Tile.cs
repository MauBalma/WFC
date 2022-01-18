using System;
using UnityEditor;
using UnityEngine;

namespace Balma.WFC
{
    public class Tile : MonoBehaviour
    {
        [Serializable]
        public struct Connection
        {
            public enum Type
            {
                Normal, Forward, Reverse
            }
            
            public string key;
            public Type type;
        }
    
        public Connection[] connections = new Connection[4];
        public bool rotable = true;
        
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
            if (connection.type == Connection.Type.Forward) type = " ???";
            if (connection.type == Connection.Type.Reverse) type = " !!!";
            
            Handles.Label(v, connection.key + type);
        }

        private void OnValidate()
        {
            if(connections.Length != 4) connections = new Connection[4];
        }
    }
}


