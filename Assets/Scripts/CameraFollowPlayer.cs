using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{
    [Tooltip("The player (or target) the camera will follow)")]
    [SerializeField] public Transform player;

    private float _initialY;
    private float _initialZ;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("CameraFollowPlayer: No player assigned!", this);
            enabled = false;
            return;
        }
      
        _initialY = transform.position.y;
        _initialZ = transform.position.z;
    }
    
    private void LateUpdate()
    {
        
        Vector3 targetPos = new Vector3(player.position.x, _initialY, _initialZ);
        transform.position = targetPos;
    }
}