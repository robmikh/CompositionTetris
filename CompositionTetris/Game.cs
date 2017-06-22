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
            _timer.SetTargetElapsedSeconds(16 / 1000);

            InitializeBoard();
            _board[3, 5] = true;
            _board[3, 6] = true;
            _board[4, 6] = true;
            _board[4, 7] = true;

            _activeTiles[0] = new TilePosition(3, 5);
            _activeTiles[1] = new TilePosition(3, 6);
            _activeTiles[2] = new TilePosition(4, 6);
            _activeTiles[3] = new TilePosition(4, 7);
            _tilesPerSecond = 0.5;
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
            _secondsSinceLastDrop += elapsedSeconds;
            var tilesToDrop = (int)(_secondsSinceLastDrop * _tilesPerSecond);
            _secondsSinceLastDrop -= tilesToDrop / _tilesPerSecond;


            while (tilesToDrop > 0)
            {
                var canMove = true;
                for (int i = 0; i < _activeTiles.Length; i++)
                {
                    var position = _activeTiles[i];

                    if (!CanMoveDown(position))
                    {
                        canMove = false;
                        break;
                    }
                }

                if (canMove)
                {
                    for (int i = 0; i < _activeTiles.Length; i++)
                    {
                        var position = _activeTiles[i];
                        _board[position.X, position.Y] = false;
                    }

                    for (int i = 0; i < _activeTiles.Length; i++)
                    {
                        var position = _activeTiles[i];
                        position.Y++;
                        _board[position.X, position.Y] = true;
                        _activeTiles[i] = position;
                    }
                }

                if (canMove)
                {
                    tilesToDrop--;
                }
                else
                {
                    break;
                }
            }

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

            _activeTiles = new TilePosition[4];
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

        private bool CanMoveDown(int x, int y)
        {
            return CanMove(x, y, 0, 1);
        }

        private bool CanMoveDown(TilePosition tilePosition)
        {
            return CanMoveDown(tilePosition.X, tilePosition.Y);
        }

        private bool CanMove(int x, int y, int dx, int dy)
        {
            var canMove = false;

            var newX = x + dx;
            var newY = y + dy;

            foreach (var position in _activeTiles)
            {
                if (position.X == newX &&
                    position.Y == newY)
                {
                    canMove = true;
                }
            }

            if (!canMove)
            {
                if ((newX >= 0 && newX < _boardWidth) &&
                (newY >= 0 && newY < _boardHeight))
                {
                    canMove = !_board[newX, newY];
                }
            }

            return canMove;
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
        private TilePosition[] _activeTiles;
        private double _tilesPerSecond;
        private double _secondsSinceLastDrop;
    }
}
