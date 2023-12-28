using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour{

    private bool isGrounded;
    private bool isSprinting;

    private Transform cam;
    private Planet planet;

    private float walkSpeed = 3f;
    private float sprintSpeed = 6f;
    private float jumpForce = 5f;
    private float gravity = -9.8f;

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

    private void Start() {
        try {

            cam = GameObject.Find("Main Camera").transform;
            planet = UniverseData.currentPlanet;

            nextPosition = transform.position;

        } catch (System.Exception e) {
            Debug.LogError("Error in function Player.Start: " + e.Message);
        }
    }

    private void FixedUpdate() {
        try {

            CalculateVelocity();
            if (jumpRequest)
                Jump();

            transform.eulerAngles = new Vector3(transform.eulerAngles.x, rotationY, transform.eulerAngles.z);
            cam.transform.eulerAngles = new Vector3(rotationX, cam.transform.eulerAngles.y, cam.transform.eulerAngles.z);

            transform.position = nextPosition;

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

            if (velocity.y < 0) {
                velocity.y = checkDownSpeed(velocity.y);
            } else if (velocity.y > 0) {
                velocity.y = checkUpSpeed(velocity.y);
            }

            nextPosition += velocity;

            if (isGrounded) {
                nextPosition.y = Mathf.Clamp(nextPosition.y, nextPosition.y - 1, Mathf.Floor(transform.position.y));
            }

            //Check if running into any blocks in all four directions
            if (velocity.z > 0 && front) {
                velocity.z = 0;
                nextPosition.z = Mathf.Clamp(nextPosition.z, nextPosition.z - 1, Mathf.Ceil(nextPosition.z) - playerWidth);
            }

            if (velocity.z < 0 && back) {
                velocity.z = 0;
                nextPosition.z = Mathf.Clamp(nextPosition.z, Mathf.Floor(nextPosition.z) + playerWidth, nextPosition.z + 1);
            }

            if(velocity.x > 0 && right) {
                velocity.x = 0;
                nextPosition.x = Mathf.Clamp(nextPosition.x, nextPosition.x - 1, Mathf.Ceil(nextPosition.x) - playerWidth);
            }

            if (velocity.x < 0 && left) {
                velocity.x = 0;
                nextPosition.x = Mathf.Clamp(nextPosition.x, Mathf.Floor(nextPosition.x) + playerWidth, nextPosition.x + 1);
            }

            //Wrap around to other side of the world
            if(nextPosition.x > VoxelData.PlanetSizeInVoxels) {
                nextPosition.x = 0;
            } else if (nextPosition.x < 0) {
                nextPosition.x = VoxelData.PlanetSizeInVoxels;
            }

            if (nextPosition.z > VoxelData.PlanetSizeInVoxels) {
                nextPosition.z = 0;
            } else if (nextPosition.z < 0) {
                nextPosition.z = VoxelData.PlanetSizeInVoxels;
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
                SaveSystem.SavePlanet(UniverseData.currentPlanet.planetData);
            }

        } catch (System.Exception e) {
            Debug.LogError("Error in function Player.GetPlayerInputs: " + e.Message);
        }
    }

    private float checkDownSpeed(float downSpeed) {

        if (planet.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
        planet.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
        planet.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)) ||
        planet.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth))) {

            isGrounded = true;
            return 0;

        } else {
            isGrounded = false;
            return downSpeed;
        }
    }

    private float checkUpSpeed(float upSpeed) {
        if (planet.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) ||
        planet.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) ||
        planet.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth)) ||
        planet.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth))) {

            return 0;

        } else {

            return upSpeed;
        }
    }

    public bool front {
        get {
            if (planet.CheckForVoxel(new Vector3(nextPosition.x, nextPosition.y, nextPosition.z + playerWidth)) ||
                planet.CheckForVoxel(new Vector3(nextPosition.x, nextPosition.y + 1f, nextPosition.z + playerWidth)))

                return true;

            else
                return false;
        }
    }

    public bool back {
        get {
            if (planet.CheckForVoxel(new Vector3(nextPosition.x, nextPosition.y, nextPosition.z - playerWidth)) ||
                planet.CheckForVoxel(new Vector3(nextPosition.x, nextPosition.y + 1f, nextPosition.z - playerWidth)))

                return true;

            else
                return false;
        }
    }

    public bool left {
        get {
            if (planet.CheckForVoxel(new Vector3(nextPosition.x - playerWidth, nextPosition.y, nextPosition.z)) ||
                planet.CheckForVoxel(new Vector3(nextPosition.x - playerWidth, nextPosition.y + 1f, nextPosition.z)))

                return true;

            else
                return false;
        }
    }

    public bool right {
        get {
            if (planet.CheckForVoxel(new Vector3(nextPosition.x + playerWidth, nextPosition.y, nextPosition.z)) ||
                planet.CheckForVoxel(new Vector3(nextPosition.x + playerWidth, nextPosition.y + 1f, nextPosition.z)))

                return true;

            else
                return false;
        }
    }
}