using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MCTS
{
    private Dictionary<string, Tuple<int, int>> nodeScores = new Dictionary<string, Tuple<int, int>>();
    public int TotalIterations { get; set; } = 0;
    public int gamesPlayed = 0;
    public int wins = 0;

    public int Search(GameModel root, int iterations)
    {
        if (!nodeScores.ContainsKey(root.GetState())){
            nodeScores.Add(root.GetState(), Tuple.Create<int, int>(1, 1));
        }
        int iteration = 0;
        int move = 0;
        while (iteration < iterations)
        {
            Stack<GameModel> path = Traverse(root);
            GameModel leaf = path.Peek();
            bool didRedWin = Rollout(leaf);
            move = Backpropagate(path, didRedWin);
            iteration++;
            TotalIterations++;
        }
        return move;
    }

    private int Backpropagate (Stack<GameModel> path, bool didRedWin)
    {
        int move = -1;

        while (path.Count > 0)
        {
            GameModel node = path.Pop();
            if (path.Count == 1) move = node.LastMove;
            if (node == null) throw new NullReferenceException("Found a null node in the path during backpropagation.");
            if (!nodeScores.ContainsKey(node.GetState())) throw new NullReferenceException("Found an unexplored node in the path during backpropagation.");
            Tuple<int, int> prevScore = nodeScores[node.GetState()];
            Tuple<int, int> newScore = null;
            if (didRedWin != node.IsRed) newScore = new Tuple<int, int>(prevScore.Item1 + 1, prevScore.Item2 + 1);
            else newScore = new Tuple<int, int>(prevScore.Item1, prevScore.Item2 + 1);
            nodeScores[node.GetState()] = newScore;
        }

        if (move == -1) throw new NullReferenceException("No new move was found in the path.");
        else return move;
    }

    private bool Rollout (GameModel node)
    {
        int depth = 0;
        while (!LeafNode(node))
        {
            if (node == null) throw new NullReferenceException("Found a null node at depth " + depth.ToString() + " during the rollout.");
            depth++;
            node = RolloutNode(node);
        }

        return node.RedWin;
    }

    private GameModel RolloutNode (GameModel node)
    {
        int[] moves = { 0, 1, 2, 3, 4, 5, 6 };
        System.Random rand = new System.Random();
        moves = moves.OrderBy(x => rand.Next()).ToArray();

        foreach (int move in moves)
            if (node.ValidMove(move))
            {
                GameModel child = node.Clone();
                child.AddPiece(move);
                return child;
            }

        throw new NullReferenceException("Did not find a valid child during a random rollout despite parent registering as a non-leaf node.");
    }

    private bool FullyExplored (GameModel node)
    {
        int[] moves = { 0, 1, 2, 3, 4, 5, 6 };

        if (node.GameOver) return false;

        foreach (int move in moves)
            if (node.ValidMove(move))
            {
                GameModel child = node.Clone();
                child.AddPiece(move);
                if (!nodeScores.ContainsKey(child.GetState())) return false;
            }

        return true;
    }

    private GameModel BestChild (GameModel node)
    {
        int[] moves = { 0, 1, 2, 3, 4, 5, 6 };

        GameModel bestChild = null;
        double bestScore = double.MinValue;

        foreach (int move in moves)
            if (node.ValidMove(move))
            {
                GameModel child = node.Clone();
                child.AddPiece(move);
                if (!nodeScores.ContainsKey(child.GetState())) throw new NullReferenceException("The child node has not yet been added to the scorecard despite registering as fully explored.");
                else 
                {
                    double score = GetScore(node, child);
                    if (score > bestScore)
                    {
                        bestChild = child;
                        bestScore = score;
                    }
                }
            }
        
        if (bestChild == null) 
            throw new NullReferenceException("No valid child was found despite registering as a non-leaf node.");
        else return bestChild;
    }

    private bool LeafNode (GameModel node)
    {
        return node.GameOver;
    }

    private GameModel Expand (GameModel node)
    {
        int[] moves = { 0, 1, 2, 3, 4, 5, 6 };
        System.Random rand = new System.Random();
        moves = moves.OrderBy(x => rand.Next()).ToArray();

        foreach (int move in moves)
            if (node.ValidMove(move))
            {
                GameModel child = node.Clone();
                child.AddPiece(move);
                if (!nodeScores.ContainsKey(child.GetState()))
                {
                    nodeScores.Add(child.GetState(), new Tuple<int, int>(0, 0));
                    return child;
                }
            }

        throw new NullReferenceException("No new child was found to expand despite the parent registering as not fully explored.");
    }

    private Stack<GameModel> Traverse(GameModel node)
    {
        Stack<GameModel> path = new Stack<GameModel>();

        while (FullyExplored(node))
        {
            path.Push(node);
            node = BestChild(node);
        }

        path.Push(node);
        if (!LeafNode(node)) path.Push(Expand(node));
        return path;
    }

    private double GetScore(GameModel parent, GameModel child)
    {
        Tuple<int, int> parentScore = nodeScores[parent.GetState()];
        Tuple<int, int> childScore = nodeScores[child.GetState()];

        double winRatio = childScore.Item1 / childScore.Item2;
        double visitRatio = Math.Log(parentScore.Item2) / childScore.Item2;
        double k = Math.Sqrt(2);

        return winRatio + k * Math.Sqrt(visitRatio);
    }

    public string GetStats()
    {
        return "Total Expanded: " + nodeScores.Count + " Total Iterations: " + TotalIterations + " Games Played: " + gamesPlayed + " Wins: " + wins;
    }
}
