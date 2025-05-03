using System;
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

    private bool _isGrounded;
    private bool _isCrouching;
    private float _currentSpeed;
    private float _originalColliderHeight;
    private Vector2 _originalColliderOffset;

    private void Start()
    {
        rb2d ??= GetComponent<Rigidbody2D>();
        capsuleCollider ??= GetComponent<CapsuleCollider2D>();

        _originalColliderHeight = capsuleCollider.size.y;
        _originalColliderOffset = capsuleCollider.offset;
    }

    private void FixedUpdate()
    {
        if (!GameManager.Instance.gameRunning) return;
        
        float horizontalRaw = Input.GetAxisRaw(horizontalAxis);
        MovePlayer(horizontalRaw);
    }

    private void Update()
    {
        if (!GameManager.Instance.gameRunning) return;

        _isGrounded = CheckIsGrounded();
        animator.SetBool("IsGrounded", _isGrounded);

        if (_isGrounded && Input.GetButtonDown(jumpAxis))
        {
            Jump();
            animator.SetBool("Jump", true);
        }
        else
        {
            animator.SetBool("Jump", false);
        }

        bool wantToCrouch = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.S);
        if (wantToCrouch && _isGrounded)
            StartCrouch();
        else
            StopCrouch();
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
            float acc = acceleration;

            if (_isCrouching)
            {
                targetSpeed = horizontalInput * moveSpeed * 0.5f;
                acc = crouchAcceleration;
            }
            
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

        capsuleCollider.size = new Vector2(capsuleCollider.size.x, _originalColliderHeight * 0.7f);
        capsuleCollider.offset = new Vector2(_originalColliderOffset.x, _originalColliderOffset.y - (_originalColliderHeight * 0.25f));
        animator.SetBool("Crouch", true);
    }

    private void StopCrouch()
    {
        if (!_isCrouching) return;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.up, _originalColliderHeight, groundLayer);
        if (hit.collider != null) return;

        _isCrouching = false;
        capsuleCollider.size = new Vector2(capsuleCollider.size.x, _originalColliderHeight);
        capsuleCollider.offset = _originalColliderOffset;
        animator.SetBool("Crouch", false);

        playerVisualTransform.localPosition = new Vector3(
            playerVisualTransform.localPosition.x,
            0f,
            playerVisualTransform.localPosition.z);
    }
}
