using MRTK.Tutorials.MultiUserCapabilities;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public class RightHandSync : MonoBehaviourPun, IPunObservable
{
    [SerializeField] private bool isUser = default;
    [SerializeField] private Transform _rightHand;
    [SerializeField] private List<Transform> _bones;

    private PhotonView photonView;
    private Transform rightHandRig;

    private Vector3 networkWristPosition;
    private Vector3 networkLocalScale;

    private Quaternion[] networkJointsRotations;
    private Transform[] localJointsArray;

    private const string endJointName = "end";

    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (enabled)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(_bones[0].localPosition);
                foreach (var joint in _bones)
                {
                    stream.SendNext(joint.localRotation);
                }
                stream.SendNext(transform.localScale);
            }
            else
            {
                networkWristPosition = (Vector3)stream.ReceiveNext();
                for (int i = 0; i < _bones.Count; i++)
                {
                    networkJointsRotations[i] = (Quaternion)stream.ReceiveNext();
                }
                networkLocalScale = (Vector3)stream.ReceiveNext();
            }
        }
    }

    void MapPosition(Transform target, Transform rigTransform)
    {
        target.position = rigTransform.position;
        target.rotation = rigTransform.rotation;
    }

    private void Awake()
    {
        networkJointsRotations = new Quaternion[_bones.Count];
        localJointsArray = new Transform[_bones.Count];
        for (int i = 0; i < _bones.Count; i++)
        {
            networkJointsRotations[i] = Quaternion.identity; // or any appropriate default rotation
        }
    }

    private void Start()
    {
        photonView = GetComponent<PhotonView>();

        if (isUser)
        {
            if (TableAnchor.Instance != null) transform.parent = FindObjectOfType<TableAnchor>().transform;

            if (photonView.IsMine) GenericNetworkManager.Instance.rightHandUser = photonView;
        }

        GameObject mrRig = GameObject.FindGameObjectWithTag("MRTK Rig");

        rightHandRig = mrRig.transform.Find("Camera Offset/MRTK RightHand Controller");

        if (photonView.IsMine)
        {
            foreach (var item in GetComponentsInChildren<Renderer>())
            {
                item.enabled = false;
            }
        }

        int index = 0;
        Transform modelParent = rightHandRig.GetChild(5).GetChild(0).GetChild(1);

        foreach (Transform child in modelParent.GetComponentsInChildren<Transform>())
        {
            // The "leaf joints" are excluded.
            if (child.name.Contains(endJointName)) { continue; }

            localJointsArray[index++] = child;
        }
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            _bones[0].localPosition = Vector3.Lerp(_bones[0].localPosition, networkWristPosition, Time.deltaTime * 10f);
            for (int i = 0; i < _bones.Count; i++)
            {
                _bones[i].localRotation = Quaternion.Slerp(_bones[i].localRotation, networkJointsRotations[i], Time.deltaTime * 10f);
            }
            transform.localScale = Vector3.Lerp(transform.localScale, networkLocalScale, Time.deltaTime * 10f);
        }

        if (photonView.IsMine && isUser)
        {
            MapPosition(_rightHand, rightHandRig);
            for (int i = 0; i < _bones.Count; i++)
            {
                MapPosition(_bones[i], localJointsArray[i]);
            }
        }
    }
}
