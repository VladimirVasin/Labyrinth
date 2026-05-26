using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Base
{
    public sealed class BaseView : MonoBehaviour
    {
        public MazeGenerationResult GenerationResult { get; private set; }

        public void Configure(MazeGenerationResult generationResult)
        {
            GenerationResult = generationResult;
        }
    }
}
