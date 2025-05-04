using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [Tooltip("Rate to accelerate toward target speed when pressing direction")]
    [SerializeField] private float accelRate   =  50f;
    [Tooltip("Rate to brake to zero when no input")]
    [SerializeField] private float brakeRate   =  80f;
    [Tooltip("Rate to reverse direction when input flips sign")]
    [SerializeField] private float turnRate    = 120f;

    [Header("Powerups")]
    [Tooltip("Indicator to show when speed-up is active")]
    [SerializeField] private GameObject speedUpIndicator;
    [Tooltip("Indicator to show when slow-down is active")]
    [SerializeField] private GameObject slowDownIndicator;
    [Tooltip("Multiplier applied to moveSpeed on speed-up")]
    [SerializeField] private float speedUpMultiplier = 1.5f;
    [Tooltip("Multiplier applied to moveSpeed on slow-down")]
    [SerializeField] private float slowDownMultiplier = 0.5f;
    [Tooltip("Duration of powerup effect in seconds")]
    [SerializeField] private float powerupDuration = 3f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 12f;

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
    [SerializeField] private string horizontalAxis = "Horizontal";
    [SerializeField] private string jumpAxis       = "Jump";

    [Header("Rewind")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Collider2D finishPoint;
    [SerializeField] private bool chaser;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpClip;

    private bool _isGrounded;
    private bool _isCrouching;
    private float _currentSpeed;
    private float _lastInput;
    private float _originalColliderHeight;
    private Vector2 _originalColliderOffset;
    private bool _isRewinding;
    private float originalMoveSpeed;

    private Coroutine _powerupCoroutine;

    // Recorded frame data for rewind
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

        // Ensure indicators are off at start
        if (speedUpIndicator != null) speedUpIndicator.SetActive(false);
        if (slowDownIndicator != null) slowDownIndicator.SetActive(false);

        GameManager.Instance.gameLost += Stop;
        GameManager.Instance.gameWon  += Stop;
        
        originalMoveSpeed = moveSpeed;
    }

    private void Stop()
    {
        rb2d.linearVelocity = Vector2.zero;
        animator.SetFloat("Speed", 0f);
        animator.SetBool("Jump", false);
        animator.SetBool("Crouch", false);
    }

    private void FixedUpdate()
    {
        if (_isRewinding || !GameManager.Instance.gameRunning) return;

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

        // Ground & Jump
        _isGrounded = CheckIsGrounded();
        animator.SetBool("IsGrounded", _isGrounded);

        if (_isGrounded && Input.GetButtonDown(jumpAxis))
        {
            Jump();
            animator.SetBool("Jump", true);
        }
        else animator.SetBool("Jump", false);

        // Crouch
        bool wantToCrouch = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.S);
        if (wantToCrouch && _isGrounded) StartCrouch();
        else StopCrouch();
    }

    private void MovePlayer(float h)
    {
        float targetSpeed = h * moveSpeed;

        // Snap to zero on input change to avoid momentum carry
        if (!Mathf.Approximately(h, _lastInput))
        {
            rb2d.linearVelocity = new Vector2(0f, rb2d.linearVelocity.y);
            _currentSpeed = 0f;
        }

        // Determine which rate to use
        float rate;
        if (Mathf.Approximately(h, 0f))
            rate = brakeRate;
        else if (Mathf.Sign(targetSpeed) != Mathf.Sign(_currentSpeed))
            rate = turnRate;
        else
            rate = accelRate;
        
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);
        rb2d.linearVelocity = new Vector2(_currentSpeed, rb2d.linearVelocity.y);
        
        animator.SetFloat("Speed", Mathf.Abs(_currentSpeed));
        spriteRenderer.flipX = (_currentSpeed < 0f);

        _lastInput = h;
    }

    private bool CheckIsGrounded()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
    }

    private void Jump()
    {
        audioSource.PlayOneShot(jumpClip);
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
        if (Physics2D.Raycast(transform.position, Vector2.up, _originalColliderHeight - 0.5f, groundLayer))
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
        if (_isRewinding) return;

        // Finish line
        if (other == finishPoint && !chaser)
        {
            GameManager.Instance.StopTimer();
            GameManager.Instance.InitRewind();
            StartCoroutine(RewindRoutine());
            return;
        }

        // Powerup triggers
        if (other.CompareTag("SpeedUp"))
        {
            Destroy(other.gameObject);
            ActivatePowerup(true);
        }
        else if (other.CompareTag("SlowDown"))
        {
            Destroy(other.gameObject);
            ActivatePowerup(false);
        }
    }

    private void ActivatePowerup(bool isSpeedUp)
    {
        // Cancel any existing powerup
        if (_powerupCoroutine != null)
        {
            StopCoroutine(_powerupCoroutine);
            ResetPowerup();
        }

        // Start new effect
        _powerupCoroutine = StartCoroutine(PowerupRoutine(isSpeedUp));
    }

    private IEnumerator PowerupRoutine(bool isSpeedUp)
    {
        // Choose multiplier and indicator
        float originalSpeed = originalMoveSpeed;
        float multiplier = isSpeedUp ? speedUpMultiplier : slowDownMultiplier;
        GameObject indicator = isSpeedUp ? speedUpIndicator : slowDownIndicator;

        // Apply effect
        moveSpeed = originalSpeed * multiplier;
        if (indicator != null) indicator.SetActive(true);

        // Wait
        yield return new WaitForSeconds(powerupDuration);

        // Reset
        moveSpeed = originalSpeed;
        ResetPowerup();
        _powerupCoroutine = null;
    }

    private void ResetPowerup()
    {
        if (speedUpIndicator != null) speedUpIndicator.SetActive(false);
        if (slowDownIndicator != null) slowDownIndicator.SetActive(false);
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

        yield return new WaitForFixedUpdate();

        foreach (var frame in _recordedFrames)
        {
            if (!GameManager.Instance.gameRunning) break;

            if (frame.isCrouching && !_isCrouching) StartCrouch();
            else if (!frame.isCrouching && _isCrouching) StopCrouch();
            _isCrouching = frame.isCrouching;

            rb2d.MovePosition(frame.position);
            float speed = frame.velocity.magnitude;
            animator.SetFloat("Speed", speed);
            spriteRenderer.flipX = (frame.velocity.x < -0.1f);

            yield return new WaitForFixedUpdate();
        }

        rb2d.bodyType = RigidbodyType2D.Dynamic;
        rb2d.interpolation = originalInterpolation;
        _recordedFrames.Clear();

        if (_isCrouching) StopCrouch();
        animator.SetFloat("Speed", 0f);
        animator.SetBool("Crouch", false);
        _isRewinding = false;

        if (GameManager.Instance.gameRunning)
            GameManager.Instance.PlayerEscaped();
    }
}
