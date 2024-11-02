using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    private const float TurnTime = 15f;
    private const float MinDistance = 2f;
    private GameObject[][] _foxes;
    private FoxController[][] _foxControllers;
    private const int FoxCount = 2;
    public float worldMinX;
    public float worldMaxX;
    public int currentPlayer;
    public int[] playerFoxIdx = new int[] { -1, -1 };
    private float _timer = TurnTime;
    public bool weaponFired;
    private bool _isGameOver;
    private TextMeshProUGUI _gameOverText;
    private TextMeshProUGUI _currentTurnText;
    private TextMeshProUGUI _timerText;

    private void Awake()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            throw new Exception("Main camera not found");
        }

        // Get the screen bounds in world coordinates
        var cameraHeight = 2f * mainCamera.orthographicSize;
        var cameraWidth = cameraHeight * mainCamera.aspect;

        // Calculate the edges
        this.worldMinX = (mainCamera.transform.position.x - cameraWidth / 2f);
        this.worldMaxX = (mainCamera.transform.position.x + cameraWidth / 2f);
        this.SpawnFoxes();
        this._gameOverText = GameObject.Find("GameOverText").GetComponent<TextMeshProUGUI>();
        this._currentTurnText = GameObject.Find("TurnText").GetComponent<TextMeshProUGUI>();
        this._timerText = GameObject.Find("TimerText").GetComponent<TextMeshProUGUI>();
    }

    private void SpawnFoxes()
    {
        var globalFoxCount = FoxCount * 2;
        var player1Foxes = new GameObject[FoxCount];
        var player2Foxes = new GameObject[FoxCount];
        var player1FoxControllers = new FoxController[FoxCount];
        var player2FoxControllers = new FoxController[FoxCount];
        var allFoxes = new GameObject[globalFoxCount];
        var foxPrefab = Resources.Load<GameObject>("Prefabs/Fox");
        for (var i = 0; i < globalFoxCount; i++)
        {
            Vector3 position;
            bool positionValid;
            do
            {
                positionValid = true;
                var randomX = Random.Range(this.worldMinX + 1, this.worldMaxX + 1);
                var randomY = 1;
                position = new Vector3(randomX, randomY, 0);

                // Check distance from other foxes
                foreach (var existingFox in allFoxes)
                {
                    if (existingFox != null && Vector3.Distance(existingFox.transform.position, position) < MinDistance)
                    {
                        positionValid = false;
                        break;
                    }
                }
            } while (!positionValid);

            var fox = Instantiate(foxPrefab);
            fox.transform.position = position;
            // flip the fox sprite for player 2
            if (i >= (FoxCount))
            {
                var foxSprite = fox.transform.Find("FoxSprite");
                foxSprite.transform.localScale = new Vector3(-1, 1, 1);
            }

            fox.tag = i < (FoxCount) ? "Player" : "Player2";
            var foxController = fox.GetComponent<FoxController>();
            if (i >= (FoxCount))
            {
                foxController.UpdateHealthBarSpriteForPlayer2();
            }

            allFoxes[i] = fox;
            if (i < (FoxCount))
            {
                player1Foxes[i] = fox;
                player1FoxControllers[i] = foxController;
                player1FoxControllers[i].foxIdx = i;
            }
            else
            {
                player2Foxes[i - (FoxCount)] = fox;
                player2FoxControllers[i - (FoxCount)] = foxController;
                player2FoxControllers[i - (FoxCount)].foxIdx = i - FoxCount;
            }
        }

        this._foxes = new GameObject[2][];
        this._foxes[0] = player1Foxes;
        this._foxes[1] = player2Foxes;
        this._foxControllers = new FoxController[2][];
        this._foxControllers[0] = player1FoxControllers;
        this._foxControllers[1] = player2FoxControllers;
    }

    private void Start()
    {
        _foxControllers[currentPlayer][++playerFoxIdx[currentPlayer]].isControllable = true;
        StartCoroutine(SwitchTurnAfterDelay());
        _currentTurnText.text = $"Current Turn: Player {currentPlayer + 1}";
        _timerText.text = $"Time Remaining: {TurnTime}s";
    }

    private int GetAliveFoxCount(int player)
    {
        return _foxControllers[player].Count(foxController => !foxController.isDead);
    }

    private int GetFoxIdx(int player)
    {
        var aliveFoxCount = GetAliveFoxCount(player);
        if (aliveFoxCount == 0) return -1;
        if (aliveFoxCount == 1) return _foxControllers[player].First(c => !c.isDead).foxIdx;
        var nextIdx = (playerFoxIdx[player] + 1) % FoxCount;
        var controller = _foxControllers[player][nextIdx];
        while (controller.isDead)
        {
            nextIdx = (nextIdx + 1) % FoxCount;
            controller = _foxControllers[player][nextIdx];
        }

        return nextIdx;
    }

    public void SwitchTurn()
    {
        // Disable current fox
        _foxControllers[currentPlayer][playerFoxIdx[currentPlayer]].ResetFoxForTurnSwitch();
        var currentPlayerFoxCount = GetAliveFoxCount(currentPlayer);
        if (currentPlayerFoxCount == 0)
        {
            this.GameOver(currentPlayer == 0 ? 1 : 0);
            return;
        }

        // Switch to next player
        currentPlayer = (currentPlayer + 1) % 2;
        var nextPlayerFoxCount = GetAliveFoxCount(currentPlayer);
        if (nextPlayerFoxCount == 0)
        {
            this.GameOver(currentPlayer == 0 ? 1 : 0);
            return;
        }

        var foxIdx = GetFoxIdx(currentPlayer);
        playerFoxIdx[currentPlayer] = foxIdx;
        // Enable next fox
        _foxControllers[currentPlayer][foxIdx].isControllable = true;
        _currentTurnText.text = $"Current Turn: Player {currentPlayer + 1}";

        // Reset timer
        this.weaponFired = false;
        _timer = TurnTime;
        this.UpdateTimerText(_timer);
    }

    private void UpdateTimerText(float time)
    {
        var seconds = (int)time;
        _timerText.text = $"Time Remaining: {seconds}s";
    }

    private void GameOver(int wonPlayer)
    {
        _isGameOver = true;
        _gameOverText.text = $"Player {wonPlayer + 1} won!\nPress R";

        // Disable all foxes
        foreach (var foxController in _foxControllers.SelectMany(controllers => controllers))
        {
            foxController.isControllable = false;
        }
    }

    private IEnumerator SwitchTurnAfterDelay()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            if (this.weaponFired) continue;
            
            this.UpdateTimerText(--_timer);
            if (_timer == 0)
            {
                this.SwitchTurn();
            }

            if (_isGameOver) yield break;
        }
    }

    private static void RestartGame()
    {
        // reload the scene
        var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene.name);
    }

    private void Update()
    {
        if (_isGameOver && Input.GetKeyUp(KeyCode.R))
        {
            GameController.RestartGame();
        }
    }
}