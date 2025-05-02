using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private Rigidbody2D rb2d;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 2f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    //[SerializeField] private Transform playerRespawnPoint;
    [SerializeField] private string horizontalAxis;
    [SerializeField] private string jumpAxis;

    // private AudioSource audioSource;

    private bool _isGrounded;
    
    // crouch
    //[SerializeField] private float crouchSpeedFactor = 0.5f;
    [SerializeField] private CapsuleCollider2D capsuleCollider;
    private float _originalColliderHeight;
    private Vector2 _originalColliderOffset;
    private bool  _isCrouching;
    [SerializeField] private float crouchTransformYOffset = -0.25f;
    [SerializeField] private Transform playerVisualTransform;
    
    // acceleration
    [SerializeField] private float acceleration = 10f; 
    private float _currentSpeed = 0f;

    private void Start()
    {
        if (rb2d == null)
            rb2d = GetComponent<Rigidbody2D>();
        if(capsuleCollider == null)
            capsuleCollider = GetComponent<CapsuleCollider2D>();
        
        // audioSource = GetComponent<AudioSource>();
        
        _originalColliderHeight = capsuleCollider.size.y;
        _originalColliderOffset = capsuleCollider.offset;
    }

    private void FixedUpdate()
    {
        // if (!GameManager.Instance.gameRunning) return;
        float horizontalInput = Input.GetAxis(horizontalAxis);
        MovePlayer(horizontalInput);
    }

    private void Update()
    {
        // if (!GameManager.Instance.gameRunning) return;
        _isGrounded = CheckIsGrounded();
        if (_isGrounded && Input.GetButtonDown(jumpAxis))
        {
            Jump();
            animator.SetBool("Jump", true);
        }
        else
        {
                animator.SetBool("Jump", false);
        }
        animator.SetBool("IsGrounded", _isGrounded);
        
        bool wantToCrouch = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (wantToCrouch && _isGrounded)
            StartCrouch();
        else
            StopCrouch();
    }

    private void MovePlayer(float horizontalInput)
    {
        float targetSpeed = horizontalInput * moveSpeed;
        
        if(!_isCrouching) // accelerate only when not crouching
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        rb2d.linearVelocity = new Vector2(_currentSpeed, rb2d.linearVelocity.y);
        
        animator.SetFloat("Speed", Math.Abs(rb2d.linearVelocity.x));
        if (rb2d.linearVelocity.x < 0f)
            spriteRenderer.flipX = true;
        if (rb2d.linearVelocity.x > 0f)
            spriteRenderer.flipX = false;
    }

    private bool CheckIsGrounded()
    {
        if (Physics2D.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void Jump()
    {
        //audioSource.Play();
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
        
        capsuleCollider.size  = new Vector2(capsuleCollider.size.x, _originalColliderHeight * 0.7f);
        capsuleCollider.offset = new Vector2(_originalColliderOffset.x, _originalColliderOffset.y - (_originalColliderHeight * 0.25f));
        animator?.SetBool("Crouch", true);
    }

    private void StopCrouch()
    {
        if (!_isCrouching) return;
        
        // Raycast up to see if there's space to stand
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.up, _originalColliderHeight, groundLayer);
        if (hit.collider != null)
            return; // still blocked — stay crouched

        _isCrouching = false;
        
        capsuleCollider.size   = new Vector2(capsuleCollider.size.x, _originalColliderHeight);
        capsuleCollider.offset = _originalColliderOffset;
        animator?.SetBool("Crouch", false);
        
        playerVisualTransform.localPosition = new Vector3(
            playerVisualTransform.localPosition.x, 
            0f, 
            playerVisualTransform.localPosition.z);
    }
}