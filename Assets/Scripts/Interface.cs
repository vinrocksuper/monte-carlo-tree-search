using UnityEngine;

public class Interface : MonoBehaviour
{
    // Game objects for instantiating and destroying game world tokens.
    public GameObject redToken;
    public GameObject yellowToken;
    public GameObject tokens;

    // Whether a human is playing or not.
    public bool human = true;

    // Controls training per frame and max training iterations.
    public int maxTrainPerFrame = 100;
    public float maxTrainThreshold = 3000000;

    // Internal matchmaking stats.
    private int aiWins = 0;
    private int playerWins = 0;

    // Magic numbers for click detection and instantiation.
    float[] xPositions = new float[] { -59.31f, -41.29f, -24.6f, -8.18f, 8.42f, 24.89f, 41.45f, 59.42f };
    float yBottom = -56.36f;
    float yTop = 43.3f;

    // Variables for game state tracking and control.
    private bool redTurn = true;
    private GameObject lastToken = null;
    private bool gameOver = false;
    private bool playerTurn = true;

    // The game model and MCTS agent.
    private GameModel model = new GameModel();
    private MCTS mcts = new MCTS();

    // Update is called once per frame
    void Update()
    {
        // While a game is currently being played...
        if (!gameOver)
        {
            // Initialize the number of training iterations for this frame to 1.
            int trainingIterations = 1;

            // If we have trained the MCTS agent less than the threshold.
            if (mcts.TotalIterations < maxTrainThreshold)
            {
                // Calculate the number of iterations to train the MCTS algorithm as a ratio of the current iterations to the max threshold.
                trainingIterations = maxTrainPerFrame * Mathf.RoundToInt((1 - ((float)mcts.TotalIterations / maxTrainThreshold)));

                // Cutoff the min iteration to a floor of 1.
                if (trainingIterations < 1) trainingIterations = 1;
            }

            // Run MCTS search for the number of training iterations.
            // This is done in the background and does not choose the next action.
            // It just updates the MCTS state dictionary based on its search.
            mcts.Search(model, trainingIterations);
        }

        // Check for player input.
        // Get the player's mouse position.
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Initialize a column position. -1 means off the side of the game board.
        int columnPosition = -1;

        // If the mouse position is within the game board magic numbers.
        if (mousePosition.y > yBottom && mousePosition.y < yTop)

            // Iterate through the possible column game world x positions.
            for (int i = 0; i < xPositions.Length; i++)

                // If our mouse position is within the range of the current magic number.
                if (mousePosition.x <= xPositions[i])
                {
                    // Record the column the player is pointing their mouse at.
                    columnPosition = i - 1;

                    // Break the for loop. We have found the column the player is hovering over.
                    break;
                }

        // Create a boolean to determine whether a token can be spawned.
        bool canSpawn = false;

        // If there is no previous token, we can spawn another.
        if (lastToken == null) canSpawn = true;
        else
        {
            // Otherwise, grab the rigidbody of the last token.
            Rigidbody rb = lastToken.GetComponentInChildren<Rigidbody>();

            // If the magnitude of the last token's velocity is less than 1 (i.e. it is not moving anymore)..
            // We can spawn another token.
            if (rb.velocity.magnitude < 1) canSpawn = true;
        }

        // If the player has clicked a valid column, a token can be spawned, the game is not over, the column is a valid move, it is the player's turn, and the game mode is human vs. AI...
        if (Input.GetMouseButtonDown(0) && columnPosition >= 0 && canSpawn && !gameOver && model.ValidMove(columnPosition) && playerTurn && human)
        {

            // Grab the appropriate yellow or red token prefab.
            GameObject currentToken = yellowToken;
            if (redTurn) currentToken = redToken;

            // Add the token to the game model and record whether the game is over.
            gameOver = model.AddPiece(columnPosition);

            // Instantiate the token at the appropriate column position.
            lastToken = Instantiate(currentToken, new Vector3(xPositions[columnPosition] + (Mathf.Abs(xPositions[columnPosition] - xPositions[columnPosition + 1]) / 2), 52f, 0f), Quaternion.identity);

            // Grab the instantiated token's rigidbody.
            Rigidbody rb = lastToken.GetComponentInChildren<Rigidbody>();

            // Go ahead and set the token velocity so it starts falling immediately.
            // This helps out our algorithm that checks whether the token has finished falling.
            rb.velocity = new Vector3(0f, -3f, 0f);

            // Set the token's parent to the token collection game object.
            lastToken.transform.parent = tokens.transform;

            // Flip the token color and player turn counters.
            redTurn = !redTurn;
            playerTurn = !playerTurn;

            // If the game is over...
            if (gameOver)
            {
                // Increment the player wins.
                playerWins++;

                // Increment the games played by the MCTS agent.
                mcts.gamesPlayed++;

                // Write the result to the log.
                Debug.Log("HUMAN WINS! Human: " + playerWins + " AI: " + aiWins + " MCTS Stats: " + mcts.GetStats());
            }
        }
        // Otherwise, if it is AI vs. AI, a new token can be dropped, the game is not over, and it is the "player" turn (the naive agent)...
        else if (canSpawn && !gameOver && playerTurn && !human)
        {

            // Grab the appropriate yellow or red token prefab.
            GameObject currentToken = yellowToken;
            if (redTurn) currentToken = redToken;

            // Flip the token color and player turn counters.
            redTurn = !redTurn;
            playerTurn = !playerTurn;

            // Create a new MCTS agent.
            // This agent will have no state dictionary and will choose an action at random.
            MCTS naiveMCTS = new MCTS();

            // Ask the naive AI to choose a move.
            int move = naiveMCTS.Search(model, 1);

            // Add the naive AI move to the game model and record whether the game is over.
            gameOver = model.AddPiece(move);

            // Instantiate the token at the appropriate column position.
            lastToken = Instantiate(currentToken, new Vector3(xPositions[move] + (Mathf.Abs(xPositions[move] - xPositions[move + 1]) / 2), 52f, 0f), Quaternion.identity);

            // Grab the instantiated token's rigidbody.
            Rigidbody rb = lastToken.GetComponentInChildren<Rigidbody>();

            // Go ahead and set the token velocity so it starts falling immediately.
            // This helps out our algorithm that checks whether the token has finished falling.
            rb.velocity = new Vector3(0f, -3f, 0f);

            // Set the token's parent to the token collection game object.
            lastToken.transform.parent = tokens.transform;

            // If the game is over...
            if (gameOver)
            {
                // Increment the "player" (naive AI) wins.
                playerWins++;

                // Increment the games played by the MCTS agent.
                mcts.gamesPlayed++;

                // Write the result to the log.
                Debug.Log("NAIVE MODEL WINS! Naive: " + playerWins + " Trained: " + aiWins + " Trained MCTS Stats: " + mcts.GetStats());
            }
        }
        // Otherwise, if a token can be placed, the game is not over, and it is the trained AI's turn...
        else if (canSpawn && !gameOver && !playerTurn)
        {
            // Grab the appropriate yellow or red token prefab.
            GameObject currentToken = yellowToken;
            if (redTurn) currentToken = redToken;

            // Flip the token color and player turn counters.
            redTurn = !redTurn;
            playerTurn = !playerTurn;

            // Ask the trained AI to choose a move.
            int move = mcts.Search(model, 1);

            // Add the AI move to the game model and record whether the game is over.
            gameOver = model.AddPiece(move);

            // Instantiate the token at the appropriate column position.
            lastToken = Instantiate(currentToken, new Vector3(xPositions[move] + (Mathf.Abs(xPositions[move] - xPositions[move + 1]) / 2), 52f, 0f), Quaternion.identity);

            // Grab the instantiated token's rigidbody.
            Rigidbody rb = lastToken.GetComponentInChildren<Rigidbody>();

            // Go ahead and set the token velocity so it starts falling immediately.
            // This helps out our algorithm that checks whether the token has finished falling.
            rb.velocity = new Vector3(0f, -3f, 0f);

            // Set the token's parent to the token collection game object.
            lastToken.transform.parent = tokens.transform;

            // If the game is over...
            if (gameOver)
            {
                // Increment the AI wins.
                aiWins++;

                // Increment the games played by the MCTS agent.
                mcts.gamesPlayed++;

                // Increment the wins by the MCTS agent.
                mcts.wins++;

                // Write the result to the log.
                if (human) Debug.Log("AI WINS! Human: " + playerWins + " AI: " + aiWins + " MCTS Stats: " + mcts.GetStats());
                else Debug.Log("TRAINED MODEL WINS! Naive: " + playerWins + " Trained: " + aiWins + " Trained MCTS Stats: " + mcts.GetStats());
            }
        }
        // Reset the game if game over.
        else if (Input.GetMouseButtonDown(0) && columnPosition >= 0 && canSpawn && gameOver && human) ResetGame();
        else if (canSpawn && gameOver && !human) ResetGame();
    }

    // Resets the game.
    private void ResetGame()
    {
        // Destroy all the tokens.
        foreach (Transform child in tokens.transform)
            GameObject.Destroy(child.gameObject);

        // Initialize the state trackers.
        gameOver = false;
        redTurn = true;

        // Create a new game model.
        model = new GameModel();
    }
}
