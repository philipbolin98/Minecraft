using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour {

    public float walkSpeed = 8f;
    public float sprintSpeed = 16f;
    public float jumpForce = 10f;
    public float gravity = -9.8f;

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
    private Vector3 nextPosition;

    private float verticalMomentum = 0;
    private bool jumpRequest = false;

    private TextMeshProUGUI playerCoordsUI;

    private void Start() {
        try {

            cam = GameObject.Find("Main Camera").transform;
            planet = GameObject.Find("Planet").GetComponent<Planet>();
            playerCoordsUI = GameObject.Find("PlayerCoords").GetComponent<TextMeshProUGUI>();

            nextPosition = transform.position;
            currentChunk = HelperFunctions.GetChunkPosFromBlockPos(Mathf.FloorToInt(nextPosition.x), Mathf.FloorToInt(nextPosition.y), Mathf.FloorToInt(nextPosition.z));

        } catch (System.Exception e) {
            Debug.LogError("Error in function Player.Start: " + e.Message);
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

            transform.position = nextPosition;

            playerCoordsUI.text = nextPosition.ToString();

            Vector2Int nextChunk = HelperFunctions.GetChunkPosFromBlockPos(Mathf.FloorToInt(nextPosition.x), Mathf.FloorToInt(nextPosition.y), Mathf.FloorToInt(nextPosition.z));

            if (nextChunk.x != currentChunk.x || nextChunk.y != currentChunk.y) {
                currentChunk = nextChunk;
                this.planet.CheckRenderDistance(nextPosition);
            }

            this.planet.RenderChunks();

        } catch (System.Exception e) {
            Debug.LogError("Error in function Player.FixedUpdate: " + e.Message);
        }
    }

    private void Update() {
        try {

            GetPlayerInputs();

        } catch (System.Exception e) {
            Debug.LogError("Error in function Player.Update: " + e.Message);
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
            if (verticalMomentum > gravity) {
                verticalMomentum += Time.fixedDeltaTime * gravity;
            }

            if (isSprinting) {
                velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.deltaTime * sprintSpeed;
            } else {
                velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.deltaTime * walkSpeed;
            }

            //Apply vertical momentum
            velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

            if (velocity.y < 0 && CheckBottom(velocity.y)) {
                velocity.y = 0;
            } else if (velocity.y > 0 && CheckTop(velocity.y)) {
                velocity.y = 0;
            }

            nextPosition += velocity;

            if (isGrounded) {
                nextPosition.y = Mathf.Clamp(nextPosition.y, nextPosition.y - 1, Mathf.Floor(transform.position.y));
            }

            //Check if running into any blocks in all four directions
            if (velocity.z > 0 && CheckFront()) {
                velocity.z = 0;
                nextPosition.z = Mathf.Clamp(nextPosition.z, nextPosition.z - 1, Mathf.Ceil(nextPosition.z) - playerWidth);
            }

            if (velocity.z < 0 && CheckBack()) {
                velocity.z = 0;
                nextPosition.z = Mathf.Clamp(nextPosition.z, Mathf.Floor(nextPosition.z) + playerWidth, nextPosition.z + 1);
            }

            if (velocity.x > 0 && CheckRight()) {
                velocity.x = 0;
                nextPosition.x = Mathf.Clamp(nextPosition.x, nextPosition.x - 1, Mathf.Ceil(nextPosition.x) - playerWidth);
            }

            if (velocity.x < 0 && CheckLeft()) {
                velocity.x = 0;
                nextPosition.x = Mathf.Clamp(nextPosition.x, Mathf.Floor(nextPosition.x) + playerWidth, nextPosition.x + 1);
            }

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

            int x = Mathf.FloorToInt(nextPosition.x);
            int y = Mathf.FloorToInt(nextPosition.y);
            int z = Mathf.FloorToInt(nextPosition.z + playerWidth);

            return HelperFunctions.CheckBlockInPlanet(planet, x, y, z) || HelperFunctions.CheckBlockInPlanet(planet, x, y + 1, z);

        } catch (System.Exception ex) {
            Debug.LogError("Error in Player CheckTop: " + ex.Message);
            return true;
        }
    }

    private bool CheckBack() {
        try {

            int x = Mathf.FloorToInt(nextPosition.x);
            int y = Mathf.FloorToInt(nextPosition.y);
            int z = Mathf.FloorToInt(nextPosition.z - playerWidth);

            return HelperFunctions.CheckBlockInPlanet(planet, x, y, z) || HelperFunctions.CheckBlockInPlanet(planet, x, y + 1, z);

        } catch (System.Exception ex) {
            Debug.LogError("Error in Player CheckBack: " + ex.Message);
            return true;
        }
    }

    private bool CheckLeft() {
        try {

            int x = Mathf.FloorToInt(nextPosition.x - playerWidth);
            int y = Mathf.FloorToInt(nextPosition.y);
            int z = Mathf.FloorToInt(nextPosition.z);

            return HelperFunctions.CheckBlockInPlanet(planet, x, y, z) || HelperFunctions.CheckBlockInPlanet(planet, x, y + 1, z);

        } catch (System.Exception ex) {
            Debug.LogError("Error in Player CheckLeft: " + ex.Message);
            return true;
        }
    }

    private bool CheckRight() {
        try {

            int x = Mathf.FloorToInt(nextPosition.x + playerWidth);
            int y = Mathf.FloorToInt(nextPosition.y);
            int z = Mathf.FloorToInt(nextPosition.z);

            return HelperFunctions.CheckBlockInPlanet(planet, x, y, z) || HelperFunctions.CheckBlockInPlanet(planet, x, y + 1, z);

        } catch (System.Exception ex) {
            Debug.LogError("Error in Player CheckRight: " + ex.Message);
            return true;
        }
    }
}