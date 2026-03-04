using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorCamera : NetworkBehaviour
{
    [Header("Orbit")]
    [SerializeField]
    private float mouseSensitivity = 0.2f;
    [SerializeField]
    private float orbitDistance = 5f;
    [SerializeField]
    private float orbitHeight = 2f;

    [Header("Zoom")]
    [SerializeField]
    private float minDistance = 2f;
    [SerializeField]
    private float maxDistance = 12f;
    [SerializeField]
    private float zoomSpeed = 4f;

    private Camera _spectatorCam;
    private float _orbitAngle;
    private float _currentDistance;
    private int _targetIndex;
    private bool _active;

    public void Activate()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        _currentDistance = orbitDistance;
        _active = true;
        _targetIndex = 0;

        GameObject camObj = new GameObject("SpectatorCamera");
        _spectatorCam = camObj.AddComponent<Camera>();
        camObj.AddComponent<AudioListener>();
    }

    public void Deactivate()
    {
        _active = false;
        if (_spectatorCam != null)
        {
            Destroy(_spectatorCam.gameObject);
            _spectatorCam = null;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer || !_active)
        {
            return;
        }

        HandleZoom();
        HandleCycle();
        UpdateCamera();
    }

    private void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll != 0f)
        {
            _currentDistance -= scroll * zoomSpeed * Time.deltaTime;
            _currentDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);
        }
    }

    private void HandleCycle()
    {
        int count = PlayerRegistry.AllAlive().Count;
        if (count <= 1)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            _targetIndex = (_targetIndex + 1) % count;
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            _targetIndex = (_targetIndex - 1 + count) % count;
        }
    }

    private void UpdateCamera()
    {
        List<Transform> alive = PlayerRegistry.AllAlive();
        if (alive.Count == 0)
        {
            return;
        }

        _targetIndex = Mathf.Clamp(_targetIndex, 0, alive.Count - 1);
        Transform target = alive[_targetIndex];

        _orbitAngle += Mouse.current.delta.ReadValue().x * mouseSensitivity;

        float rad = _orbitAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * _currentDistance;
        offset.y = orbitHeight;

        if (_spectatorCam == null)
        {
            return;
        }

        _spectatorCam.transform.position = target.position + offset;
        _spectatorCam.transform.LookAt(target.position + Vector3.up * orbitHeight * 0.5f);
    }

    private void OnDestroy()
    {
        if (_spectatorCam != null)
        {
            Destroy(_spectatorCam.gameObject);
        }
    }
}
