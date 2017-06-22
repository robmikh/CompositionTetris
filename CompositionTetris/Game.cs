using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Composition;

namespace CompositionTetris
{
    class Game
    {
        public Game(ContainerVisual visual)
        {
            _compositor = visual.Compositor;
            _gameRoot = _compositor.CreateContainerVisual();
            _contentRoot = _compositor.CreateContainerVisual();
            _background = _compositor.CreateSpriteVisual();

            _gameRoot.RelativeSizeAdjustment = Vector2.One;
            _contentRoot.RelativeSizeAdjustment = Vector2.One;
            _background.RelativeSizeAdjustment = Vector2.One;
            _background.Brush = _compositor.CreateColorBrush(Colors.White);

            visual.Children.InsertAtTop(_gameRoot);
            _gameRoot.Children.InsertAtBottom(_background);
            _gameRoot.Children.InsertAtTop(_contentRoot);

            _timer = new StepTimer();

            InitializeBoard();
            _board[3, 5] = true;
        }

        public bool Update()
        {
            bool quit = false;

            _timer.Tick(() =>
            {
                var elapsedSeconds = _timer.GetElapsedSeconds();

                quit = UpdateInternal(elapsedSeconds);
            });

            return quit;
        }

        private bool UpdateInternal(double elapsedSeconds)
        {
            UpdateBoard();

            return false;
        }

        private void InitializeBoard()
        {
            _boardWidth = 10;
            _boardHeight = 16;
            _tileSize = new Vector2(64);

            _boardVisual = _compositor.CreateSpriteVisual();
            _boardVisual.Size = new Vector2(_boardWidth, _boardHeight) * _tileSize;
            _boardVisual.Brush = _compositor.CreateColorBrush(Colors.Black);
            _boardVisual.AnchorPoint = new Vector2(0.5f);
            _boardVisual.RelativeOffsetAdjustment = new Vector3(0.5f);

            _board = new bool[_boardWidth, _boardHeight];
            _tileVisuals = new SpriteVisual[_boardWidth, _boardHeight];
            for (int i = 0; i < _boardWidth; i++)
            {
                for (int j = 0; j < _boardHeight; j++)
                {
                    _board[i, j] = false;

                    var visual = _compositor.CreateSpriteVisual();
                    visual.Size = _tileSize;
                    visual.Offset = new Vector3(i * _tileSize.X, j * _tileSize.Y, 0);
                    _tileVisuals[i, j] = visual;

                    _boardVisual.Children.InsertAtTop(visual);
                }
            }

            _tileBrush = _compositor.CreateColorBrush(Colors.Red);

            _contentRoot.Children.InsertAtTop(_boardVisual);
        }

        private void UpdateBoard()
        {
            for (int i = 0; i < _boardWidth; i++)
            {
                for (int j = 0; j < _boardHeight; j++)
                {
                    var visual = _tileVisuals[i, j];
                    visual.Brush = _board[i, j] ? _tileBrush : null;
                }
            }
        }

        private Compositor _compositor;
        private ContainerVisual _gameRoot;
        private ContainerVisual _contentRoot;
        private SpriteVisual _background;

        private StepTimer _timer;

        private bool[,] _board;
        private int _boardWidth;
        private int _boardHeight;
        private SpriteVisual _boardVisual;
        private Vector2 _tileSize;
        private SpriteVisual[,] _tileVisuals;
        private CompositionColorBrush _tileBrush;
    }
}
