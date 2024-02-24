using System;
using TMPro;
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
    private float playerRadius = .25f;
    private float playerHeight = 1.8f;

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
                hit = LiangBarsky3D(startPos, endPos, blockPos);
            } else if (cam.forward.x < 0) {
                blockPos = startBlockPos + Vector3Int.left;
                hit = LiangBarsky3D(startPos, endPos, blockPos);
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
                hit = LiangBarsky3D(startPos, endPos, blockPos);
            } else if (cam.forward.y < 0) {
                blockPos = startBlockPos + Vector3Int.down;
                hit = LiangBarsky3D(startPos, endPos, blockPos);
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
                hit = LiangBarsky3D(startPos, endPos, blockPos);
            } else if (cam.forward.z < 0) {
                blockPos = startBlockPos + Vector3Int.back;
                hit = LiangBarsky3D(startPos, endPos, blockPos);
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

    /// <summary>
    /// Liang Barsky line clipping algorithm extrapolated to 3D
    /// Determines if a ray intersects a block at a given position
    /// See: https://en.wikipedia.org/wiki/Liang%E2%80%93Barsky_algorithm
    /// </summary>
    /// <param name="pos1">The camera position</param>
    /// <param name="pos2">The camera position + reach</param>
    /// <param name="blockPos">The block to check for an intersection</param>
    /// <returns>True/False</returns>
    public bool LiangBarsky3D(Vector3 pos1, Vector3 pos2, Vector3Int blockPos) {
        try {

            int xMin = blockPos.x;
            int yMin = blockPos.y;
            int zMin = blockPos.z;
            int xMax = xMin + 1;
            int yMax = yMin + 1;
            int zMax = zMin + 1;

            float p1 = -(pos2.x - pos1.x);
            float p2 = -p1;
            float p3 = -(pos2.y - pos1.y);
            float p4 = -p3;
            float p5 = -(pos2.z - pos1.z);
            float p6 = -p5;

            float q1 = pos1.x - xMin;
            float q2 = xMax - pos1.x;
            float q3 = pos1.y - yMin;
            float q4 = yMax - pos1.y;
            float q5 = pos1.z - zMin;
            float q6 = zMax - pos1.z;

            if ((p1 == 0 && q1 < 0) || (p2 == 0 && q2 < 0) || (p3 == 0 && q3 < 0) || (p4 == 0 && q4 < 0) || (p5 == 0 && q5 < 0) || (p6 == 0 && q6 < 0)) {
                return false;
            }

            float min = 1;
            float max = 0;

            LiangBarsky3DHelper(p1, p2, q1, q2, ref min, ref max);
            LiangBarsky3DHelper(p3, p4, q3, q4, ref min, ref max);
            LiangBarsky3DHelper(p5, p6, q5, q6, ref min, ref max);

            return max <= min;

        } catch (Exception ex) {
            Debug.LogError("Error in Player LiangBarsky3D: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Helper function for the repetitive portion of LiangBarsky3D
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="q1"></param>
    /// <param name="q2"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    private void LiangBarsky3DHelper(float p1, float p2, float q1, float q2, ref float min, ref float max) {
        try {

            if (p1 != 0) {
                float r1 = q1 / p1;
                float r2 = q2 / p2;

                if (p1 < 0) {
                    max = Mathf.Max(max, r1);
                    min = Mathf.Min(min, r2);
                } else {
                    max = Mathf.Max(max, r2);
                    min = Mathf.Min(min, r1);
                }
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Player LiangBarsky3DHelper: " + ex.Message);
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
            
            if (velocity.y < 0) {
                if (CheckBottom()) {
                    velocity.y = 0;
                }
            } else if (velocity.y > 0) {
                if (CheckTop()) {
                    velocity.y = 0;
                    verticalMomentum = 0;
                }
            }

            Vector3 nextPosition = this.currentPosition + velocity;

            if (isGrounded) {       
                nextPosition.y = Mathf.Clamp(nextPosition.y, nextPosition.y - 1, Mathf.Floor(transform.position.y));
            }

            //Check if running into any blocks in all four directions
            if (velocity.z > 0) {
                if (CheckSide(VoxelData.BlockFace.Front)) {
                    velocity.z = 0;
                    nextPosition.z = Mathf.FloorToInt(nextPosition.z + playerRadius) - playerRadius;
                }
            } else if (velocity.z < 0) {
                if (CheckSide(VoxelData.BlockFace.Back)) {
                    velocity.z = 0;
                    nextPosition.z = Mathf.CeilToInt(nextPosition.z - playerRadius) + playerRadius;
                }
            }

            if (velocity.x > 0) {
                if (CheckSide(VoxelData.BlockFace.Left)) {
                    velocity.x = 0;
                    nextPosition.x = Mathf.FloorToInt(nextPosition.x + playerRadius) - playerRadius;
                }
            } else if (velocity.x < 0) {
                if (CheckSide(VoxelData.BlockFace.Right)) {
                    velocity.x = 0;
                    nextPosition.x = Mathf.CeilToInt(nextPosition.x - playerRadius) + playerRadius;
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

            if (chunk == null) {
                return;
            }

            Vector3Int blockPosInChunk = HelperFunctions.GetNormalizedBlockPosInChunk(blockPosInPlanet);

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

            chunkPos = HelperFunctions.GetNormalizedChunkPos(this.planet, chunkPos);
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

    private bool CheckBottom() {
        try {

            int y = Mathf.FloorToInt(transform.position.y + velocity.y);
            int minX = Mathf.FloorToInt(transform.position.x - playerRadius);
            int maxX = Mathf.FloorToInt(transform.position.x + playerRadius);
            int minZ = Mathf.FloorToInt(transform.position.z - playerRadius);
            int maxZ = Mathf.FloorToInt(transform.position.z + playerRadius);

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

    private bool CheckTop() {
        try {

            int y = Mathf.FloorToInt(transform.position.y + playerHeight + velocity.y);
            int minX = Mathf.FloorToInt(transform.position.x - playerRadius);
            int maxX = Mathf.FloorToInt(transform.position.x + playerRadius);
            int minZ = Mathf.FloorToInt(transform.position.z - playerRadius);
            int maxZ = Mathf.FloorToInt(transform.position.z + playerRadius);

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

            int minY = Mathf.FloorToInt(currentPosition.y);
            int maxY = Mathf.CeilToInt(currentPosition.y + playerHeight - 1);
            int x, z;

            switch (side) {
                case VoxelData.BlockFace.Left:
                    x = Mathf.FloorToInt(currentPosition.x + velocity.x + playerRadius);
                    z = Mathf.FloorToInt(currentPosition.z);
                    break;
                case VoxelData.BlockFace.Right:
                    x = Mathf.FloorToInt(currentPosition.x + velocity.x - playerRadius);
                    z = Mathf.FloorToInt(currentPosition.z);
                    break;
                case VoxelData.BlockFace.Front:
                    x = Mathf.FloorToInt(currentPosition.x);
                    z = Mathf.FloorToInt(currentPosition.z + velocity.z + playerRadius);
                    break;
                case VoxelData.BlockFace.Back:
                    x = Mathf.FloorToInt(currentPosition.x);
                    z = Mathf.FloorToInt(currentPosition.z + velocity.z - playerRadius);
                    break;
                default:
                    return true;
            }

            for (int y = minY; y <= maxY; y++) {
                if (HelperFunctions.CheckBlockInPlanet(planet, x, y, z)) {
                    return true;
                }
            }

        } catch (Exception ex) {
            Debug.LogError("Error in Player CheckSide: " + ex.Message);
        }

        return false;
    }
}