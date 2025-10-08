using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System;

public class PlayerMovement : MonoBehaviour
{
    public float diagonalAnimationAdjustmentTime = 0.099f; //SYSTEM: Delay animation time from diagonal: Adjust this value as needed
    private bool isUpdatingLastDirection = false; // System: Prevent multiple coroutines

    public bool forceDiagonalMovement; //TEST: Immediately makes movement diagonal to test animations

    Animator animator;
    private Rigidbody2D rb;
    private Vector2 movement;
    private float lastDirection;
    private float currentSpeed;
    public float walkSpeed = 5f;
    public float runSpeed = 10f;

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        currentSpeed = walkSpeed; // Set initial speed
    }

    private void FixedUpdate()
    {
        //Controls movement
        rb.linearVelocity = movement * currentSpeed;

    }

    private void Update()
    {
        //Note: Animator should create a blend tree for the 8 directions and set motion values according to values on DefineLastDirection()
        animator.SetFloat("Blend", lastDirection);
        animator.SetFloat("X", rb.linearVelocity.x);
        animator.SetFloat("Y", rb.linearVelocity.y);
        animator.SetFloat("Speed", rb.linearVelocity.magnitude);




        if (!isUpdatingLastDirection) // If moving away from diagonal, delay before switching to avoid animation flicker
        {
            StartCoroutine(DelayDirectionChange());
        }

    }
        
    private IEnumerator DelayDirectionChange()
    {
        isUpdatingLastDirection = true;
        yield return new WaitForSecondsRealtime(diagonalAnimationAdjustmentTime); // Wait before changing direction

        DefineLastDirection();

        //Debug.Log("lastDirection changed to: " + lastDirection);
        isUpdatingLastDirection = false;
    }

    public void OnMove(InputValue value)
    {
        //Debug.Log("InputValue: " + value);
        //Debug.Log("X: " + animator.GetFloat("X"));
        //Debug.Log("Y: " + animator.GetFloat("Y"));
        //Debug.Log("Blend: " + animator.GetFloat("Blend"));
        //Debug.Log("Speed: " + animator.GetFloat("Speed"));

        movement = value.Get<Vector2>();
        //Debug.Log("movement: " + movement);
        //Debug.Log("X: " + animator.GetFloat("X"));
        //Debug.Log("Y: " + animator.GetFloat("Y"));
        //Debug.Log("Blend: " + animator.GetFloat("Blend"));
        //Debug.Log("Speed: " + animator.GetFloat("Speed"));
    }

    //NOTE: needs a Press and release interaction on the action map button to work
    public void OnSprint(InputValue value)
    {
        if (value.isPressed)
        {
            currentSpeed = runSpeed;
            //Debug.Log("Sprint started");
        }
        else
        {
            currentSpeed = walkSpeed;
            //Debug.Log("Sprint canceled");
        }
        
    }

    void DefineLastDirection()
    {
        float tolerance = 0.2f;

        // Determine new direction after delay

        //Down
        if (Mathf.Abs(rb.linearVelocity.x - 0) < tolerance && Mathf.Abs(rb.linearVelocity.y - -1 * currentSpeed) < tolerance)
        {
            lastDirection = 0f;
        }

        //Right-Down
        else if (Mathf.Abs(rb.linearVelocity.x - .707f * currentSpeed) < tolerance && Mathf.Abs(rb.linearVelocity.y - (-.707f * currentSpeed)) < tolerance)
        {
            lastDirection = 0.5f;
        }

        //Right
        else if (Mathf.Abs(rb.linearVelocity.x - 1 * currentSpeed) < tolerance && Mathf.Abs(rb.linearVelocity.y - 0) < tolerance)
        {
            lastDirection = 1;
        }

        //Right-Up
        else if (Mathf.Abs(rb.linearVelocity.x - (0.707f * currentSpeed)) < tolerance && Mathf.Abs(rb.linearVelocity.y - (0.707f * currentSpeed)) < tolerance)
        {
            lastDirection = 1.5f;
            //Debug.Log(lastDirection);

        }

        //Up
        else if (Mathf.Abs(rb.linearVelocity.x - 0) < tolerance && Mathf.Abs(rb.linearVelocity.y - 1 * currentSpeed) < tolerance)
        {
            lastDirection = 2;
        }

        //Left-Up
        else if (Mathf.Abs(rb.linearVelocity.x - (-0.707f * currentSpeed)) < tolerance && Mathf.Abs(rb.linearVelocity.y - (0.707f * currentSpeed)) < tolerance)
        {
            lastDirection = 2.5f;
        }

        //Left
        else if (Mathf.Abs(rb.linearVelocity.x - -1 * currentSpeed) < tolerance && Mathf.Abs(rb.linearVelocity.y - 0) < tolerance)
        {
            lastDirection = 3f;
        }

        //Left-Down
        else if (Mathf.Abs(rb.linearVelocity.x - (-0.707f * currentSpeed)) < tolerance && Mathf.Abs(rb.linearVelocity.y - (-0.707f * currentSpeed)) < tolerance)
        {
            lastDirection = 3.5f;
        }
    }

}
