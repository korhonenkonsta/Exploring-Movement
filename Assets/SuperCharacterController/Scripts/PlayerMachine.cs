using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SuperCharacterController))]
[RequireComponent(typeof(PlayerInputController))]

public class PlayerMachine : SuperStateMachine {

    List<Vector3> lastLocalMoves = new List<Vector3>();
    List<Vector3> lastMoveInputs = new List<Vector3>();

    public Transform AnimatedMesh;

    public float WalkSpeed = 5.0f;
    public float originalWalkspeed = 5.0f;
    public float walkSpeedAtJump;
    public float extraWallRunSpeed;
    public float extraWallRunJumpAcceleration;
    public float momentum;

    public float WalkAcceleration = 30.0f;
    public float originalWalkAcceleration = 30.0f;

    public float JumpAcceleration = 20.0f;
    public float originalJumpAcceleration;
    public float reducedJumpAcceleration;
    public float JumpHeight = 5.0f;
    public float Gravity = 25.0f;
    public float originalGravity;
    public float wallRunGravity;
    public float tetherLength;
    public float originalTetherLength;
    public float desiredTetherLength;
    public float maximumTetherLength = 100000;
    public float h;
    public float timeTethered;

    public float reducedTime;
    public float timeInCurrentState;

    public int jumpCount;
    public int maxJumpCount;

    public bool canWallJump;
    public bool canWallRun;
    public bool canGrapple;

    public bool isWallJumping;
    public bool isWallRunning;
    public bool isTethered;
    public bool tetherMomentumBoostDone;

    // Add more states by comma separating them
    enum PlayerStates { Idle, Walk, Jump, Fall, Sprint }

    private SuperCharacterController controller;
    public SuperCollision previousFirstCollision;

    // current velocity
    private Vector3 moveDirection;
    // current direction our character's art is facing
    public Vector3 lookDirection { get; private set; }

    public Vector3 previousPlanarMovedirection;
    public Vector3 previousNonZeroLocalMovement;
    public Vector3 previousNonZeroAverageLocalMovement;
    public Vector3 collisionNormal;
    public Vector3 collisionPoint;
    public Vector3 tetherPoint;

    public LineRenderer ropeRender;
    public Material ropeMaterial;

    private PlayerInputController input;

	void Start ()
    {
        Cursor.visible = false;
        input = gameObject.GetComponent<PlayerInputController>();

        // Grab the controller object from our object
        controller = gameObject.GetComponent<SuperCharacterController>();
		
		// Our character's current facing direction, planar to the ground
        lookDirection = transform.forward;

        // Set our currentState to idle on startup
        currentState = PlayerStates.Idle;

        originalWalkspeed = WalkSpeed;
        extraWallRunSpeed = WalkSpeed * 1.75f;
        extraWallRunJumpAcceleration = JumpAcceleration * 2f;
        originalWalkAcceleration = WalkAcceleration;
        originalJumpAcceleration = JumpAcceleration;
        reducedJumpAcceleration = JumpAcceleration / 10f;
        originalGravity = Gravity;
        wallRunGravity = originalGravity / 2;
	}

