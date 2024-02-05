using System;
using System.Collections.Generic;
using TMPro;
using Unity.Profiling;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour {

    public float walkSpeed;
    public float sprintSpeed;
    public float jumpForce;
    public float reach;

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

    private GameObject breakHighlightBlock;
    private GameObject placeHighlightBlock;

    private Vector3Int breakPos;
    private Vector3Int placePos;

    private void Start() {
        try {

            cam = GameObject.Find("Main Camera").transform;
            planet = GameObject.Find("Planet").GetComponent<Planet>();
            playerBlockCoordsUI = GameObject.Find("PlayerBlockCoords").GetComponent<TextMeshProUGUI>();
            playerChunkCoordsUI = GameObject.Find("PlayerChunkCoords").GetComponent<TextMeshProUGUI>();
            breakHighlightBlock = GameObject.Find("HighlightBlock");
            placeHighlightBlock = GameObject.Find("PlaceBlock");

            currentPosition = transform.position;
            currentChunk = HelperFunctions.GetChunkPosFromBlockPos(Mathf.FloorToInt(currentPosition.x), Mathf.FloorToInt(currentPosition.y), Mathf.FloorToInt(currentPosition.z));

            Cursor.lockState = CursorLockMode.Locked;

        } catch (Exception ex) {
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
            cam.eulerAngles = new Vector3(rotationX, cam.eulerAngles.y, cam.eulerAngles.z);

            transform.position = currentPosition;

            Vector2Int newChunk = HelperFunctions.GetChunkPosFromBlockPos(Mathf.FloorToInt(currentPosition.x), Mathf.FloorToInt(currentPosition.y), Mathf.FloorToInt(currentPosition.z));

            if (newChunk.x != currentChunk.x || newChunk.y != currentChunk.y) {
                currentChunk = newChunk;
                this.planet.CheckRenderDistance(currentPosition);
            }

            this.planet.RenderChunks();

            playerBlockCoordsUI.text = currentPosition.ToString();
            playerChunkCoordsUI.text = currentChunk.ToString();

        } catch (Exception ex) {
            Debug.LogError("Error in Player FixedUpdate: " + ex.Message);
        }
    }

    private void Update() {
        try {

            GetPlayerInputs();
            PositionHighlightBlocks();

        } catch (Exception ex) {
            Debug.LogError("Error in Player Update: " + ex.Message);
        }
    }

    private void PositionHighlightBlocks() {
        try {

            Ray ray = new Ray(cam.position, cam.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, reach)) {

                int x, y, z;

                if (hit.normal == Vector3.up || hit.normal == Vector3.down) {
                    x = Mathf.FloorToInt(hit.point.x);
                    y = Mathf.RoundToInt(hit.point.y);
                    z = Mathf.FloorToInt(hit.point.z);
                } else if (hit.normal == Vector3.left || hit.normal == Vector3.right) {
                    x = Mathf.RoundToInt(hit.point.x);
                    y = Mathf.FloorToInt(hit.point.y);
                    z = Mathf.FloorToInt(hit.point.z);
                } else { //Forward or back
                    x = Mathf.FloorToInt(hit.point.x);
                    y = Mathf.FloorToInt(hit.point.y);
                    z = Mathf.RoundToInt(hit.point.z);
                }

                breakPos = new Vector3Int(x, y, z);

                Vector3Int normal = new Vector3Int(Mathf.FloorToInt(hit.normal.x), Mathf.FloorToInt(hit.normal.y), Mathf.FloorToInt(hit.normal.z));

                if (normal == Vector3Int.up || normal == Vector3Int.right || normal == Vector3Int.forward) {
                    breakPos -= normal;
                }

                if (HelperFunctions.CheckBlockInPlanet(this.planet, breakPos.x, breakPos.y, breakPos.z)) {

                    if (breakHighlightBlock.transform.position != breakPos || !breakHighlightBlock.activeSelf) {
                        breakHighlightBlock.transform.position = breakPos;
                        breakHighlightBlock.SetActive(true);
                    }

                    placePos = breakPos + normal;

                    if (placeHighlightBlock.transform.position != placePos || !placeHighlightBlock.activeSelf) {
                        placeHighlightBlock.transform.position = placePos;
                        placeHighlightBlock.SetActive(true);
                    }

                } else {

                    if (breakHighlightBlock.activeSelf) {
                        breakHighlightBlock.SetActive(false);
                    }

                    if (placeHighlightBlock.activeSelf) {
                        placeHighlightBlock.SetActive(false);
                    }
                }

            } else {

                if (breakHighlightBlock.activeSelf) {
                    breakHighlightBlock.SetActive(false);
                }

                if (placeHighlightBlock.activeSelf) {
                    placeHighlightBlock.SetActive(false);
                }
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Player PositionHighlightBlocks: " + ex.Message);
        }
    }

    void Jump() {
        try {

            verticalMomentum = jumpForce;
            isGrounded = false;
            jumpRequest = false;

        } catch (Exception e) {
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
                if (CheckSide(VoxelData.BlockFace.Front)) {
                    velocity.z = 0;
                    nextPosition.z = Mathf.FloorToInt(nextPosition.z + playerWidth) - playerWidth;
                }
            } else if (velocity.z < 0) {
                if (CheckSide(VoxelData.BlockFace.Back)) {
                    velocity.z = 0;
                    nextPosition.z = Mathf.CeilToInt(nextPosition.z - playerWidth) + playerWidth;
                }
            }

            if (velocity.x > 0) {
                if (CheckSide(VoxelData.BlockFace.Left)) {
                    velocity.x = 0;
                    nextPosition.x = Mathf.FloorToInt(nextPosition.x + playerWidth) - playerWidth;
                }
            } else if (velocity.x < 0) {
                if (CheckSide(VoxelData.BlockFace.Right)) {
                    velocity.x = 0;
                    nextPosition.x = Mathf.CeilToInt(nextPosition.x - playerWidth) + playerWidth;
                }
            }

            this.currentPosition = nextPosition;

        } catch (Exception e) {
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

            if (Input.GetMouseButtonDown(0) && breakHighlightBlock.activeSelf) {
                EditBlock(breakPos, BlockID.Air);
            }

            if (Input.GetMouseButtonDown(1) && placeHighlightBlock.activeSelf) {
                EditBlock(placePos, BlockID.Stone);
            }

        } catch (Exception ex) {
            Debug.LogError("Error in function Player.GetPlayerInputs: " + ex.Message);
        }
    }

    private void EditBlock(Vector3Int blockPosInPlanet, BlockID blockID) {
        try {

            Chunk chunk = HelperFunctions.GetChunkFromBlockPos(this.planet, blockPosInPlanet.x, blockPosInPlanet.y, blockPosInPlanet.z);

            if (chunk == null) {
                return;
            }

            Vector3Int blockPosInChunk = HelperFunctions.GetNormalizedBlockPosInChunk(blockPosInPlanet);

            chunk.data.blocks[blockPosInChunk.x, blockPosInChunk.y, blockPosInChunk.z] = blockID;

            RegenerateMesh(chunk.pos);

            if (blockPosInChunk.x == 0) {
                RegenerateMesh(chunk.pos + Vector2Int.left);
            } else if (blockPosInChunk.x == VoxelData.ChunkSize - 1) {
                RegenerateMesh(chunk.pos + Vector2Int.right);
            }

            if (blockPosInChunk.z == 0) {
                RegenerateMesh(chunk.pos + Vector2Int.down);
            } else if (blockPosInChunk.z ==  VoxelData.ChunkSize - 1) {
                RegenerateMesh(chunk.pos + Vector2Int.up);
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Player EditBlock: " + ex.Message);
        }
    }

    private void RegenerateMesh(Vector2Int chunkPos) {
        try {

            chunkPos = HelperFunctions.GetNormalizedChunkPos(this.planet, chunkPos);
            Chunk chunk = this.planet.chunks[chunkPos.x, chunkPos.y];

            if (chunk == null) {
                return;
            }

            chunk.isRendered = false;
            chunk.isMeshGenerated = false;

            lock (this.planet.chunksToMeshLock) {
                this.planet.chunksToMesh.Insert(0, chunkPos);
            }

            //Debug.Log(DateTime.Now + " Editing chunk: " + chunkPos);

        } catch (Exception ex) {
            Debug.LogError("Error in Player PrepareChunkForEditing: " + ex.Message);
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

        } catch (Exception ex) {
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

        } catch (Exception ex) {
            Debug.LogError("Error in Player CheckTop: " + ex.Message);
            return true;
        }
    }

    private bool CheckSide(VoxelData.BlockFace side) {
        try {

            int y = Mathf.FloorToInt(currentPosition.y);
            int x, z;

            switch (side) {
                case VoxelData.BlockFace.Left:
                    x = Mathf.FloorToInt(currentPosition.x + velocity.x + playerWidth);
                    z = Mathf.FloorToInt(currentPosition.z);
                    break;
                case VoxelData.BlockFace.Right:
                    x = Mathf.FloorToInt(currentPosition.x + velocity.x - playerWidth);
                    z = Mathf.FloorToInt(currentPosition.z);
                    break;
                case VoxelData.BlockFace.Front:
                    x = Mathf.FloorToInt(currentPosition.x);
                    z = Mathf.FloorToInt(currentPosition.z + velocity.z + playerWidth);
                    break;
                case VoxelData.BlockFace.Back:
                    x = Mathf.FloorToInt(currentPosition.x);
                    z = Mathf.FloorToInt(currentPosition.z + velocity.z - playerWidth);
                    break;
                default:
                    return true;
            }

            return HelperFunctions.CheckBlockInPlanet(planet, x, y, z) || HelperFunctions.CheckBlockInPlanet(planet, x, y + 1, z);

        } catch (Exception ex) {
            Debug.LogError("Error in Player CheckSide: " + ex.Message);
            return true;
        }
    }
}