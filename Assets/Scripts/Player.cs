using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour {

    public float walkSpeed;
    public float sprintSpeed;
    public float crouchSpeed;
    public float jumpForce;
    public float reach;

    private Inventory inventory;

    private bool isGrounded;
    private bool isSprinting;
    private bool isCrouching;
    private bool spectatorMode;
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
    private int ascentDirection = 0;
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

            inventory = new Inventory();

            currentPosition = transform.position;
            currentChunk = HelperFunctions.GetChunkPosFromBlockPos(Mathf.FloorToInt(currentPosition.x), Mathf.FloorToInt(currentPosition.z));

            Cursor.lockState = CursorLockMode.Locked;

        } catch (Exception ex) {
            Debug.LogError("Error in Player Start: " + ex.Message);
        }
    }

    private void FixedUpdate() {
        try {

            if (spectatorMode) {
                Fly();
            } else {
                MovePlayer();
                if (jumpRequest) {
                    Jump();
                }
            }

            transform.eulerAngles = new Vector3(transform.eulerAngles.x, rotationY, transform.eulerAngles.z);
            cam.eulerAngles = new Vector3(rotationX, cam.eulerAngles.y, cam.eulerAngles.z);

            transform.position = currentPosition;

            Vector2Int newChunk = HelperFunctions.GetChunkPosFromBlockPos(Mathf.FloorToInt(currentPosition.x), Mathf.FloorToInt(currentPosition.z));

            if (newChunk.x != this.currentChunk.x || newChunk.y != this.currentChunk.y) {
                this.currentChunk = newChunk;
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

    /// <summary>
    /// Gets the player inputs
    /// </summary>
    private void GetPlayerInputs() {
        try {

            horizontal = Input.GetAxis("Horizontal"); //A and D keys
            vertical = Input.GetAxis("Vertical"); //W and S keys

            //Set rotation based on mouse movement
            rotationY += Input.GetAxis("Mouse X") * mouseSensitivity;
            rotationX -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            isSprinting = Input.GetButton("Sprint");

            //F2 to enable spectator mode for debugging purposes
            if (Input.GetKeyDown(KeyCode.F2)) {
                spectatorMode = !spectatorMode;
            }

            if (spectatorMode) {

                if (Input.GetButton("Jump")) {
                    ascentDirection = 1;
                } else if (Input.GetButton("Crouch")) {
                    ascentDirection = -1;
                } else {
                    ascentDirection = 0;
                }

                isCrouching = false;
                jumpRequest = false;

            } else {

                ascentDirection = 0;
                isCrouching = Input.GetButton("Crouch");

                //Space to jump only if currently grounded
                if (isGrounded && Input.GetButtonDown("Jump")) {
                    jumpRequest = true;
                }
            }

            //F1 to reload the game for debugging purposes
            if (Input.GetKeyDown(KeyCode.F1)) {
                SceneManager.LoadScene("SampleScene");
            }

            //On left click, break the block at the targeted location
            if (Input.GetMouseButtonDown(0) && breakHighlightBlock.activeSelf) {
                EditBlock(breakPos, BlockID.Air);
                PositionHighlightBlocks();
            }

            //On right click, place a block at the targeted location
            if (Input.GetMouseButtonDown(1) && placeHighlightBlock.activeSelf) {
                EditBlock(placePos, BlockID.Stone);
                PositionHighlightBlocks();
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (scroll != 0) {
                inventory.ChangeActiveSlot(scroll);
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Player.GetPlayerInputs: " + ex.Message);
        }
    }

    /// <summary>
    /// Calculates the next position of the player based off velocity, gravity, etc.
    /// </summary>
    private void MovePlayer() {
        try {

            //Apply gravity to vertical momentum
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
                if (CheckSide(nextPosition, Block.BlockFace.Bottom)) {
                    nextPosition.y = Mathf.CeilToInt(nextPosition.y + velocity.y);
                    velocity.y = 0;
                    isGrounded = true;
                } else {
                    nextPosition.y += velocity.y;
                    isGrounded = false;
                }
            } else if (velocity.y > 0) {
                if (CheckSide(nextPosition, Block.BlockFace.Top)) {
                    nextPosition.y = Mathf.FloorToInt(nextPosition.y + velocity.y + playerHeight) - playerHeight;
                    velocity.y = 0;
                    verticalMomentum = 0;
                } else {
                    nextPosition.y += velocity.y;
                }
            }

            if (velocity.x > 0) {
                if (CheckSide(nextPosition, Block.BlockFace.Right)) {
                    nextPosition.x = Mathf.FloorToInt(nextPosition.x + velocity.x + playerRadius) - playerRadius;
                    velocity.x = 0;
                } else {
                    nextPosition.x += velocity.x;
                }
            } else if (velocity.x < 0) {
                if (CheckSide(nextPosition, Block.BlockFace.Left)) {
                    nextPosition.x = Mathf.CeilToInt(nextPosition.x + velocity.x - playerRadius) + playerRadius;
                    velocity.x = 0;
                } else {
                    nextPosition.x += velocity.x;
                }
            }

            if (velocity.z > 0) {
                if (CheckSide(nextPosition, Block.BlockFace.Front)) {
                    nextPosition.z = Mathf.FloorToInt(nextPosition.z + velocity.z + playerRadius) - playerRadius;
                    velocity.z = 0;
                } else {
                    nextPosition.z += velocity.z;
                }
            } else if (velocity.z < 0) {
                if (CheckSide(nextPosition, Block.BlockFace.Back)) {
                    nextPosition.z = Mathf.CeilToInt(nextPosition.z + velocity.z - playerRadius) + playerRadius;
                    velocity.z = 0;
                } else {
                    nextPosition.z += velocity.z;
                }
            }

            this.currentPosition = nextPosition;

        } catch (Exception ex) {
            Debug.LogError("Error in Player.MovePlayer: " + ex.Message);
        }
    }

    private void Fly() {
        try {

            float speedModifier = (isSprinting ? sprintSpeed : walkSpeed) * 3;
            velocity = speedModifier * Time.deltaTime * ((transform.forward * vertical) + (transform.right * horizontal));

            float ascent = ascentDirection * speedModifier * Time.deltaTime;

            Vector3 nextPosition = currentPosition;
            nextPosition.x += velocity.x;
            nextPosition.z += velocity.z;
            nextPosition.y += ascent;

            this.currentPosition = nextPosition;

        } catch (Exception ex) {
            Debug.LogError("Error in Player.Fly: " + ex.Message);
        }
    }

    /// <summary>
    /// Checks if there are any blocks on a given side of the player that impedes his movement in that direction
    /// </summary>
    /// <param name="pos">The player's position</param>
    /// <param name="side">The side to check</param>
    /// <returns>True if the player is running into a block, false otherwise</returns>
    private bool CheckSide(Vector3 pos, Block.BlockFace side) {
        try {
            
            int minX, maxX, minY, maxY, minZ, maxZ;

            switch (side) {
                case Block.BlockFace.Right:
                    minX = Mathf.FloorToInt(pos.x + velocity.x + playerRadius);
                    maxX = minX + 1;
                    minY = Mathf.FloorToInt(currentPosition.y);
                    maxY = Mathf.CeilToInt(currentPosition.y + playerHeight - 1) + 1;
                    minZ = Mathf.FloorToInt(pos.z - playerRadius);
                    maxZ = Mathf.CeilToInt(pos.z + playerRadius);
                    break;
                case Block.BlockFace.Left:
                    minX = Mathf.FloorToInt(pos.x + velocity.x - playerRadius);
                    maxX = minX + 1;
                    minY = Mathf.FloorToInt(currentPosition.y);
                    maxY = Mathf.CeilToInt(currentPosition.y + playerHeight - 1) + 1;
                    minZ = Mathf.FloorToInt(pos.z - playerRadius);
                    maxZ = Mathf.CeilToInt(pos.z + playerRadius);
                    break;
                case Block.BlockFace.Front:
                    minX = Mathf.FloorToInt(pos.x - playerRadius);
                    maxX = Mathf.CeilToInt(pos.x + playerRadius);
                    minY = Mathf.FloorToInt(currentPosition.y);
                    maxY = Mathf.CeilToInt(currentPosition.y + playerHeight - 1) + 1;
                    minZ = Mathf.FloorToInt(pos.z + velocity.z + playerRadius);
                    maxZ = minZ + 1;
                    break;
                case Block.BlockFace.Back:
                    minX = Mathf.FloorToInt(pos.x - playerRadius);
                    maxX = Mathf.CeilToInt(pos.x + playerRadius);
                    minY = Mathf.FloorToInt(currentPosition.y);
                    maxY = Mathf.CeilToInt(currentPosition.y + playerHeight - 1) + 1;
                    minZ = Mathf.FloorToInt(pos.z + velocity.z - playerRadius);
                    maxZ = minZ + 1;
                    break;
                case Block.BlockFace.Top:
                    minX = Mathf.FloorToInt(pos.x - playerRadius);
                    maxX = Mathf.CeilToInt(pos.x + playerRadius);
                    minY = Mathf.FloorToInt(pos.y + velocity.y + playerHeight);
                    maxY = minY + 1;
                    minZ = Mathf.FloorToInt(pos.z - playerRadius);
                    maxZ = Mathf.CeilToInt(pos.z + playerRadius);
                    break;
                case Block.BlockFace.Bottom:
                    minX = Mathf.FloorToInt(pos.x - playerRadius);
                    maxX = Mathf.CeilToInt(pos.x + playerRadius);
                    minY = Mathf.FloorToInt(pos.y + velocity.y);
                    maxY = minY + 1;
                    minZ = Mathf.FloorToInt(pos.z - playerRadius);
                    maxZ = Mathf.CeilToInt(pos.z + playerRadius);
                    break;
                default:
                    return false;
            }

            for (int x = minX; x < maxX; x++) {
                for (int z = minZ; z < maxZ; z++) {
                    for (int y = minY; y < maxY; y++) {
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

    /// <summary>
    /// Sets the variables required to signal a jump
    /// </summary>
    void Jump() {
        try {

            verticalMomentum = jumpForce;
            isGrounded = false;
            jumpRequest = false;

        } catch (Exception e) {
            Debug.LogError("Error in function Player.Jump: " + e.Message);
        }
    }

    /// <summary>
    /// Updates the debug UI
    /// </summary>
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

            int chunkX = Mathf.FloorToInt(x / Chunk.ChunkSize);
            int chunkZ = Mathf.FloorToInt(z / Chunk.ChunkSize);
            Vector2Int chunkPos = new Vector2Int(chunkX, chunkZ);

            playerChunkCoordsUI.text = chunkPos.ToString();

        } catch (Exception ex) {
            Debug.LogError("Error in Player UpdateDebugUI: " + ex.Message);
        }
    }

    /// <summary>
    /// Places and enables/disables the highlight blocks
    /// </summary>
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

    /// <summary>
    /// Checks if the player is standing where the placeHighlightBlock should be placed and therefore cannot place a block 
    /// </summary>
    /// <returns>True if the player can place a block, false otherwise</returns>
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

    /// <summary>
    /// Determines if the player camera is aiming at a block that is within reach
    /// </summary>
    /// <param name="startBlockPos">The starting block position</param>
    /// <returns>True if a solid block within reach was targeted, false otherwise</returns>
    private bool GetTargetedBlock(Vector3Int startBlockPos) {
        try {

            Vector3 startPos = cam.position;
            Vector3 endPos = startPos + cam.forward.normalized * reach;
            Vector3Int blockPos = Vector3Int.zero;

            bool hit = false;

            //Check the neighbors of the starting block on the x axis
            if (cam.forward.x > 0) {
                blockPos = startBlockPos + Vector3Int.right;
                hit = HelperFunctions.LiangBarsky3D(startPos, endPos, blockPos);
            } else if (cam.forward.x < 0) {
                blockPos = startBlockPos + Vector3Int.left;
                hit = HelperFunctions.LiangBarsky3D(startPos, endPos, blockPos);
            }

            if (hit) {
                //If a solid block was targeted, store its position and return true
                if (HelperFunctions.CheckBlockInPlanet(this.planet, blockPos.x, blockPos.y, blockPos.z)) {
                    breakPos = blockPos;
                    placePos = startBlockPos;
                    return true;
                } else { //The block was not solid, recurse with the block position as the new starting point
                    return GetTargetedBlock(blockPos);
                }
            }

            //Check the neighbors of the starting block on the y axis
            if (cam.forward.y > 0) {
                blockPos = startBlockPos + Vector3Int.up;
                hit = HelperFunctions.LiangBarsky3D(startPos, endPos, blockPos);
            } else if (cam.forward.y < 0) {
                blockPos = startBlockPos + Vector3Int.down;
                hit = HelperFunctions.LiangBarsky3D(startPos, endPos, blockPos);
            }

            if (hit) {
                //If a solid block was targeted, store its position and return true
                if (HelperFunctions.CheckBlockInPlanet(this.planet, blockPos.x, blockPos.y, blockPos.z)) {
                    breakPos = blockPos;
                    placePos = startBlockPos;
                    return true;
                } else { //The block was not solid, recurse with the block position as the new starting point
                    return GetTargetedBlock(blockPos);
                }
            }

            //Check the neighbors of the starting block on the z axis
            if (cam.forward.z > 0) {
                blockPos = startBlockPos + Vector3Int.forward;
                hit = HelperFunctions.LiangBarsky3D(startPos, endPos, blockPos);
            } else if (cam.forward.z < 0) {
                blockPos = startBlockPos + Vector3Int.back;
                hit = HelperFunctions.LiangBarsky3D(startPos, endPos, blockPos);
            }

            if (hit) {
                //If a solid block was targeted, store its position and return true
                if (HelperFunctions.CheckBlockInPlanet(this.planet, blockPos.x, blockPos.y, blockPos.z)) {
                    breakPos = blockPos;
                    placePos = startBlockPos;
                    return true;
                } else { //The block was not solid, recurse with the block position as the new starting point
                    return GetTargetedBlock(blockPos);
                }
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Player GetTargetedBlock: " + ex.Message);
        }

        //The starting block has no neighboring block that was hit
        //This means that the entirety of the player reach has been checked without hitting a solid block, so return false
        return false;
    }

    /// <summary>
    /// Changes the ID of a block at a given xyz coord
    /// </summary>
    /// <param name="blockPosInPlanet">The position of the block in the planet</param>
    /// <param name="blockID">The new ID of the block</param>
    private void EditBlock(Vector3Int blockPosInPlanet, BlockID blockID) {
        try {

            Chunk chunk = HelperFunctions.GetChunkFromBlockPos(this.planet, blockPosInPlanet.x, blockPosInPlanet.y, blockPosInPlanet.z);

            if (chunk == null || chunk.stage < ChunkStage.Rendered) {
                return;
            }

            Vector3Int blockPosInChunk = HelperFunctions.NormalizeBlockPos(blockPosInPlanet.x, blockPosInPlanet.y, blockPosInPlanet.z, Chunk.ChunkSize);

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

    /// <summary>
    /// Regenerates a chunk mesh when a block is placed or destroyed by a player
    /// </summary>
    /// <param name="chunkPos">The position of the chunk being edited</param>
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
}