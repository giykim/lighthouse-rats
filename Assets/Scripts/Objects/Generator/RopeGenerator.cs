using Mirror;
using UnityEngine;

public class RopeGenerator : NetworkBehaviour
{
    [Header("Rope Settings")]
    [SerializeField]
    private int segmentCount = 10;
    [SerializeField]
    private float segmentLength = 0.2f;
    [SerializeField]
    private float segmentWidth = 0.05f;
    [SerializeField]
    private Transform topAnchorPoint;

    private GameObject[] _segments;
    private LineRenderer _lineRenderer;

    private void Start()
    {
        if (isServer)
        {
            GenerateRope();
        }

        SetupLineRenderer();
    }

    [Server]
    private void GenerateRope()
    {
        _segments = new GameObject[segmentCount];
        Rigidbody prevRb = null;
        NetworkIdentity[] segmentIdentities = new NetworkIdentity[segmentCount];

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segment = new GameObject($"RopeSegment_{i}");
            segment.transform.SetParent(transform);
            segment.transform.position = topAnchorPoint.position - Vector3.up * (i * segmentLength);

            Rigidbody rb = segment.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            rb.linearDamping = 0.5f;

            CapsuleCollider col = segment.AddComponent<CapsuleCollider>();
            col.height = segmentLength;
            col.radius = segmentWidth;

            NetworkIdentity netId = segment.AddComponent<NetworkIdentity>();

            if (i == 0)
            {
                rb.isKinematic = true;
            }
            else
            {
                CharacterJoint joint = segment.AddComponent<CharacterJoint>();
                joint.enableProjection = true;
                joint.connectedBody = prevRb;
                SoftJointLimit limit = new SoftJointLimit();
                limit.limit = 20f;
                joint.lowTwistLimit = limit;
                joint.highTwistLimit = limit;
                joint.swing1Limit = limit;
                joint.swing2Limit = limit;
            }

            _segments[i] = segment;
            prevRb = rb;
            NetworkServer.Spawn(segment);
            segmentIdentities[i] = netId;
        }

        RpcReceiveSegments(segmentIdentities);
    }

    [ClientRpc]
    private void RpcReceiveSegments(NetworkIdentity[] segmentIdentities)
    {
        _segments = new GameObject[segmentIdentities.Length];
        for (int i = 0; i < segmentIdentities.Length; i++)
        {
            if (segmentIdentities[i] != null)
                _segments[i] = segmentIdentities[i].gameObject;
        }
    }

    private void SetupLineRenderer()
    {
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.startWidth = segmentWidth;
        _lineRenderer.endWidth = segmentWidth;
        _lineRenderer.positionCount = segmentCount;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = new Color(0.6f, 0.4f, 0.2f);
        _lineRenderer.endColor = new Color(0.6f, 0.4f, 0.2f);
    }

    private void Update()
    {
        if (_segments == null)
        {
            return;
        }

        for (int i = 0; i < _segments.Length; i++)
        {
            if (_segments[i] != null)
            {
                _lineRenderer.SetPosition(i, _segments[i].transform.position);
            }
        }
    }

    public GameObject[] Segments => _segments;
    public int SegmentCount => segmentCount;
}
