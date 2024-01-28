using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour {

    public float walkSpeed;
    public float sprintSpeed;
    public float jumpForce;

    private bool isGrounded;
    private bool isSprinting;
    private Vector2Int currentChunk;

    private Transform cam;
    private Planet planet;

    private float mouseSensitivity = 3f;
    private float playerWidth = .25f;

    private float horizontal;
    private float vertical;
    private float rotationY = 0f;
    private float rotationX = 0f;

    [SerializeField]
    private Vector3 velocity;
    private Vector3 currentPosition;

    private float verticalMomentum = 0;
    private bool jumpRequest = false;

    private TextMeshProUGUI playerBlockCoordsUI;
    private TextMeshProUGUI playerChunkCoordsUI;

    private void Start() {
        try {

            cam = GameObject.Find("Main Camera").transform;
            planet = GameObject.Find("Planet").GetComponent<Planet>();
            playerBlockCoordsUI = GameObject.Find("PlayerBlockCoords").GetComponent<TextMeshProUGUI>();
            playerChunkCoordsUI = GameObject.Find("PlayerChunkCoords").GetComponent<TextMeshProUGUI>();

            currentPosition = transform.position;
            currentChunk = HelperFunctions.GetChunkPosFromBlockPos(Mathf.FloorToInt(currentPosition.x), Mathf.FloorToInt(currentPosition.y), Mathf.FloorToInt(currentPosition.z));

        } catch (System.Exception ex) {
            Debug.LogError("Error in Player Start: " + ex.Message);
        }
    }

    private void FixedUpdate() {
        try {

            CalculateVelocity();
            if (jumpRequest) {
                Jump();
            }

            transform.eulerAngles = new Vector3(transform.eulerAngles.x, rotationY, transform.eulerAngles.z);
            cam.transform.eulerAngles = new Vector3(rotationX, cam.transform.eulerAngles.y, cam.transform.eulerAngles.z);

            transform.position = currentPosition;

            Vector2Int newChunk = HelperFunctions.GetChunkPosFromBlockPos(Mathf.FloorToInt(currentPosition.x), Mathf.FloorToInt(currentPosition.y), Mathf.FloorToInt(currentPosition.z));

            if (newChunk.x != currentChunk.x || newChunk.y != currentChunk.y) {
                currentChunk = newChunk;
                this.planet.CheckRenderDistance(currentPosition);
            }

            this.planet.RenderChunks();

            playerBlockCoordsUI.text = currentPosition.ToString();
            playerChunkCoordsUI.text = currentChunk.ToString();

        } catch (System.Exception ex) {
            Debug.LogError("Error in Player FixedUpdate: " + ex.Message);
        }
    }

    private void Update() {
        try {

            GetPlayerInputs();

        } catch (System.Exception ex) {
            Debug.LogError("Error in Player Update: " + ex.Message);
        }
    }

    void Jump() {
        try {

            verticalMomentum = jumpForce;
            isGrounded = false;
            jumpRequest = false;

        } catch (System.Exception e) {
            Debug.LogError("Error in function Player.Jump: " + e.Message);
        }
    }

    private void CalculateVelocity() {
        try {

            //Affect vertical momentum with velocity
            if (verticalMomentum > this.planet.gravity) {
                verticalMomentum += Time.fixedDeltaTime * this.planet.gravity;
            }

            float speedModifier = isSprinting ? sprintSpeed : walkSpeed;
            velocity = speedModifier * Time.deltaTime * ((transform.forward * vertical) + (transform.right * horizontal));


            //Apply vertical momentum
            velocity += Time.fixedDeltaTime * verticalMomentum * Vector3.up;

            if (velocity.y < 0 && CheckBottom(velocity.y)) {
                velocity.y = 0;
            } else if (velocity.y > 0 && CheckTop(velocity.y)) {
                velocity.y = 0;
            }

            Vector3 nextPosition = this.currentPosition + velocity;

            if (isGrounded) {       
                nextPosition.y = Mathf.Clamp(nextPosition.y, nextPosition.y - 1, Mathf.Floor(transform.position.y));
            }

            //Check if running into any blocks in all four directions
            if (velocity.z > 0) {
                if (CheckFront()) {
                    velocity.z = 0;
                    nextPosition.z = Mathf.FloorToInt(nextPosition.z + playerWidth) - playerWidth;
                }
            } else if (velocity.z < 0) {
                if (CheckBack()) {
                    velocity.z = 0;
                    nextPosition.z = Mathf.CeilToInt(nextPosition.z - playerWidth) + playerWidth;
                }
            }

            if (velocity.x > 0) {
                if (CheckLeft()) {
                    velocity.x = 0;
                    nextPosition.x = Mathf.FloorToInt(nextPosition.x + playerWidth) - playerWidth;
                }
            } else if (velocity.x < 0) {
                if (CheckRight()) {
                    velocity.x = 0;
                    nextPosition.x = Mathf.CeilToInt(nextPosition.x - playerWidth) + playerWidth;
                }
            }

            this.currentPosition = nextPosition;

        } catch (System.Exception e) {
            Debug.LogError("Error in function Player.CalculateVelocity: " + e.Message);
        }
    }

    private void GetPlayerInputs() {
        try {

            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");

            rotationY += Input.GetAxis("Mouse X") * mouseSensitivity;
            rotationX -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            if (Input.GetButtonDown("Sprint")) {
                isSprinting = true;
            }

            if (Input.GetButtonUp("Sprint")) {
                isSprinting = false;
            }

            if (isGrounded && Input.GetButtonDown("Jump")) {
                jumpRequest = true;
            }

            if (Input.GetKeyDown(KeyCode.F1)) {
                SceneManager.LoadScene("SampleScene");
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function Player.GetPlayerInputs: " + e.Message);
        }
    }

    private bool CheckBottom(float downSpeed) {
        try {

            int y = Mathf.FloorToInt(transform.position.y + downSpeed);
            int minX = Mathf.FloorToInt(transform.position.x - playerWidth);
            int maxX = Mathf.FloorToInt(transform.position.x + playerWidth);
            int minZ = Mathf.FloorToInt(transform.position.z - playerWidth);
            int maxZ = Mathf.FloorToInt(transform.position.z + playerWidth);

            if (HelperFunctions.CheckBlockInPlanet(planet, minX, y, minZ) || HelperFunctions.CheckBlockInPlanet(planet, maxX, y, minZ) ||
                HelperFunctions.CheckBlockInPlanet(planet, maxX, y, maxZ) || HelperFunctions.CheckBlockInPlanet(planet, minX, y, maxZ)) {

                isGrounded = true;
                return true;

            } else {
                isGrounded = false;
                return false;
            }

        } catch (System.Exception ex) {
            Debug.LogError("Error in Player CheckBottom: " + ex.Message);
            return true;
        }
    }

    private bool CheckTop(float upSpeed) {
        try {

            int y = Mathf.FloorToInt(transform.position.y + 2f + upSpeed);
            int minX = Mathf.FloorToInt(transform.position.x - playerWidth);
            int maxX = Mathf.FloorToInt(transform.position.x + playerWidth);
            int minZ = Mathf.FloorToInt(transform.position.z - playerWidth);
            int maxZ = Mathf.FloorToInt(transform.position.z + playerWidth);

            if (HelperFunctions.CheckBlockInPlanet(planet, minX, y, minZ) || HelperFunctions.CheckBlockInPlanet(planet, maxX, y, minZ) || 
                HelperFunctions.CheckBlockInPlanet(planet, maxX, y, maxZ) || HelperFunctions.CheckBlockInPlanet(planet, minX, y, maxZ)) {
                return true;
            } else {
                return false;
            }

        } catch (System.Exception ex) {
            Debug.LogError("Error in Player CheckTop: " + ex.Message);
            return true;
        }
    }

    private bool CheckFront() {
        try {

            int x = Mathf.FloorToInt(currentPosition.x);
            int y = Mathf.FloorToInt(currentPosition.y);
            int z = Mathf.FloorToInt(currentPosition.z + velocity.z + playerWidth);

            return HelperFunctions.CheckBlockInPlanet(planet, x, y, z) || HelperFunctions.CheckBlockInPlanet(planet, x, y + 1, z);

        } catch (System.Exception ex) {
            Debug.LogError("Error in Player CheckTop: " + ex.Message);
            return true;
        }
    }

    private bool CheckBack() {
        try {

            int x = Mathf.FloorToInt(currentPosition.x);
            int y = Mathf.FloorToInt(currentPosition.y);
            int z = Mathf.FloorToInt(currentPosition.z + velocity.z - playerWidth);

            return HelperFunctions.CheckBlockInPlanet(planet, x, y, z) || HelperFunctions.CheckBlockInPlanet(planet, x, y + 1, z);

        } catch (System.Exception ex) {
            Debug.LogError("Error in Player CheckBack: " + ex.Message);
            return true;
        }
    }

    private bool CheckLeft() {
        try {

            int x = Mathf.FloorToInt(currentPosition.x + velocity.x + playerWidth);
            int y = Mathf.FloorToInt(currentPosition.y);
            int z = Mathf.FloorToInt(currentPosition.z);

            return HelperFunctions.CheckBlockInPlanet(planet, x, y, z) || HelperFunctions.CheckBlockInPlanet(planet, x, y + 1, z);

        } catch (System.Exception ex) {
            Debug.LogError("Error in Player CheckLeft: " + ex.Message);
            return true;
        }
    }

    private bool CheckRight() {
        try {

            int x = Mathf.FloorToInt(currentPosition.x + velocity.x - playerWidth);
            int y = Mathf.FloorToInt(currentPosition.y);
            int z = Mathf.FloorToInt(currentPosition.z);

            return HelperFunctions.CheckBlockInPlanet(planet, x, y, z) || HelperFunctions.CheckBlockInPlanet(planet, x, y + 1, z);

        } catch (System.Exception ex) {
            Debug.LogError("Error in Player CheckRight: " + ex.Message);
            return true;
        }
    }
}