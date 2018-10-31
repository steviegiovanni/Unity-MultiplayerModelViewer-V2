﻿// author: Stevie Giovanni

using ModelViewer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Multiplayer
{
    /// <summary>
    /// multiplayer interface that encapsulates the multi parts object class so that it will work with multiplayer
    /// </summary>
    public class MultiuserMPOInterface : Photon.PunBehaviour, IPunObservable
    {
        /// <summary>
        /// the multiparts object associated with this interface
        /// </summary>
        private MultiPartsObject _mpo;
        public MultiPartsObject MPO
        {
            get
            {
                if (_mpo == null)
                    _mpo = GetComponent<MultiPartsObject>();
                return _mpo;
            }
        }

        /// <summary>
        /// the task list associated with this interface
        /// </summary>
        private TaskList _taskList;
        public TaskList TaskList
        {
            get
            {
                if (_taskList == null)
                    _taskList = GetComponent<TaskList>();
                return _taskList;
            }
        }

        /// <summary>
        /// list of nodes in the MPO for easy access by index
        /// </summary>
        private List<Node> _nodes;
        public List<Node> Nodes
        {
            get
            {
                if (_nodes == null)
                {
                    _nodes = new List<Node>();
                    List<Node> toProcess = new List<Node>();
                    toProcess.Add(MPO.Root);
                    while (toProcess.Count != 0)
                    {
                        Node curNode = toProcess[0];
                        _nodes.Add(toProcess[0]);
                        foreach (var child in curNode.Childs)
                            toProcess.Add(child);
                        toProcess.RemoveAt(0);
                    }
                }
                return _nodes;
            }
        }

        /// <summary>
        /// list of ownerIds that is currently the owner of the node
        /// </summary>
        private List<int> _ownerIds;
        public List<int> OwnerIds
        {
            get
            {
                if (_ownerIds == null)
                {
                    _ownerIds = new List<int>();
                    foreach (var node in Nodes)
                        _ownerIds.Add(-1);
                }
                return _ownerIds;
            }
        }

        

        /// <summary>
        /// - synchronize the current task id
        /// - synchronize the list of ownerid array
        /// - synchronize the transforms of all nodes
        /// </summary>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.isWriting)
            {
                // we own this player: send the others our data
                stream.SendNext(TaskList.CurrentTaskId);

                // send owner ids
                int [] ownerIdsArray = OwnerIds.ToArray();
                stream.SendNext(ownerIdsArray);
                
                // send positionas and rotations
                Vector3[] positions = new Vector3[Nodes.Count];
                Quaternion[] rotations = new Quaternion[Nodes.Count];
                for(int i=0; i < Nodes.Count; i++)
                {
                    positions[i] = Nodes[i].GameObject.transform.position;
                    rotations[i] = Nodes[i].GameObject.transform.rotation;
                }

                stream.SendNext(positions);
                stream.SendNext(rotations);
            }
            else
            {
                // network player, receive data
                TaskList.CurrentTaskId = (int)stream.ReceiveNext();

                // get owner ids array
                int[] ownerIdsArray = (int[])stream.ReceiveNext();
                for (int j = 0; j < ownerIdsArray.Length; j++)
                    OwnerIds[j] = ownerIdsArray[j];

                // read positions and rotations
                Vector3[] positions = (Vector3[])stream.ReceiveNext();
                Quaternion[] rotations = (Quaternion[])stream.ReceiveNext();
                for (int i = 0; i < Nodes.Count; i++)
                {
                    if (ownerIdsArray[i] != PhotonNetwork.player.ID || ownerIdsArray[i] == -1)
                        Nodes[i].GameObject.transform.SetPositionAndRotation(positions[i], rotations[i]);
                }
            }
        }

        /// <summary>
        /// transfer ownership when requested
        /// </summary>
        public override void OnOwnershipRequest(object[] viewAndPlayer)
        {
            //MPO.ReleaseCage();
            
            if (photonView.isMine)
            {
                PhotonView view = viewAndPlayer[0] as PhotonView;
                PhotonPlayer requestingPlayer = viewAndPlayer[1] as PhotonPlayer;
                photonView.TransferOwnership(requestingPlayer);
            }
            //base.OnOwnershipRequest(viewAndPlayer);
        }

        public void Start()
        {
            TaskList.OnTaskFinished.RemoveAllListeners();
            TaskList.OnTaskFinished.AddListener(IncrementTaskId);
        }

        // Update is called once per frame
        void Update()
        {
            // test input without AR/VR setup
            if (Input.GetKeyUp(KeyCode.Q))
            {
                GrabCage();
            }

            if (Input.GetKeyUp(KeyCode.W))
            {
                ReleaseCage();
            }

            if (Input.GetKeyUp(KeyCode.A))
            {
                Select();
            }

            if (Input.GetKeyUp(KeyCode.S))
            {
                Deselect();
            }

            if (Input.GetKeyUp(KeyCode.Z))
            {
                Grab();
            }

            if (Input.GetKeyUp(KeyCode.X))
            {
                Release();
            }

            if (PhotonNetwork.connected)
            {
                if (!PhotonNetwork.isMasterClient)
                {
                    for (int i = 0; i < Nodes.Count; i++)
                    {
                        if (OwnerIds[i] == PhotonNetwork.player.ID)
                            photonView.RPC("UpdateNodeTransform", PhotonTargets.MasterClient, i, Nodes[i].GameObject.transform.position, Nodes[i].GameObject.transform.rotation);
                    }
                }
            }

            // enables tasks associated with the node that we own and disables the ones we don't own
            for(int j = 0; j < OwnerIds.Count; j++)
            {
                foreach(var task in TaskList.Tasks)
                {
                    if (task.GameObject == Nodes[j].GameObject)
                        task.Enabled = (OwnerIds[j] == PhotonNetwork.player.ID);
                }
            }
        }

        [PunRPC]
        void UpdateNodeTransform(int nodeId, Vector3 position, Quaternion rotation, PhotonMessageInfo info)
        {
            List<Vector3> childPos = new List<Vector3>();
            List<Quaternion> childRot = new List<Quaternion>();
            foreach(Transform child in Nodes[nodeId].GameObject.transform)
            {
                childPos.Add(child.position);
                childRot.Add(child.rotation);
            }


            Nodes[nodeId].GameObject.transform.SetPositionAndRotation(position, rotation);

            int i = 0;
            foreach (Transform child in Nodes[nodeId].GameObject.transform)
            {
                child.SetPositionAndRotation(childPos[i],childRot[i]);
                i++;
            }
        }

        [PunRPC]
        void GrabNode(int nodeId, PhotonMessageInfo info)
        {
            OwnerIds[nodeId] = info.sender.ID;
        }

        [PunRPC]
        void IncrementTaskIdRPC(PhotonMessageInfo info)
        {
            TaskList.CurrentTaskId++;
        }

        public void IncrementTaskId()
        {
            if(!photonView.isMine && PhotonNetwork.connected)
                photonView.RPC("IncrementTaskIdRPC", PhotonTargets.MasterClient);
            else
                TaskList.IncrementTaskId();
        }

        public void GrabCage()
        {
            // take ownership of the cage
            if (!photonView.isMine)
                photonView.RequestOwnership();

            MPO.GrabCage();
        }

        public void ReleaseCage()
        {
            MPO.ReleaseCage();
        }

        public void Select()
        {
            MPO.Select();
        }

        public void Deselect()
        {
            MPO.Deselect();
        }

        public void ToggleSelect()
        {
            MPO.ToggleSelect();
        }

        public void Grab()
        {
            // take ownership of each of the selected node
            foreach(var node in MPO.SelectedNodes)
            {
                int i = 0;
                while (Nodes[i] != node)
                    i++;

                if(PhotonNetwork.connected)
                    photonView.RPC("GrabNode", PhotonTargets.MasterClient, i);

                //if (!node.GameObject.GetComponent<MultiuserMPOPartInterface>().photonView.isMine)
                //    node.GameObject.GetComponent<MultiuserMPOPartInterface>().photonView.RequestOwnership();
                /*foreach (var task in TaskList.Tasks) {
                    if (task.GameObject == node.GameObject)
                        task.Enabled = true;
                }*/
            }
            MPO.GrabIfPointingAt();
        }

        public void Release() {
            MPO.Release();
        }
    }
}
