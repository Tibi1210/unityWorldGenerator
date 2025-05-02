using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// This class handles the movement of the player with given input from the input manager
/// </summary>
public class PlayerController : MonoBehaviour
{

    [Header("Settings")]
    [Tooltip("Player speed")]
    public float moveSpeed = 2f;
    public float lookSpeed = 60f;
    public float jumpPower = 8f;
    public float gravity = 9.81f;


    private CharacterController characterController;
    private InputManager inputManager;

    private void setUpCharacterController()
    {
        characterController = GetComponent<CharacterController>();
        if(characterController == null )
        {
            Debug.LogError("no character controller on game object");
        }
    }
    private void setUpInputmanager()
    {
        inputManager = InputManager.instance;
    }

    Vector3 moveDir;
    private void processMovement()
    {
        float leftrightInput = inputManager.horizontalMoveAxis;
        float forwardBackwardInput = inputManager.verticalMoveAxis;
        bool jumpPressed = inputManager.jumpPressed;
        if (characterController.isGrounded)
        {
            moveDir = new Vector3(leftrightInput, 0, forwardBackwardInput);
            moveDir = transform.TransformDirection(moveDir);
            moveDir *= moveSpeed;

            if (jumpPressed)
            {
                moveDir.y = jumpPower;
            }
        }
        else
        {
            moveDir = new Vector3(leftrightInput * moveSpeed, moveDir.y, forwardBackwardInput * moveSpeed);
            moveDir = transform.TransformDirection(moveDir);
        }

        moveDir.y -= gravity * Time.deltaTime;

        if (characterController.isGrounded && moveDir.y < 0)
        {
            moveDir.y = -0.3f;
        }

        characterController.Move(moveDir * Time.deltaTime);
    }

    private void processRotation()
    {
        float horizontalLookInput = inputManager.horizontalLookAxis;
        Vector3 playerRotation = transform.rotation.eulerAngles;

        transform.rotation = Quaternion.Euler(new Vector3(playerRotation.x,playerRotation.y + horizontalLookInput * lookSpeed * Time.deltaTime, playerRotation.z));
    }

    /// <summary>
    /// Description:
    /// Standard Unity function called once before the first Update call
    /// Input:
    /// none
    /// Return:
    /// void (no return)
    /// </summary>
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        setUpCharacterController();
        setUpInputmanager();
    }

    /// <summary>
    /// Description:
    /// Standard Unity function called once every frame
    /// Input:
    /// none
    /// Return:
    /// void (no return)
    /// </summary>
    void Update()
    {
        processMovement();
        processRotation();
    }
}
