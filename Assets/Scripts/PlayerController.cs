using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float acceleration = 10f;

    [Header("Jump")]
    [SerializeField] private float jumpForce;

    [Header("Physics")]
    [SerializeField] private Rigidbody2D rb2d;
    [SerializeField] private CapsuleCollider2D capsuleCollider;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 2f;

    [Header("Crouch")]
    [SerializeField] private float crouchTransformYOffset = -0.25f;
    [SerializeField] private Transform playerVisualTransform;
    [SerializeField] private float crouchAcceleration = 4f;

    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Input")]
    [SerializeField] private string horizontalAxis;
    [SerializeField] private string jumpAxis;

    [Header("Rewind")]
    [Tooltip("Where to teleport back to when rewind starts")]
    [SerializeField] private Transform startPoint;
    [Tooltip("Trigger Collider (IsTrigger) that starts rewind")]
    [SerializeField] private Collider2D finishPoint;
    [SerializeField] private bool chaser;
    
    private bool _isGrounded;
    private bool _isCrouching;
    private float _currentSpeed;
    private float _originalColliderHeight;
    private Vector2 _originalColliderOffset;
    private bool _isRewinding;

    // data structure to record each physics frame
    private struct FrameData
    {
        public Vector2 position;
        public Vector2 velocity;
        public bool    isCrouching;
        public float   deltaTime;
    }
    private List<FrameData> _recordedFrames = new List<FrameData>();

    private void Start()
    {
        rb2d ??= GetComponent<Rigidbody2D>();
        capsuleCollider ??= GetComponent<CapsuleCollider2D>();

        _originalColliderHeight = capsuleCollider.size.y;
        _originalColliderOffset = capsuleCollider.offset;
    }

    private void FixedUpdate()
    {
        if (_isRewinding || !GameManager.Instance.gameRunning) return;

        // record this physics frame
        if (!chaser)
        {
            _recordedFrames.Add(new FrameData
            {
                position    = rb2d.position,
                velocity    = rb2d.linearVelocity,
                isCrouching = _isCrouching,
                deltaTime   = Time.fixedDeltaTime
            });   
        }
        float horizontalRaw = Input.GetAxisRaw(horizontalAxis);
        MovePlayer(horizontalRaw);
    }

    private void Update()
    {
        if (_isRewinding || !GameManager.Instance.gameRunning) return;

        _isGrounded = CheckIsGrounded();
        animator.SetBool("IsGrounded", _isGrounded);

        if (_isGrounded && Input.GetButtonDown(jumpAxis))
        {
            Jump();
            animator.SetBool("Jump", true);
        }
        else animator.SetBool("Jump", false);

        bool wantToCrouch = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.S);
        if (wantToCrouch && _isGrounded) StartCrouch();
        else StopCrouch();
    }

    private void MovePlayer(float horizontalInput)
    {
        if (Mathf.Approximately(horizontalInput, 0f))
        {
            _currentSpeed = 0f;
        }
        else
        {
            float targetSpeed = horizontalInput * moveSpeed;
            float acc = _isCrouching ? crouchAcceleration : acceleration;
            if (_isCrouching) targetSpeed = 0.5f; 

            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acc * Time.fixedDeltaTime);
        }

        rb2d.linearVelocity = new Vector2(_currentSpeed, rb2d.linearVelocity.y);
        animator.SetFloat("Speed", Mathf.Abs(_currentSpeed));

        if (_currentSpeed < 0f) spriteRenderer.flipX = true;
        else if (_currentSpeed > 0f) spriteRenderer.flipX = false;
    }

    private bool CheckIsGrounded()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
    }

    private void Jump()
    {
        rb2d.linearVelocity = new Vector2(rb2d.linearVelocity.x, jumpForce);
    }

    private void StartCrouch()
    {
        if (_isCrouching) return;
        _isCrouching = true;

        playerVisualTransform.localPosition = new Vector3(
            playerVisualTransform.localPosition.x,
            crouchTransformYOffset,
            playerVisualTransform.localPosition.z);

        capsuleCollider.size   = new Vector2(capsuleCollider.size.x, _originalColliderHeight * 0.7f);
        capsuleCollider.offset = new Vector2(_originalColliderOffset.x, 
            _originalColliderOffset.y - (_originalColliderHeight * 0.25f));
        animator.SetBool("Crouch", true);
    }

    private void StopCrouch()
    {
        if (!_isCrouching) return;
        if (Physics2D.Raycast(transform.position, Vector2.up, _originalColliderHeight, groundLayer)) 
            return;

        _isCrouching = false;
        capsuleCollider.size   = new Vector2(capsuleCollider.size.x, _originalColliderHeight);
        capsuleCollider.offset = _originalColliderOffset;
        animator.SetBool("Crouch", false);

        playerVisualTransform.localPosition = new Vector3(
            playerVisualTransform.localPosition.x, 0f, playerVisualTransform.localPosition.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(_isRewinding) return;
        if (other == finishPoint && !chaser)
        {
            GameManager.Instance.StopTimer(); 
            GameManager.Instance.InitRewind();
            StartCoroutine(RewindRoutine());
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (chaser && other.gameObject.CompareTag("Player") && GameManager.Instance.gameRunning)
        {
            GameManager.Instance.CatchPlayer();
        }
    }

    private IEnumerator RewindRoutine()
    {
        _isRewinding = true;
        
        var originalInterpolation = rb2d.interpolation;
        
        rb2d.bodyType = RigidbodyType2D.Kinematic;
        rb2d.interpolation = RigidbodyInterpolation2D.None;
        rb2d.linearVelocity = Vector2.zero;
        rb2d.angularVelocity = 0f;
        
        rb2d.position = startPoint.position; 
        transform.position = startPoint.position;
        StopCrouch();
        _isCrouching = false;

        yield return new WaitForFixedUpdate();
        
        for (int i = 0; i < _recordedFrames.Count; i++)
        {
            if(GameManager.Instance.gameRunning == false) break;
            FrameData frame = _recordedFrames[i];
            
            if (frame.isCrouching && !_isCrouching)
            {
                StartCrouch(); // Call only if state changed
            }
            else if (!frame.isCrouching && _isCrouching)
            {
                StopCrouch(); // Call only if state changed
            }
            _isCrouching = frame.isCrouching;
            
            rb2d.MovePosition(frame.position);
            
            float speed = frame.velocity.magnitude;
            animator.SetFloat("Speed", speed);
            
            if (frame.velocity.x < -0.1f) spriteRenderer.flipX = true;
            else if (frame.velocity.x > 0.1f) spriteRenderer.flipX = false;

            // Wait for the next physics update cycle
            yield return new WaitForFixedUpdate();
        }
        
        rb2d.bodyType = RigidbodyType2D.Dynamic;
        rb2d.interpolation = originalInterpolation;
        
        _recordedFrames.Clear();
        
        if (_isCrouching) StopCrouch();

        animator.SetFloat("Speed", 0f);
        animator.SetBool("Crouch", false);

        _isRewinding = false;
        if(GameManager.Instance.gameRunning)
            GameManager.Instance.PlayerEscaped();
    }
}