    private void CreateAndDeleteRope()
    {
        if (canGrapple && input.Current.RopeInput && !isTethered)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));

            if (Physics.Raycast(ray, out hit))
            {
                isTethered = true;
                timeTethered = 0f;
                tetherMomentumBoostDone = false;
                tetherPoint = hit.point;
                tetherLength = Vector3.Distance(hit.point, transform.position);
                originalTetherLength = tetherLength;

                h = hit.point.y - transform.position.y;

                // Set rope lenght
                if (h > 0)
                {
                    desiredTetherLength = h - 0.5f;
                }
                else
                {
                    desiredTetherLength = originalTetherLength / 2f;
                }

                if (desiredTetherLength <= originalTetherLength / 3f)
                {
                    desiredTetherLength = originalTetherLength / 3f;
                }

                print("tetherPoint: " + isTethered + " tetherpoint: " + tetherPoint + " tetherlenght: " + tetherLength);

                // Rope graphics
                if (ropeRender)
                {
                    Destroy(ropeRender);
                }

                ropeRender = gameObject.AddComponent<LineRenderer>();
                ropeRender.material = ropeMaterial;
                ropeRender.widthMultiplier = 0.05f;

                ropeRender.SetPosition(0, AnimatedMesh.position);
                ropeRender.SetPosition(1, hit.point);
                ropeRender.useWorldSpace = true;

                momentum += 1f;
            }
        }

        if (input.Current.CancelRopeInput)
        {
            isTethered = false;

            if (ropeRender)
            {
                Destroy(ropeRender);
            }
        }
    }

    protected override void EarlyGlobalSuperUpdate()
    {
		// Rotate out facing direction horizontally based on mouse input
        // (Taking into account that this method may be called multiple times per frame)
        lookDirection = Quaternion.AngleAxis(input.Current.MouseInput.x * (controller.deltaTime / Time.deltaTime), controller.up) * lookDirection;

        // Put any code in here you want to run BEFORE the state's update function.
        // This is run regardless of what state you're in
        if (reducedTime > 0)
        {
            reducedTime -= Time.deltaTime;
        }

        timeInCurrentState += Time.deltaTime;
    }

    protected override void LateGlobalSuperUpdate()
    {
        // Put any code in here you want to run AFTER the state's update function.
        // This is run regardless of what state you're in

        //Debug.DrawLine(transform.position, moveDirection, Color.red);

        if (isTethered)
        {
            timeTethered += Time.deltaTime;
            print(momentum);

            if (momentum > 0) //AND PREVIOUS POS IS LOWER THAN CURRENT (FAKE GRAVITY)
            {
                momentum -= 0.01f; //TEMP
            }

            if (momentum < 0)
            {
                momentum = 0;
            }

            if (tetherLength > desiredTetherLength * 1.5f)
            {
                tetherLength -= 0.15f; //FUN VALUE
            }
            else if (tetherLength > desiredTetherLength)
            {
                tetherLength -= (0.01f + 0.30f * (1 - desiredTetherLength / tetherLength));
                momentum += 0.1f * (1f - momentum/20f); //FUN VALUE
            }
            else
            {
                //print("DESIRED LENGTH REACHED");
                if (tetherMomentumBoostDone == false)
                {
                    tetherMomentumBoostDone = true;
                }
            }

            Vector3 testPosition = transform.position + moveDirection * controller.deltaTime;

            // Keep character at end of rope
            if ((testPosition - tetherPoint).magnitude > tetherLength)
            {
                testPosition = tetherPoint + (testPosition - tetherPoint).normalized * tetherLength;
            }

            moveDirection = (testPosition - transform.position) / controller.deltaTime;
            moveDirection -= controller.up * Gravity * controller.deltaTime;
            // Slow down
            moveDirection = Vector3.MoveTowards(moveDirection, new Vector3(0, -1, 0), (20f + timeTethered * 1.5f) * controller.deltaTime);

            transform.position = testPosition;
            
            Vector3 myUp = (tetherPoint - AnimatedMesh.position);
            AnimatedMesh.LookAt(tetherPoint, AnimatedMesh.up);
            AnimatedMesh.Rotate(-90, 0, 0);

            if (ropeRender)
            {
                ropeRender.SetPosition(0, transform.position);
                ropeRender.SetPosition(0, AnimatedMesh.position);
            }
        }
        else
        {
            // Move the player by our velocity every frame
            transform.position += moveDirection * controller.deltaTime;
            // Rotate our mesh to face where we are "looking"
            AnimatedMesh.rotation = Quaternion.LookRotation(lookDirection, controller.up);
        }
    }

    private bool AcquiringGround()
    {
        return controller.currentGround.IsGrounded(false, 0.01f);
    }

    private bool MaintainingGround()
    {
        return controller.currentGround.IsGrounded(true, 0.5f);//Test values
    }

    public void RotateGravity(Vector3 up)
    {
        lookDirection = Quaternion.FromToRotation(transform.up, up) * lookDirection;
    }

    /// <summary>
    /// Constructs a vector representing our movement local to our lookDirection, which is
    /// controlled by the camera
    /// </summary>
    private Vector3 LocalMovement()
    {
        Vector3 right = Vector3.Cross(controller.up, lookDirection);

        Vector3 local = Vector3.zero;

        if (input.Current.MoveInput.x != 0)
        {
            local += right * input.Current.MoveInput.x;
        }

        if (input.Current.MoveInput.z != 0)
        {
            local += lookDirection * input.Current.MoveInput.z;
        }

        return local.normalized;
    }

    // Calculate the initial velocity of a jump based off gravity and desired maximum height attained
    private float CalculateJumpSpeed(float jumpHeight, float gravity)
    {
        return Mathf.Sqrt(jumpHeight * gravity);
    }

	/*void Update () {
	 * Update is normally run once on every frame update. We won't be using it
     * in this case, since the SuperCharacterController component sends a callback Update 
     * called SuperUpdate. SuperUpdate is recieved by the SuperStateMachine, and then fires
     * further callbacks depending on the state
	}*/

    // Below are state functions. Each one is called based on the name of the state,
    // so when currentState = Idle, we call Idle_EnterState. If currentState = Jump, we call
    // Jump_SuperUpdate()

    void Idle_EnterState()
    {
        timeInCurrentState = 0;
        controller.EnableSlopeLimit();
        controller.DisableClamping();
    }

    void Idle_SuperUpdate()
    {
        if (momentum > 0)
        {
            momentum -= 0.1f;

            momentum -= timeInCurrentState * 2;
        }

        if (momentum < 0)
        {
            momentum = 0;
        }

        jumpCount = 0;
        Gravity = originalGravity;
        isWallRunning = false;
        JumpAcceleration = originalJumpAcceleration;
        
        if (LocalMovement().magnitude > 0)
        {
            WalkSpeed = originalWalkspeed;
        }
        else
        {
            WalkSpeed = 0;
        }

        //Rope
        CreateAndDeleteRope();

        if (input.Current.JumpInput && jumpCount < maxJumpCount)
        {
            currentState = PlayerStates.Jump;
            return;
        }

        if (!MaintainingGround())
        {
            currentState = PlayerStates.Fall;
            return;
        }

        if (input.Current.MoveInput != Vector3.zero)
        {
            if (input.Current.SprintInput)
            {
                currentState = PlayerStates.Sprint;
                return;
            }

            currentState = PlayerStates.Walk;
            return;
        }

        // Apply friction
        moveDirection = Vector3.MoveTowards(moveDirection, Vector3.zero, (30.0f + originalWalkspeed) * controller.deltaTime);
    }

    void Idle_ExitState()
    {
        // Run once when we exit the idle state
    }

    void Walk_EnterState()
    {
        timeInCurrentState = 0;
    }

    void Walk_SuperUpdate()
    {
        if (momentum > 0)
        {
            momentum -= 0.1f;

            momentum -= timeInCurrentState * 2;
        }

        if (momentum < 0)
        {
            momentum = 0;
        }

        jumpCount = 0;
        WalkSpeed = originalWalkspeed;
        WalkAcceleration = originalWalkAcceleration;
        JumpAcceleration = originalJumpAcceleration;

        //Rope
        CreateAndDeleteRope();

        if (input.Current.JumpInput && jumpCount < maxJumpCount)
        {
            currentState = PlayerStates.Jump;
            return;
        }

        if (!MaintainingGround())
        {
            currentState = PlayerStates.Fall;
            return;
        }

        if (input.Current.SprintInput)
        {
            currentState = PlayerStates.Sprint;
            return;
        }

        if (input.Current.MoveInput != Vector3.zero)
        {
            moveDirection = Vector3.MoveTowards(moveDirection, LocalMovement() * WalkSpeed, WalkAcceleration * controller.deltaTime);
        }
        else
        {
            currentState = PlayerStates.Idle;
            return;
        }
    }

    void Sprint_EnterState()
    {
        timeInCurrentState = 0;
    }

    void Sprint_SuperUpdate()
    {
        if (momentum > 0)
        {
            momentum -= 0.1f;

            momentum -= timeInCurrentState * 2;
        }

        if (momentum < 0)
        {
            momentum = 0;
        }

        jumpCount = 0;
        WalkSpeed = originalWalkspeed * 1.4f;
        WalkAcceleration = originalWalkAcceleration * 1.4f;
        JumpAcceleration = originalJumpAcceleration;

        //Rope
        CreateAndDeleteRope();

        if (input.Current.JumpInput && jumpCount < maxJumpCount)
        {
            currentState = PlayerStates.Jump;
            return;
        }

        if (!MaintainingGround())
        {
            currentState = PlayerStates.Fall;
            return;
        }

        if (input.Current.MoveInput != Vector3.zero)
        {
            if (input.Current.SprintInput)
            {
                moveDirection = Vector3.MoveTowards(moveDirection, LocalMovement() * WalkSpeed, WalkAcceleration * controller.deltaTime);
            }
            else
            {
                currentState = PlayerStates.Walk;
            }
        }
        else
        {
            currentState = PlayerStates.Idle;
            return;
        }
    }

    void Jump_EnterState()
    {
        jumpCount++;

        //Maybe another way to implement this?
        momentum += 0.25f;
        print("momentum: " + momentum);

        controller.DisableClamping();
        controller.DisableSlopeLimit();
        
        walkSpeedAtJump = WalkSpeed;

        //Walljump
        print("iswalljumping" + isWallJumping + "colliding: " + controller.collisionData.Count);

        if (isWallJumping && controller.collisionData.Count > 0)
        {
            //May want to separate horizontal and vertical components for better control

            //If planar speed is low, inrease it to a minimum
            if (Vector3.ProjectOnPlane(moveDirection, Vector3.up).magnitude < 20)
            {
                moveDirection = Vector3.ProjectOnPlane(moveDirection, Vector3.up).normalized * (originalWalkspeed + extraWallRunSpeed/2) + new Vector3(0, moveDirection.y, 0);
                JumpAcceleration = reducedJumpAcceleration;
                reducedTime = 0.4f;
            }

            //Direction of jump
            moveDirection = Vector3.Reflect(moveDirection, controller.collisionData[0].normal);
            float angle = Vector3.Angle(Math3d.ProjectVectorOnPlane(controller.up, moveDirection), controller.collisionData[0].normal);
            print("angle between movedirection and wall normal: " + angle);

            if (angle > 40f)
            {
                Vector3 tempMoveDirection = moveDirection;
                moveDirection = Quaternion.AngleAxis((angle - 40f), Vector3.up) * moveDirection;
                float  newAngle = Vector3.Angle(Math3d.ProjectVectorOnPlane(controller.up, moveDirection), controller.collisionData[0].normal);

                if (newAngle > 40)
                {
                    tempMoveDirection = Quaternion.AngleAxis(-(angle - 40f), Vector3.up) * tempMoveDirection;
                    moveDirection = tempMoveDirection;
                    newAngle = Vector3.Angle(Math3d.ProjectVectorOnPlane(controller.up, moveDirection), controller.collisionData[0].normal);
                }
            }

            if (moveDirection.y < 0)
            {
                moveDirection.y = 0;
            }

            //Don't walljump too high
            if (moveDirection.y > 1)
            {
                moveDirection += controller.up * CalculateJumpSpeed(JumpHeight, Gravity) / (moveDirection.y);
            }
            else
            {
                moveDirection += controller.up * CalculateJumpSpeed(JumpHeight, Gravity) / 1.6f;
            }

            previousNonZeroLocalMovement = Vector3.ProjectOnPlane(moveDirection, Vector3.up).normalized;

            jumpCount = 1;
            isWallJumping = false;
        }
        else
        //Regular jump
        {
            moveDirection += controller.up * CalculateJumpSpeed(JumpHeight, Gravity);
        }
    }

    void Jump_SuperUpdate()
    {
        //TODO: 
        //Input interpreting
        //Take camera position into account when determining if wall run shoud start?
        //Bumbing off the ceiling
        //Is the extra frame/s for wallrunning needed?

        //Rope
        CreateAndDeleteRope();

        //For reducing falsely interpreted input
        lastMoveInputs.Add(input.Current.MoveInput);
        //Seems to be always the last two frames

        if (lastMoveInputs.Count > 10)
        {
            lastMoveInputs.RemoveAt(0);
        }

        Vector3 sum = Vector3.zero;
        Vector3 average = Vector3.zero;

        foreach (Vector3 v in lastMoveInputs)
        {
            sum += v;
        }

        average = sum / lastMoveInputs.Count;

        if (lastMoveInputs[lastMoveInputs.Count - 1].magnitude == 0)//Edit this to check final inputs to see if they differ from intended
        {
            average = Vector3.zero;
        }

        lastLocalMoves.Add(LocalMovement());

        if (lastLocalMoves.Count > 10)
        {
            lastLocalMoves.RemoveAt(0);
        }

        Vector3 sum2 = Vector3.zero;
        Vector3 average2 = Vector3.zero;

        foreach (Vector3 v in lastLocalMoves)
        {
            sum2 += v;
        }

        average2 = sum / lastLocalMoves.Count;

        if (lastLocalMoves[lastLocalMoves.Count-1].magnitude == 0)//Edit this to check final inputs to see if they differ from intended
        {
            average2 = Vector3.zero;
        }

        //Horizontal and vertical components
        Vector3 planarMoveDirection = Math3d.ProjectVectorOnPlane(controller.up, moveDirection);
        Vector3 verticalMoveDirection = moveDirection - planarMoveDirection;

        //Are we touching anything?
        if (controller.collisionData.Count > 0 || previousFirstCollision.gameObject != null)
        {
            if (controller.collisionData.Count > 0)
            {
                collisionNormal = controller.collisionData[0].normal;
                collisionPoint = controller.collisionData[0].point;
            }
            else
            {
                collisionNormal = previousFirstCollision.normal;
                collisionPoint = previousFirstCollision.point;
            }

            if (previousFirstCollision.gameObject)
            print(controller.collisionData.Count + "and" + previousFirstCollision.gameObject.name);

            if (controller.collisionData.Count > 1)
            {
                //print("how many colliders? " + controller.collisionData.Count);

                //foreach (SuperCollision col in controller.collisionData)
                //{
                //    print("type: "+col.superCollisionType+ "object: "+ col.gameObject + "normal: "+ col.normal);
                //}
            }

            //Walljump
            if (input.Current.JumpInput && jumpCount <= maxJumpCount && canWallJump)
            {
                isWallJumping = true;
                Gravity = originalGravity;
                WalkSpeed = originalWalkspeed;

                if (walkSpeedAtJump > originalWalkspeed)
                {
                    WalkSpeed = walkSpeedAtJump;
                }

                if (reducedTime > 0)
                {
                    print("does this even");
                    JumpAcceleration = reducedJumpAcceleration;
                }
                else
                {
                    JumpAcceleration = originalJumpAcceleration;
                }

                currentState = PlayerStates.Jump;
                return;
            }

            float steepAngle = Vector3.Angle(Vector3.up, collisionNormal);
            float jumpAngle = Vector3.Angle(planarMoveDirection, collisionNormal);

            print("steepangle: " + steepAngle);

            //Wallrun
            if (110f >= steepAngle && steepAngle >= 55.1f && jumpAngle <= 155f && canWallRun)
            {
                if (isWallRunning == false)
                {
                    print("wallrun started at" + Time.timeSinceLevelLoad);
                    if (verticalMoveDirection.y < 0)
                    {
                        verticalMoveDirection.y = 4;
                    }

                    jumpCount = 1;
                    planarMoveDirection *= 2f;
                }

                isWallRunning = true;
                Gravity = 0;
            }

            if (steepAngle < 15f)
            {
                print("steepangle < 15f");
                WalkSpeed = 0;
                moveDirection = planarMoveDirection;
                previousPlanarMovedirection = Vector3.zero;
                currentState = PlayerStates.Idle;
                return;
            }

            if (steepAngle > 160f)
            {
                print("steepangle > 160f");
                WalkSpeed = 0;
                moveDirection = planarMoveDirection;
                previousPlanarMovedirection = Vector3.zero;
                currentState = PlayerStates.Fall;
                return;
            }
        }
        else
        {
            if (isWallRunning)
            {
                print("stopped wallrun");
                planarMoveDirection *= 0.5f;
            }

            isWallRunning = false;
            Gravity = originalGravity;
            WalkSpeed = originalWalkspeed + momentum;

            if (walkSpeedAtJump > originalWalkspeed)
            {
                WalkSpeed = walkSpeedAtJump + momentum;
            }

            if (reducedTime > 0)
            {
                JumpAcceleration = reducedJumpAcceleration;
            }
            else
            {
                JumpAcceleration = originalJumpAcceleration;
            }

            //Double jump and beyond
            if (input.Current.JumpInput && jumpCount < maxJumpCount)
            {
                if (moveDirection.y < 0)
                {
                    moveDirection.y = 0;
                }

                currentState = PlayerStates.Jump;
                return;
            }
        }

        if (Vector3.Angle(verticalMoveDirection, controller.up) > 90 && AcquiringGround())
        {
            moveDirection = planarMoveDirection;
            previousPlanarMovedirection = Vector3.zero;
            currentState = PlayerStates.Idle;
            return;            
        }

        //Wallrun speed
        if (isWallRunning)
        {
            WalkSpeed = originalWalkspeed + extraWallRunSpeed + momentum;
            JumpAcceleration = originalJumpAcceleration + extraWallRunJumpAcceleration;
        }
        else
        {
            WalkSpeed = originalWalkspeed + momentum;

            if (walkSpeedAtJump > originalWalkspeed || (walkSpeedAtJump > 0 && walkSpeedAtJump < originalWalkspeed))
            {
                WalkSpeed = walkSpeedAtJump + momentum;
            }

            JumpAcceleration = originalJumpAcceleration;

            //IF WE WANT TO STOP FAST IN THE AIR
            //if (LocalMovement().magnitude == 0)
            //{
            //    WalkSpeed = walkSpeedAtJump + momentum;
            //}
        }

        //Do we have LocalMovement caused by input etc
        if (LocalMovement().magnitude > 0)
        {
            //WalkSpeed = originalWalkspeed;
            if (isWallRunning)
            {
                //WalkSpeed = originalWalkspeed + extraWallRunSpeed * 2;
                //JumpAcceleration = originalJumpAcceleration * 3;
            }
            else
            {
                WalkSpeed = originalWalkspeed + momentum;

                if (walkSpeedAtJump > originalWalkspeed || (walkSpeedAtJump > 0 && walkSpeedAtJump < originalWalkspeed))
                {
                    WalkSpeed = walkSpeedAtJump;
                }

                if (reducedTime > 0)
                {
                    JumpAcceleration = reducedJumpAcceleration;
                }
                else
                {
                    JumpAcceleration = originalJumpAcceleration;
                }
            }

            if (walkSpeedAtJump > originalWalkspeed)
            {
                WalkSpeed = walkSpeedAtJump + momentum;
            }

            if (isWallRunning == false)
            {
                planarMoveDirection = Vector3.MoveTowards(planarMoveDirection, LocalMovement() * WalkSpeed, JumpAcceleration * controller.deltaTime);
            }
        }
        else
        {
            if (isWallRunning == false)
            {
                planarMoveDirection = Vector3.MoveTowards(planarMoveDirection, planarMoveDirection.normalized * WalkSpeed, JumpAcceleration * controller.deltaTime);
            }
            
            if (controller.collisionData.Count == 0)
            {
                if (WalkSpeed - WalkSpeed * 0.01f > 0)
                {
                    WalkSpeed -= WalkSpeed * 0.01f;
                }
            }
        }

        if (isWallRunning)
        {
            WalkSpeed = originalWalkspeed + extraWallRunSpeed + momentum;
            JumpAcceleration = originalJumpAcceleration * 2;

            float angle = Vector3.Angle(planarMoveDirection, collisionNormal);
            print("angle: " + angle);

            //Wallrun direction
            if (angle >= 90f)
            {
                Vector3 tempPlanarMoveDirection = Math3d.ProjectVectorOnPlane(controller.up, collisionPoint) + (planarMoveDirection * WalkSpeed - Math3d.ProjectVectorOnPlane(controller.up, collisionPoint)) - Math3d.ProjectVectorOnPlane(controller.up, collisionNormal) * Vector3.Dot((planarMoveDirection * WalkSpeed - Math3d.ProjectVectorOnPlane(controller.up, collisionPoint)), Math3d.ProjectVectorOnPlane(controller.up, collisionNormal));
                angle = Vector3.Angle(tempPlanarMoveDirection, collisionNormal);

                print("temp planar dir: " + tempPlanarMoveDirection + " normal: " + collisionNormal + " angle after: " + angle);

                if (angle >= 90f)
                {
                    tempPlanarMoveDirection = Quaternion.AngleAxis(-(angle - 90f - 1f), Vector3.up) * tempPlanarMoveDirection;

                    float angle2 = angle;

                    angle = Vector3.Angle(tempPlanarMoveDirection, collisionNormal);

                    print("angle if over or equal 90: " + angle);

                    if (angle >= 91.9f)
                    {
                        tempPlanarMoveDirection = Quaternion.AngleAxis(((angle2 - 90f) * 2f - 2f), Vector3.up) * tempPlanarMoveDirection;
                        angle = Vector3.Angle(tempPlanarMoveDirection, collisionNormal);
                    }
                }
                else
                {
                    tempPlanarMoveDirection = Quaternion.AngleAxis((90f - angle + 1f), Vector3.up) * tempPlanarMoveDirection;

                    float angle2 = angle;

                    angle = Vector3.Angle(tempPlanarMoveDirection, collisionNormal);
                    
                    print("angle if under 90: " + angle);

                    if (angle <= 90f)
                    {
                        tempPlanarMoveDirection = Quaternion.AngleAxis(-((90f - angle2) * 2f + 2f), Vector3.up) * tempPlanarMoveDirection;
                        angle = Vector3.Angle(tempPlanarMoveDirection, collisionNormal);
                    }
                }

                planarMoveDirection = Vector3.MoveTowards(planarMoveDirection, tempPlanarMoveDirection.normalized * WalkSpeed, JumpAcceleration * controller.deltaTime);
            }
            else
            {
                planarMoveDirection = Vector3.MoveTowards(planarMoveDirection, planarMoveDirection.normalized * WalkSpeed, JumpAcceleration * controller.deltaTime);
            }

            verticalMoveDirection -= controller.up * wallRunGravity * controller.deltaTime;
        }
        else
        {
            verticalMoveDirection -= controller.up * Gravity * controller.deltaTime;
        }

        moveDirection = planarMoveDirection + verticalMoveDirection;

        if (LocalMovement().magnitude > 0)
        {
            previousNonZeroLocalMovement = LocalMovement();
        }

        previousNonZeroAverageLocalMovement = average2;

        previousPlanarMovedirection = planarMoveDirection;

        if (controller.collisionData.Count > 0)
        {
            previousFirstCollision = controller.collisionData[0];
        }
        else
        {
            if (previousFirstCollision.gameObject)
            {
                previousFirstCollision.gameObject = null;
            }
        }
    }

    void Fall_EnterState()
    {
        controller.DisableClamping();
        controller.DisableSlopeLimit();

        print("falling");
        // moveDirection = trueVelocity;
    }

    void Fall_SuperUpdate()
    {
        //TODO: 
        //Input interpreting
        //Take camera position into account when determining if wall run shoud start?
        //Bumbing off the ceiling
        //Is the extra frame/s for wallrunning needed?

        //Rope
        CreateAndDeleteRope();

        //For reducing falsely interpreted input
        lastMoveInputs.Add(input.Current.MoveInput);
        //Seems to be always the last two frames

        if (lastMoveInputs.Count > 10)
        {
            lastMoveInputs.RemoveAt(0);
        }

        Vector3 sum = Vector3.zero;
        Vector3 average = Vector3.zero;

        foreach (Vector3 v in lastMoveInputs)
        {
            sum += v;
        }

        average = sum / lastMoveInputs.Count;

        if (lastMoveInputs[lastMoveInputs.Count - 1].magnitude == 0)//Edit this to check final inputs to see if they differ from intended
        {
            average = Vector3.zero;
        }

        lastLocalMoves.Add(LocalMovement());

        if (lastLocalMoves.Count > 10)
        {
            lastLocalMoves.RemoveAt(0);
        }

        Vector3 sum2 = Vector3.zero;
        Vector3 average2 = Vector3.zero;

        foreach (Vector3 v in lastLocalMoves)
        {
            sum2 += v;
        }

        average2 = sum / lastLocalMoves.Count;

        if (lastLocalMoves[lastLocalMoves.Count - 1].magnitude == 0)//Edit this to check final inputs to see if they differ from intended
        {
            average2 = Vector3.zero;
        }

        //Horizontal and vertical components
        Vector3 planarMoveDirection = Math3d.ProjectVectorOnPlane(controller.up, moveDirection);
        Vector3 verticalMoveDirection = moveDirection - planarMoveDirection;

        //Are we touching anything?
        if (controller.collisionData.Count > 0 || previousFirstCollision.gameObject != null)
        {
            if (controller.collisionData.Count > 0)
            {
                collisionNormal = controller.collisionData[0].normal;
                collisionPoint = controller.collisionData[0].point;
            }
            else
            {
                collisionNormal = previousFirstCollision.normal;
                collisionPoint = previousFirstCollision.point;
            }

            if (previousFirstCollision.gameObject)
                print(controller.collisionData.Count + "and" + previousFirstCollision.gameObject.name);

            if (controller.collisionData.Count > 1)
            {
                //print("how many colliders? " + controller.collisionData.Count);

                //foreach (SuperCollision col in controller.collisionData)
                //{
                //    print("type: "+col.superCollisionType+ "object: "+ col.gameObject + "normal: "+ col.normal);
                //}
            }

            //Walljump
            if (input.Current.JumpInput && jumpCount <= maxJumpCount && canWallJump)
            {
                isWallJumping = true;
                Gravity = originalGravity;
                WalkSpeed = originalWalkspeed;

                if (walkSpeedAtJump > originalWalkspeed)
                {
                    WalkSpeed = walkSpeedAtJump;
                }

                if (reducedTime > 0)
                {
                    print("does this even");
                    JumpAcceleration = reducedJumpAcceleration;
                }
                else
                {
                    JumpAcceleration = originalJumpAcceleration;
                }

                currentState = PlayerStates.Jump;
                return;
            }

            float steepAngle = Vector3.Angle(Vector3.up, collisionNormal);
            float jumpAngle = Vector3.Angle(planarMoveDirection, collisionNormal);

            print("steepangle: " + steepAngle);

            //Wallrun
            if (110f >= steepAngle && steepAngle >= 55.1f && jumpAngle <= 155f && canWallRun)
            {
                if (isWallRunning == false)
                {
                    print("wallrun started at" + Time.timeSinceLevelLoad);
                    if (verticalMoveDirection.y < 0)
                    {
                        verticalMoveDirection.y = 4;
                    }

                    jumpCount = 1;
                    planarMoveDirection *= 2f;
                }

                isWallRunning = true;
                Gravity = 0;
            }

            if (steepAngle < 15f)
            {
                print("steepangle < 15f");
                WalkSpeed = 0;
                moveDirection = planarMoveDirection;
                previousPlanarMovedirection = Vector3.zero;
                currentState = PlayerStates.Idle;
                return;
            }

            if (steepAngle > 160f)
            {
                print("steepangle > 160f");
                WalkSpeed = 0;
                moveDirection = planarMoveDirection;
                previousPlanarMovedirection = Vector3.zero;
                currentState = PlayerStates.Fall;
                return;
            }
        }
        else
        {
            if (isWallRunning)
            {
                print("stopped wallrun");
                planarMoveDirection *= 0.5f;
            }

            isWallRunning = false;
            Gravity = originalGravity;
            WalkSpeed = originalWalkspeed + momentum;

            if (walkSpeedAtJump > originalWalkspeed)
            {
                WalkSpeed = walkSpeedAtJump + momentum;
            }

            if (reducedTime > 0)
            {
                JumpAcceleration = reducedJumpAcceleration;
            }
            else
            {
                JumpAcceleration = originalJumpAcceleration;
            }

            //Double jump and beyond
            if (input.Current.JumpInput && jumpCount < maxJumpCount)
            {
                if (moveDirection.y < 0)
                {
                    moveDirection.y = 0;
                }

                currentState = PlayerStates.Jump;
                return;
            }
        }

        if (Vector3.Angle(verticalMoveDirection, controller.up) > 90 && AcquiringGround())
        {
            moveDirection = planarMoveDirection;
            previousPlanarMovedirection = Vector3.zero;
            currentState = PlayerStates.Idle;
            return;
        }

        //Wallrun speed
        if (isWallRunning)
        {
            WalkSpeed = originalWalkspeed + extraWallRunSpeed + momentum;
            JumpAcceleration = originalJumpAcceleration + extraWallRunJumpAcceleration;
        }
        else
        {
            WalkSpeed = originalWalkspeed + momentum;

            if (walkSpeedAtJump > originalWalkspeed || (walkSpeedAtJump > 0 && walkSpeedAtJump < originalWalkspeed))
            {
                WalkSpeed = walkSpeedAtJump + momentum;
            }

            JumpAcceleration = originalJumpAcceleration;

            //IF WE WANT TO STOP FAST IN THE AIR
            //if (LocalMovement().magnitude == 0)
            //{
            //    WalkSpeed = walkSpeedAtJump + momentum;
            //}
        }

        //Do we have LocalMovement caused by input etc
        if (LocalMovement().magnitude > 0)
        {
            //WalkSpeed = originalWalkspeed;
            if (isWallRunning)
            {
                //WalkSpeed = originalWalkspeed + extraWallRunSpeed * 2;
                //JumpAcceleration = originalJumpAcceleration * 3;
            }
            else
            {
                WalkSpeed = originalWalkspeed + momentum;

                if (walkSpeedAtJump > originalWalkspeed || (walkSpeedAtJump > 0 && walkSpeedAtJump < originalWalkspeed))
                {
                    WalkSpeed = walkSpeedAtJump;
                }

                if (reducedTime > 0)
                {
                    JumpAcceleration = reducedJumpAcceleration;
                }
                else
                {
                    JumpAcceleration = originalJumpAcceleration;
                }
            }

            if (walkSpeedAtJump > originalWalkspeed)
            {
                WalkSpeed = walkSpeedAtJump + momentum;
            }

            if (isWallRunning == false)
            {
                planarMoveDirection = Vector3.MoveTowards(planarMoveDirection, LocalMovement() * WalkSpeed, JumpAcceleration * controller.deltaTime);
            }
        }
        else
        {
            if (isWallRunning == false)
            {
                planarMoveDirection = Vector3.MoveTowards(planarMoveDirection, planarMoveDirection.normalized * WalkSpeed, JumpAcceleration * controller.deltaTime);
            }

            if (controller.collisionData.Count == 0)
            {
                if (WalkSpeed - WalkSpeed * 0.01f > 0)
                {
                    WalkSpeed -= WalkSpeed * 0.01f;
                }
            }
        }

        if (isWallRunning)
        {
            WalkSpeed = originalWalkspeed + extraWallRunSpeed + momentum;
            JumpAcceleration = originalJumpAcceleration * 2;

            float angle = Vector3.Angle(planarMoveDirection, collisionNormal);
            print("angle: " + angle);

            //Wallrun direction
            if (angle >= 90f)
            {
                Vector3 tempPlanarMoveDirection = Math3d.ProjectVectorOnPlane(controller.up, collisionPoint) + (planarMoveDirection * WalkSpeed - Math3d.ProjectVectorOnPlane(controller.up, collisionPoint)) - Math3d.ProjectVectorOnPlane(controller.up, collisionNormal) * Vector3.Dot((planarMoveDirection * WalkSpeed - Math3d.ProjectVectorOnPlane(controller.up, collisionPoint)), Math3d.ProjectVectorOnPlane(controller.up, collisionNormal));
                angle = Vector3.Angle(tempPlanarMoveDirection, collisionNormal);

                print("temp planar dir: " + tempPlanarMoveDirection + " normal: " + collisionNormal + " angle after: " + angle);

                if (angle >= 90f)
                {
                    tempPlanarMoveDirection = Quaternion.AngleAxis(-(angle - 90f - 1f), Vector3.up) * tempPlanarMoveDirection;

                    float angle2 = angle;

                    angle = Vector3.Angle(tempPlanarMoveDirection, collisionNormal);

                    print("angle if over or equal 90: " + angle);

                    if (angle >= 91.9f)
                    {
                        tempPlanarMoveDirection = Quaternion.AngleAxis(((angle2 - 90f) * 2f - 2f), Vector3.up) * tempPlanarMoveDirection;
                        angle = Vector3.Angle(tempPlanarMoveDirection, collisionNormal);
                    }
                }
                else
                {
                    tempPlanarMoveDirection = Quaternion.AngleAxis((90f - angle + 1f), Vector3.up) * tempPlanarMoveDirection;

                    float angle2 = angle;

                    angle = Vector3.Angle(tempPlanarMoveDirection, collisionNormal);

                    print("angle if under 90: " + angle);

                    if (angle <= 90f)
                    {
                        tempPlanarMoveDirection = Quaternion.AngleAxis(-((90f - angle2) * 2f + 2f), Vector3.up) * tempPlanarMoveDirection;
                        angle = Vector3.Angle(tempPlanarMoveDirection, collisionNormal);
                    }
                }

                planarMoveDirection = Vector3.MoveTowards(planarMoveDirection, tempPlanarMoveDirection.normalized * WalkSpeed, JumpAcceleration * controller.deltaTime);
            }
            else
            {
                planarMoveDirection = Vector3.MoveTowards(planarMoveDirection, planarMoveDirection.normalized * WalkSpeed, JumpAcceleration * controller.deltaTime);
            }

            verticalMoveDirection -= controller.up * wallRunGravity * controller.deltaTime;
        }
        else
        {
            verticalMoveDirection -= controller.up * Gravity * controller.deltaTime;
        }

        moveDirection = planarMoveDirection + verticalMoveDirection;

        if (LocalMovement().magnitude > 0)
        {
            previousNonZeroLocalMovement = LocalMovement();
        }

        previousNonZeroAverageLocalMovement = average2;

        previousPlanarMovedirection = planarMoveDirection;

        if (controller.collisionData.Count > 0)
        {
            previousFirstCollision = controller.collisionData[0];
        }
        else
        {
            if (previousFirstCollision.gameObject)
            {
                previousFirstCollision.gameObject = null;
            }
        }
    }
}
