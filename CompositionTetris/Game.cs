using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;

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
            _timer.SetTargetElapsedSeconds(16.0 / 1000);
            _timer.SetFixedTimeStep(true);

            InitializeBoard();
            SpawnPiece();            
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

            var isSpaceDownThisFrame = IsKeyDown(VirtualKey.Space);
            var isLeftDownThisFrame = IsKeyDown(VirtualKey.Left);
            var isRightDownThisFrame = IsKeyDown(VirtualKey.Right);

            if (_activeTiles != null)
            {
                if (_wasSpaceDownLastFrame && !isSpaceDownThisFrame)
                {
                    tilesToDrop = _boardHeight;
                    _secondsSinceLastDrop = 0;
                }

                int dx = 0;
                int dy = 0;

                if (_wasLeftDownLastFrame && !isLeftDownThisFrame)
                {
                    dx = -1;
                }

                if (_wasRightDownLastFrame && !isRightDownThisFrame)
                {
                    dx = 1;
                }

                TryMoveActivePiece(dx, 0);

                if (tilesToDrop > 0)
                {
                    dy = 1;
                    do
                    {
                        if (TryMoveActivePiece(0, dy))
                        {
                            tilesToDrop--;
                        }
                        else
                        {
                            _activeTiles = null;
                            break;
                        }
                    } while (tilesToDrop > 0);

                    if (_activeTiles == null)
                    {
                        CheckAndClearLines();
                    }
                }
            }
            else
            {
                SpawnPiece();
            }

            UpdateBoard();

            _wasSpaceDownLastFrame = isSpaceDownThisFrame;
            _wasLeftDownLastFrame = isLeftDownThisFrame;
            _wasRightDownLastFrame = isRightDownThisFrame;
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

        private bool SpawnPiece()
        {
            bool success = false;

            if (_activeTiles == null)
            {
                _activeTiles = new TilePosition[4];

                _activeTiles[0] = new TilePosition(3, 5);
                _activeTiles[1] = new TilePosition(3, 6);
                _activeTiles[2] = new TilePosition(4, 6);
                _activeTiles[3] = new TilePosition(4, 7);

                foreach (var position in _activeTiles)
                {
                    if (_board[position.X, position.Y])
                    {
                        _activeTiles = null;
                        break;
                    }
                }

                if (_activeTiles != null)
                {
                    foreach (var position in _activeTiles)
                    {
                        _board[position.X, position.Y] = true;
                    }
                }
            }

            return success;
        }

        private bool IsKeyDown(VirtualKey key)
        {
            var isDown = false;

            var window = CoreWindow.GetForCurrentThread();
            var state = window.GetKeyState(key);
            if (state.HasFlag(CoreVirtualKeyStates.Down))
            {
                isDown = true;
            }

            return isDown;
        }

        private bool TryMoveActivePiece(int dx, int dy)
        {
            var canMove = true;
            for (int i = 0; i < _activeTiles.Length; i++)
            {
                var position = _activeTiles[i];

                if (!CanMove(position.X, position.Y, dx, dy))
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
                    position.X += dx;
                    position.Y += dy;
                    _board[position.X, position.Y] = true;
                    _activeTiles[i] = position;
                }
            }

            return canMove;
        }

        private void CheckAndClearLines()
        {
            // Check each row
            for (int j = 0; j < _boardHeight; j++)
            {
                int piecesOnLine = 0;
                for (int i = 0; i < _boardWidth; i++)
                {
                    if (_board[i, j])
                    {
                        piecesOnLine++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (piecesOnLine == _boardWidth)
                {
                    // Clear the line
                    for (int i = 0; i < _boardWidth; i++)
                    {
                        _board[i, j] = false;
                    }

                    // Move everything above
                    for (int tempJ = j - 1; tempJ >= 0; tempJ--)
                    {
                        for (int i = 0; i < _boardWidth; i++)
                        {
                            _board[i, tempJ + 1] = _board[i, tempJ];
                        }
                    }
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
        private TilePosition[] _activeTiles;
        private double _tilesPerSecond;
        private double _secondsSinceLastDrop;
        private bool _wasSpaceDownLastFrame;
        private bool _wasLeftDownLastFrame;
        private bool _wasRightDownLastFrame;
    }
}
