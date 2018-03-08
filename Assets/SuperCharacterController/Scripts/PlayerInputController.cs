using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerInputController : MonoBehaviour {

    public PlayerInput Current;
    public Vector2 RightStickMultiplier = new Vector2(3, -1.5f);

	// Use this for initialization
	void Start () {
        Current = new PlayerInput();
	}

	// Update is called once per frame
	void Update () {
        
        // Retrieve our current WASD or Arrow Key input
        // Using GetAxisRaw removes any kind of gravity or filtering being applied to the input
        // Ensuring that we are getting either -1, 0 or 1
        Vector3 moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        Vector2 rightStickInput = new Vector2(Input.GetAxisRaw("RightH"), Input.GetAxisRaw("RightV"));

        // pass rightStick values in place of mouse when non-zero
        mouseInput.x = rightStickInput.x != 0 ? rightStickInput.x * RightStickMultiplier.x : mouseInput.x;
        mouseInput.y = rightStickInput.y != 0 ? rightStickInput.y * RightStickMultiplier.y : mouseInput.y;

        bool jumpInput = Input.GetButtonDown("Jump");
        bool ropeInput = Input.GetMouseButtonDown(0);

        if (!ropeInput)
        {
            ropeInput = Input.GetKeyDown(KeyCode.F);
        }

        bool cancelRopeInput = Input.GetMouseButtonUp(0);

        if (!cancelRopeInput)
        {
            cancelRopeInput = Input.GetKeyUp(KeyCode.F);
        }

        bool sprintInput = Input.GetButton("Sprint");

        Current = new PlayerInput()
        {
            MoveInput = moveInput,
            MouseInput = mouseInput,
            JumpInput = jumpInput,
            RopeInput = ropeInput,
            CancelRopeInput = cancelRopeInput,
            SprintInput = sprintInput
        };

        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (Input.GetKey(KeyCode.Backspace))
        {
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        }

    }
}

public struct PlayerInput
{
    public Vector3 MoveInput;
    public Vector2 MouseInput;
    public bool JumpInput;
    public bool SprintInput;
    public bool RopeInput;
    public bool CancelRopeInput;
}
