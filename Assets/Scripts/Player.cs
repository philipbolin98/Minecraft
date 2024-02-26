using System;
using TMPro;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour {

    public float walkSpeed;
    public float sprintSpeed;
    public float crouchSpeed;
    public float jumpForce;
    public float reach;

    private bool isGrounded;
    private bool isSprinting;
    private bool isCrouching;
    private Vector2Int currentChunk;

    private Transform cam;
    private Planet planet;

    private float mouseSensitivity = 3f;
    private static float playerRadius = .25f;
    private static float playerStandHeight = 1.8f;
    private static float playerCrouchHeight = 1.4f;
    private float playerHeight = playerStandHeight;

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
            currentChunk = HelperFunctions.GetChunkPosFromBlockPos(Mathf.FloorToInt(currentPosition.x), Mathf.FloorToInt(currentPosition.z));

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

            Vector2Int newChunk = HelperFunctions.GetChunkPosFromBlockPos(Mathf.FloorToInt(currentPosition.x), Mathf.FloorToInt(currentPosition.z));

            if (newChunk.x != currentChunk.x || newChunk.y != currentChunk.y) {
                currentChunk = newChunk;
                this.planet.CheckRenderDistance(currentPosition);
            }

            this.planet.RenderChunks();
            this.UpdateDebugUI();

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

    private void UpdateDebugUI() {
        try {

            int halfPlanetSize = this.planet.PlanetSizeInVoxels / 2;
            float x = HelperFunctions.Modulo(this.currentPosition.x, this.planet.PlanetSizeInVoxels);
            float z = HelperFunctions.Modulo(this.currentPosition.z, this.planet.PlanetSizeInVoxels);

            if (x >= halfPlanetSize) {
                x -= this.planet.PlanetSizeInVoxels;
            }

            if (z >= halfPlanetSize) {
                z -= this.planet.PlanetSizeInVoxels;
            }

            Vector3 playerPos = new Vector3(x, this.currentPosition.y, z);
            playerBlockCoordsUI.text = playerPos.ToString();

            int chunkX = Mathf.FloorToInt(x / VoxelData.ChunkSize);
            int chunkZ = Mathf.FloorToInt(z / VoxelData.ChunkSize);
            Vector2Int chunkPos = new Vector2Int(chunkX, chunkZ);

            playerChunkCoordsUI.text = chunkPos.ToString();

        } catch (Exception ex) {
            Debug.LogError("Error in Player UpdateDebugUI: " + ex.Message);
        }
    }

    private void PositionHighlightBlocks() {
        try {

            Vector3Int camPos = Vector3Int.FloorToInt(cam.position);

            if (GetTargetedBlock(camPos)) {

                if (breakHighlightBlock.transform.position != breakPos || !breakHighlightBlock.activeSelf) {
                    breakHighlightBlock.transform.position = breakPos;
                    breakHighlightBlock.SetActive(true);
                }

                if (CanPlayerPlaceBlock()) {

                    if (placeHighlightBlock.transform.position != placePos || !placeHighlightBlock.activeSelf) {
                        placeHighlightBlock.transform.position = placePos;
                        placeHighlightBlock.SetActive(true);
                    }

                } else {

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

    private bool CanPlayerPlaceBlock() {
        try {

            float minX = Mathf.FloorToInt(currentPosition.x - playerRadius);
            float maxX = Mathf.CeilToInt(currentPosition.x + playerRadius);
            float minY = Mathf.FloorToInt(currentPosition.y);
            float maxY = Mathf.CeilToInt(currentPosition.y + playerHeight);
            float minZ = Mathf.FloorToInt(currentPosition.z - playerRadius);
            float maxZ = Mathf.CeilToInt(currentPosition.z + playerRadius);

            return placePos.x < minX || placePos.x >= maxX || placePos.y < minY || placePos.y >= maxY || placePos.z < minZ || placePos.z >= maxZ;

        } catch (Exception ex) {
            Debug.LogError("Error in Player CanPlayerPlaceBlock: " + ex.Message);
            return false;
        }
    }

    private bool GetTargetedBlock(Vector3Int startBlockPos) {
        try {

            Vector3 startPos = cam.position;
            Vector3 endPos = startPos + cam.forward.normalized * reach;
            Vector3Int blockPos = Vector3Int.zero;

            bool hit = false;

            if (cam.forward.x > 0) {
                blockPos = startBlockPos + Vector3Int.right;
                hit = HelperFunctions.LiangBarsky3D(startPos, endPos, blockPos);
            } else if (cam.forward.x < 0) {
                blockPos = startBlockPos + Vector3Int.left;
                hit = HelperFunctions.LiangBarsky3D(startPos, endPos, blockPos);
            }

            if (hit) {
                if (HelperFunctions.CheckBlockInPlanet(this.planet, blockPos.x, blockPos.y, blockPos.z)) {
                    breakPos = blockPos;
                    placePos = startBlockPos;
                    return true;
                } else {
                    return GetTargetedBlock(blockPos);
                }
            }

            if (cam.forward.y > 0) {
                blockPos = startBlockPos + Vector3Int.up;
                hit = HelperFunctions.LiangBarsky3D(startPos, endPos, blockPos);
            } else if (cam.forward.y < 0) {
                blockPos = startBlockPos + Vector3Int.down;
                hit = HelperFunctions.LiangBarsky3D(startPos, endPos, blockPos);
            }

            if (hit) {
                if (HelperFunctions.CheckBlockInPlanet(this.planet, blockPos.x, blockPos.y, blockPos.z)) {
                    breakPos = blockPos;
                    placePos = startBlockPos;
                    return true;
                } else {
                    return GetTargetedBlock(blockPos);
                }
            }

            if (cam.forward.z > 0) {
                blockPos = startBlockPos + Vector3Int.forward;
                hit = HelperFunctions.LiangBarsky3D(startPos, endPos, blockPos);
            } else if (cam.forward.z < 0) {
                blockPos = startBlockPos + Vector3Int.back;
                hit = HelperFunctions.LiangBarsky3D(startPos, endPos, blockPos);
            }

            if (hit) {
                if (HelperFunctions.CheckBlockInPlanet(this.planet, blockPos.x, blockPos.y, blockPos.z)) {
                    breakPos = blockPos;
                    placePos = startBlockPos;
                    return true;
                } else {
                    return GetTargetedBlock(blockPos);
                }
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Player GetTargetedBlock: " + ex.Message);
        }

        return false;
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

            float speedModifier;

            if (isCrouching) {
                speedModifier = crouchSpeed;
                playerHeight = playerCrouchHeight;
            } else {
                speedModifier = isSprinting ? sprintSpeed : walkSpeed;
                playerHeight = playerStandHeight;
            }

            cam.localPosition = new Vector3(0, playerHeight - .2f, -.15f);

            velocity = speedModifier * Time.deltaTime * ((transform.forward * vertical) + (transform.right * horizontal));

            //Apply vertical momentum
            velocity += Time.fixedDeltaTime * verticalMomentum * Vector3.up;

            Vector3 nextPosition = this.currentPosition;

            if (velocity.y < 0) {
                if (CheckTopOrBottom(nextPosition, VoxelData.BlockFace.Bottom)) {
                    nextPosition.y = Mathf.CeilToInt(nextPosition.y + velocity.y);
                    velocity.y = 0;
                    isGrounded = true;
                } else {
                    nextPosition.y += velocity.y;
                    isGrounded = false;
                }
            } else if (velocity.y > 0) {
                if (CheckTopOrBottom(nextPosition, VoxelData.BlockFace.Top)) {
                    nextPosition.y = Mathf.FloorToInt(nextPosition.y + velocity.y + playerHeight) - playerHeight;
                    velocity.y = 0;
                    verticalMomentum = 0;
                } else {
                    nextPosition.y += velocity.y;
                }
            }

            if (velocity.x > 0) {
                if (CheckSide(nextPosition, VoxelData.BlockFace.Right)) {
                    nextPosition.x = Mathf.FloorToInt(nextPosition.x + velocity.x + playerRadius) - playerRadius;
                    velocity.x = 0;
                } else {
                    nextPosition.x += velocity.x;
                }
            } else if (velocity.x < 0) {
                if (CheckSide(nextPosition, VoxelData.BlockFace.Left)) {
                    nextPosition.x = Mathf.CeilToInt(nextPosition.x + velocity.x - playerRadius) + playerRadius;
                    velocity.x = 0;
                } else {
                    nextPosition.x += velocity.x;
                }
            }

            if (velocity.z > 0) {
                if (CheckSide(nextPosition, VoxelData.BlockFace.Front)) {
                    nextPosition.z = Mathf.FloorToInt(nextPosition.z + velocity.z + playerRadius) - playerRadius;
                    velocity.z = 0;
                } else {
                    nextPosition.z += velocity.z;
                }
            } else if (velocity.z < 0) {
                if (CheckSide(nextPosition, VoxelData.BlockFace.Back)) {
                    nextPosition.z = Mathf.CeilToInt(nextPosition.z + velocity.z - playerRadius) + playerRadius;
                    velocity.z = 0;
                } else {
                    nextPosition.z += velocity.z;
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

            if (Input.GetButtonDown("Crouch")) {
                isCrouching = true;
            }

            if (Input.GetButtonUp("Crouch")) {
                isCrouching = false;
            }

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
                PositionHighlightBlocks();
            }

            if (Input.GetMouseButtonDown(1) && placeHighlightBlock.activeSelf) {
                EditBlock(placePos, BlockID.Stone);
                PositionHighlightBlocks();
            }

        } catch (Exception ex) {
            Debug.LogError("Error in function Player.GetPlayerInputs: " + ex.Message);
        }
    }

    private void EditBlock(Vector3Int blockPosInPlanet, BlockID blockID) {
        try {

            Chunk chunk = HelperFunctions.GetChunkFromBlockPos(this.planet, blockPosInPlanet.x, blockPosInPlanet.y, blockPosInPlanet.z);

            if (chunk == null || chunk.stage < ChunkStage.Rendered) {
                return;
            }

            Vector3Int blockPosInChunk = HelperFunctions.NormalizeBlockPos(blockPosInPlanet.x, blockPosInPlanet.y, blockPosInPlanet.z, VoxelData.ChunkSize);

            chunk.data.blocks[blockPosInChunk.x, blockPosInChunk.y, blockPosInChunk.z] = blockID;

            RegenerateMesh(chunk.pos);

            if (blockPosInChunk.x == 0) {
                RegenerateMesh(chunk.pos + Vector2Int.left);
            }

            if (blockPosInChunk.z == 0) {
                RegenerateMesh(chunk.pos + Vector2Int.down);
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Player EditBlock: " + ex.Message);
        }
    }

    private void RegenerateMesh(Vector2Int chunkPos) {
        try {

            chunkPos = HelperFunctions.NormalizeChunkPos(this.planet, chunkPos);
            Chunk chunk = this.planet.chunks[chunkPos.x, chunkPos.y];

            if (chunk == null) {
                return;
            }

            chunk.CreateMeshData();
            chunk.CreateMesh();

        } catch (Exception ex) {
            Debug.LogError("Error in Player PrepareChunkForEditing: " + ex.Message);
        }
    }

    private bool CheckTopOrBottom(Vector3 pos, VoxelData.BlockFace side) {
        try {

            int y = Mathf.FloorToInt(pos.y + velocity.y + (side == VoxelData.BlockFace.Top ? playerHeight : 0));
            int minX = Mathf.FloorToInt(pos.x - playerRadius);
            int maxX = Mathf.CeilToInt(pos.x + playerRadius);
            int minZ = Mathf.FloorToInt(pos.z - playerRadius);
            int maxZ = Mathf.CeilToInt(pos.z + playerRadius);

            for (int x = minX; x < maxX; x++) {
                for (int z = minZ; z < maxZ; z++) {
                    if (HelperFunctions.CheckBlockInPlanet(planet, x, y, z)) {
                        return true;
                    }
                }
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Player CheckBottom: " + ex.Message);
        }

        return false;
    }

    private bool CheckSide(Vector3 pos, VoxelData.BlockFace side) {
        try {

            int minY = Mathf.FloorToInt(currentPosition.y);
            int maxY = Mathf.CeilToInt(currentPosition.y + playerHeight - 1);
            int minX, minZ, maxX, maxZ;

            switch (side) {
                case VoxelData.BlockFace.Right:
                    minX = Mathf.FloorToInt(pos.x + velocity.x + playerRadius);
                    maxX = minX + 1;
                    minZ = Mathf.FloorToInt(pos.z - playerRadius);
                    maxZ = Mathf.CeilToInt(pos.z + playerRadius);
                    break;
                case VoxelData.BlockFace.Left:
                    minX = Mathf.FloorToInt(pos.x + velocity.x - playerRadius);
                    maxX = minX + 1;
                    minZ = Mathf.FloorToInt(pos.z - playerRadius);
                    maxZ = Mathf.CeilToInt(pos.z + playerRadius);
                    break;
                case VoxelData.BlockFace.Front:
                    minX = Mathf.FloorToInt(pos.x - playerRadius);
                    maxX = Mathf.CeilToInt(pos.x + playerRadius);
                    minZ = Mathf.FloorToInt(pos.z + velocity.z + playerRadius);
                    maxZ = minZ + 1;
                    break;
                case VoxelData.BlockFace.Back:
                    minX = Mathf.FloorToInt(pos.x - playerRadius);
                    maxX = Mathf.CeilToInt(pos.x + playerRadius);
                    minZ = Mathf.FloorToInt(pos.z + velocity.z - playerRadius);
                    maxZ = minZ + 1;
                    break;
                default:
                    return false;
            }

            for (int x = minX; x < maxX; x++) {
                for (int z = minZ; z < maxZ; z++) {
                    for (int y = minY; y <= maxY; y++) {
                        if (HelperFunctions.CheckBlockInPlanet(planet, x, y, z)) {
                            return true;
                        }
                    }
                }
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Player CheckSide: " + ex.Message);
        }

        return false;
    }
}