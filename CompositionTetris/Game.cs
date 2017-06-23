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
            TrySpawnPiece();            
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
            var isUpDownThisFrame = IsKeyDown(VirtualKey.Up);
            var isDownDownThisFrame = IsKeyDown(VirtualKey.Down);

            if (!_lost)
            {
                if (_activeTiles != null)
                {
                    int dx = 0;
                    int dy = 0;

                    if (_wasUpDownLastFrame && !isUpDownThisFrame)
                    {
                        tilesToDrop = _boardHeight;
                        _secondsSinceLastDrop = 0;
                    }

                    if (_wasSpaceDownLastFrame && !isSpaceDownThisFrame)
                    {
                        TryRotateActivePiece(false);
                    }

                    if (_wasDownDowmLastFrame && !isDownDownThisFrame)
                    {
                        dy = 1;
                    }

                    if (_wasLeftDownLastFrame && !isLeftDownThisFrame)
                    {
                        dx = -1;
                    }

                    if (_wasRightDownLastFrame && !isRightDownThisFrame)
                    {
                        dx = 1;
                    }

                    TryMoveActivePiece(dx, 0);
                    TryMoveActivePiece(0, dy);

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
                    var success = TrySpawnPiece();
                    if (!success)
                    {
                        _tileBrush.Color = Colors.LightGray;
                        _lost = true;
                    }
                }
            }
            else
            {
                if (_wasSpaceDownLastFrame && !isSpaceDownThisFrame)
                {
                    for (int i = 0; i < _boardWidth; i++)
                    {
                        for (int j = 0; j < _boardHeight; j++)
                        {
                            _board[i, j] = false;
                        }
                    }

                    _lost = false;
                    _tileBrush.Color = Colors.Red;
                }
            }

            UpdateBoard();

            _wasSpaceDownLastFrame = isSpaceDownThisFrame;
            _wasLeftDownLastFrame = isLeftDownThisFrame;
            _wasRightDownLastFrame = isRightDownThisFrame;
            _wasUpDownLastFrame = isUpDownThisFrame;
            _wasDownDowmLastFrame = isDownDownThisFrame;
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

        private bool CanMove(int x, int y, int dx, int dy, bool ignoreBounds)
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
                else if (ignoreBounds)
                {
                    canMove = true;
                }
            }

            return canMove;
        }

        private bool TrySpawnPiece()
        {
            bool success = false;

            if (_activeTiles == null)
            {
                var startPosition = new TilePosition(_boardWidth / 2, 0);
                var pieceTemplate = GetRandomPieceTemplate();
                _activeTiles = new TilePosition[pieceTemplate.Length];

                for (int i = 0; i < _activeTiles.Length; i++)
                {
                    _activeTiles[i] = pieceTemplate[i] + startPosition;
                }

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
                    success = true;
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
            var newPiece = TryMovePiece(_activeTiles, dx, dy);
            var canMove = newPiece != null;

            if (canMove)
            {
                for (int i = 0; i < _activeTiles.Length; i++)
                {
                    var position = _activeTiles[i];
                    _board[position.X, position.Y] = false;
                }

                for (int i = 0; i < newPiece.Length; i++)
                {
                    var position = newPiece[i];
                    _board[position.X, position.Y] = true;
                }

                _activeTiles = newPiece;
            }

            return canMove;
        }

        private TilePosition[] TryMovePiece(TilePosition[] piece, int dx, int dy, bool ignoreBounds = false)
        {
            var newPiece = new TilePosition[piece.Length];
            var canMove = true;

            for (int i = 0; i < piece.Length; i++)
            {
                var position = piece[i];

                if (!CanMove(position.X, position.Y, dx, dy, ignoreBounds))
                {
                    canMove = false;
                    break;
                }
            }

            if (canMove)
            {
                for (int i = 0; i < piece.Length; i++)
                {
                    var position = piece[i];
                    position.X += dx;
                    position.Y += dy;
                    newPiece[i] = position;
                }
            }
            else
            {
                newPiece = null;
            }

            return newPiece;

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

        private bool TryRotateActivePiece(bool left)
        {
            var success = false;

            var longestDistance = float.MinValue;
            var centerPoint = new Vector2();
            for (int i = 0; i < _activeTiles.Length; i++)
            {
                var position = _activeTiles[i];

                foreach (var otherPosition in _activeTiles)
                {
                    var distance = TilePosition.Distance(position, otherPosition);
                    if (distance > longestDistance)
                    {
                        longestDistance = distance;
                        centerPoint = new Vector2(position.X + otherPosition.X, position.Y + otherPosition.Y);
                        centerPoint /= 2;
                    }
                }
            }

            var angle = (float)(Math.PI / 2.0);
            if (left)
            {
                angle *= -1.0f;
            }
            var rotation = Matrix3x2.CreateRotation(angle, centerPoint);
            var newActiveTiles = new TilePosition[_activeTiles.Length];

            for (int i = 0; i < _activeTiles.Length; i++)
            {
                var position = _activeTiles[i];
                var temp = Vector2.Transform(new Vector2(position.X, position.Y), rotation);
                var newPosition = new TilePosition((int)Math.Ceiling(temp.X), (int)Math.Ceiling(temp.Y));

                newActiveTiles[i] = newPosition;
            }

            var withinBounds = true;
            var farthestLeft = int.MaxValue;
            var farthestRight = int.MinValue;
            var farthestTop = int.MaxValue;
            var farthestBottom = int.MinValue;
            for (int i = 0; i < newActiveTiles.Length; i++)
            {
                var newPosition = newActiveTiles[i];

                if (newPosition.X < farthestLeft)
                {
                    farthestLeft = newPosition.X;
                }
                if (newPosition.X > farthestRight)
                {
                    farthestRight = newPosition.X;
                }

                if (newPosition.Y < farthestTop)
                {
                    farthestTop = newPosition.Y;
                }
                if (newPosition.Y > farthestBottom)
                {
                    farthestBottom = newPosition.Y;
                }

                if (!((newPosition.X >= 0 && newPosition.X < _boardWidth) && (newPosition.Y >= 0 && newPosition.Y < _boardHeight)))
                {
                    withinBounds = false;
                }
            }

            if (!withinBounds)
            {
                int dx = 0;
                int dy = 0;

                if (farthestLeft != int.MaxValue && farthestLeft < 0)
                {
                    dx = farthestLeft * -1;
                }
                else if (farthestRight != int.MinValue && farthestRight >= _boardWidth)
                {
                    dx = farthestRight - _boardWidth - 1;
                }

                if (farthestTop != int.MaxValue && farthestTop < 0)
                {
                    dy = farthestTop * -1;
                }
                else if (farthestBottom != int.MinValue && farthestBottom >= _boardHeight)
                {
                    dy = farthestBottom - _boardHeight - 1;
                }

                while (dx != 0)
                {
                    var newPiece = TryMovePiece(newActiveTiles, dx < 0 ? -1 : 1, 0, true);
                    if (newPiece != null)
                    {
                        newActiveTiles = newPiece;
                        dx += dx < 0 ? 1 : -1;
                    }
                    else
                    {
                        break;
                    }
                }

                while (dy != 0)
                {
                    var newPiece = TryMovePiece(newActiveTiles, 0, dy < 0 ? -1 : 1, true);
                    if (newPiece != null)
                    {
                        newActiveTiles = newPiece;
                        dy += dy < 0 ? 1 : -1;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            
            foreach (var newPosition in newActiveTiles)
            {
                var overlap = false;
                foreach (var position in _activeTiles)
                {
                    if (newPosition == position)
                    {
                        overlap = true;
                        break;
                    }
                }

                if (!overlap && _board[newPosition.X, newPosition.Y])
                {
                    newActiveTiles = null;
                    break;
                }
            }

            if (newActiveTiles != null)
            {
                foreach (var pos in _activeTiles)
                {
                    _board[pos.X, pos.Y] = false;
                }

                foreach (var pos in newActiveTiles)
                {
                    _board[pos.X, pos.Y] = true;
                }

                _activeTiles = newActiveTiles;
                success = true;
            }

            return success;
        }

        private static TilePosition[] GetRandomPieceTemplate()
        {
            var index = s_random.Next(0, PieceTemplates.Length);

            return PieceTemplates[index];
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
        private bool _wasUpDownLastFrame;
        private bool _wasDownDowmLastFrame;
        private bool _lost;

        private static Random s_random = new Random();
        private static TilePosition[] SPieceTemplate =
        {
            new TilePosition(0, 0),
            new TilePosition(0, 1),
            new TilePosition(1, 1),
            new TilePosition(1, 2)
        };
        private static TilePosition[] ZPieceTemplate =
        {
            new TilePosition(1, 0),
            new TilePosition(1, 1),
            new TilePosition(0, 1),
            new TilePosition(0, 2)
        };
        private static TilePosition[] SquareTemplate =
        {
            new TilePosition(0, 0),
            new TilePosition(1, 0),
            new TilePosition(0, 1),
            new TilePosition(1, 1)
        };
        private static TilePosition[] LPieceTemplate =
        {
            new TilePosition(0, 0),
            new TilePosition(0, 1),
            new TilePosition(0, 2),
            new TilePosition(1, 2)
        };
        private static TilePosition[] ReverseLPieceTemplate =
        {
            new TilePosition(0, 0),
            new TilePosition(0, 1),
            new TilePosition(0, 2),
            new TilePosition(-1, 2)
        };
        private static TilePosition[] LinePieceTemplate =
        {
            new TilePosition(0, 0),
            new TilePosition(0, 1),
            new TilePosition(0, 2),
            new TilePosition(0, 3),
        };
        private static TilePosition[] TPieceTemplate =
        {
            new TilePosition(0, 0),
            new TilePosition(1, 0),
            new TilePosition(2, 0),
            new TilePosition(1, 1)
        };
        private static TilePosition[][] PieceTemplates =
        {
            SPieceTemplate,
            ZPieceTemplate,
            SquareTemplate,
            LPieceTemplate,
            ReverseLPieceTemplate,
            LinePieceTemplate,
            TPieceTemplate
        };
    }
}
