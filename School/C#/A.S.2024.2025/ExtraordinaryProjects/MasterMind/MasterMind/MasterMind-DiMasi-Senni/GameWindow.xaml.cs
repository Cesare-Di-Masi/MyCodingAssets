using MastermindLib;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MasterMind_DiMasi_Senni
{
    public partial class GameWindow : Window
    {
        private int startVertical = 89;
        private int startHorizontal = 445;
        private int _tipsSize = 86;

        private GameManager currentGame;
        private CodeBreaker currentCodeBreaker;
        private List<Button> selectColoursList = new List<Button>();
        private List<List<Ellipse>> allAttempts = new List<List<Ellipse>>();
        private List<List<Button>> allTips = new List<List<Button>>();

        private int _maxWidth = 335;
        private int _maxHeight = 491;
        private int _currentAttempt = 0;
        private int _rectangleHeight, _rectangleSpacing;
        private int _buttonSize, _buttonSpacing;
        private int _currentPosVertical = 89;
        private int _currentPosHorizontal = 445;
        private int solveCodeVerticalPosition = 20;
        private int solveCodeHorizontalPosition = 445;
        private int solveCodeSize = 20;

        private Canvas _gameCanvas;

        public GameWindow(GameManager game)
        {
            InitializeComponent();

            this.SizeToContent = SizeToContent.WidthAndHeight;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            currentGame = game;
            _gameCanvas = new Canvas
            {
                Width = this.Width,
                Height = this.Height,
                Background = Brushes.Transparent // Mantieni lo sfondo trasparente
            };

            allAttempts = new List<List<Ellipse>>(currentGame.NAttempts);
            allTips = new List<List<Button>>(currentGame.NAttempts);

            for (int i = 0; i < game.NAttempts; i++)
            {
                allAttempts.Add(new List<Ellipse>(game.CodeLength));
                allTips.Add(new List<Button>(3));
            }

            selectColoursList = new List<Button>(currentGame.CodeLength);

            calculateRectangle();
            calculateButton();
            generateCodeSolver();

            // Assicura che _gameCanvas venga rimosso dal genitore precedente
            if (_gameCanvas.Parent != null)
            {
                ((Panel)_gameCanvas.Parent).Children.Remove(_gameCanvas);
            }

            // Controlla che _gameCanvas non sia già presente prima di aggiungerlo
            if (!this.gridGame.Children.Contains(_gameCanvas))
            {
                this.gridGame.Children.Add(_gameCanvas);
            }

            currentCodeBreaker = new CodeBreaker(game.NColours);

            this.Show();
        }

        public void calculateRectangle()
        {
            _rectangleHeight = _maxHeight / (currentGame.NAttempts + 1);
            _rectangleSpacing = _rectangleHeight / (currentGame.NAttempts + 1);
        }

        public void calculateButton()
        {
            _buttonSize = _maxWidth / (currentGame.CodeLength + 1);
            _buttonSpacing = _buttonSize / (currentGame.CodeLength + 1);
            if (_buttonSize > _rectangleHeight)
            {
                _buttonSize = _rectangleHeight;
                _buttonSpacing = _buttonSize / (currentGame.CodeLength + 1);
            }
        }

        private void btnMandaCombinazione_Click(object sender, RoutedEventArgs e)
        {
            newTurn();
        }

        private void newTurn()
        {
            GameStatus status = currentGame.EndOfTheTurn(buttonToCode());

            if (status == GameStatus.Playing)
            {
                System.Windows.Point ellipsePoint = new System.Windows.Point(_currentPosHorizontal, _currentPosVertical);
                System.Windows.Point tipsPoint = new System.Windows.Point(30, _currentPosVertical);

                // Generate the row rectangle
                generateRectangle(ellipsePoint);

                for (int i = 0; i < currentGame.CodeLength; i++)
                {
                    // Generate the new row of ellipses (previous attempt)
                    allAttempts[_currentAttempt].Add(generateEllipse(ellipsePoint, selectColoursList[i], _currentAttempt, i));
                    ellipsePoint.X += _buttonSpacing + _buttonSize; // Move right for next circle
                }

                for (int i = 0; i < 3; i++)
                {
                    tipsPoint.X += 86 * i;
                    generateTips(i, tipsPoint);
                }

                _currentPosVertical -= _rectangleSpacing - _rectangleHeight; // Move to next row for future attempt
                _currentAttempt++; // Increase the attempt counter
            }
            else
            {
                // Disable selection buttons when game ends
                foreach (var button in selectColoursList)
                {
                    button.IsEnabled = false;
                }

                System.Windows.Point solutionPoint = new System.Windows.Point(solveCodeHorizontalPosition, solveCodeVerticalPosition);
                Colours[] solution = currentGame.GiveColourCode();

                for (int i = 0; i < currentGame.CodeLength; i++)
                {
                    generateEllipse(solutionPoint, solution[i]); // Display the correct solution
                    solutionPoint.X += _buttonSpacing;
                }
            }
        }

        public void generateCodeSolver()
        {
            for (int i = 0; i < currentGame.CodeLength; i++)
            {
                Button button = generateButton(i, new Point(_currentPosHorizontal, _currentPosVertical));
                selectColoursList.Add(button);
                ShowButtonColours(button);

                _currentPosHorizontal += _buttonSpacing + _buttonSize;
            }
            _currentPosHorizontal = startHorizontal;
        }

        private Colours[] buttonToCode()
        {
            Colours[] code = new Colours[currentGame.CodeLength];
            for (int i = 0; i < currentGame.CodeLength; i++)
            {
                if (int.TryParse(selectColoursList[i].Content.ToString(), out int colorValue))
                    code[i] = (Colours)colorValue;
                else
                    code[i] = Colours.Red;
            }
            return code;
        }

        private void ChangeColour(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn)
            {
                if (btn.Content == null)
                {
                    btn.Content = "1"; // Ensure content is initialized
                }

                if (int.TryParse(btn.Content.ToString(), out int curr))
                {
                    Colours cl = Colours.Red + curr; // Get current color

                    // Check mouse button type
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        currentCodeBreaker.NextColour(ref cl); // Left Click → Next Color
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        currentCodeBreaker.PreviousColour(ref cl); // Right Click → Previous Color
                    }

                    btn.Content = ((int)cl).ToString(); // Update button content
                }
                ShowButtonColours(btn); // Apply new color to UI
            }
        }

        private void ShowButtonColours(Button btn)
        {
            int cur = int.Parse((string)btn.Content);
            switch (cur)
            {
                case 0:
                    btn.Background = Brushes.Red;
                    break;

                case 1:
                    btn.Background = Brushes.Blue;
                    break;

                case 2:
                    btn.Background = Brushes.Green;
                    break;

                case 3:
                    btn.Background = Brushes.Yellow;
                    break;

                case 4:
                    btn.Background = Brushes.Purple;
                    break;

                case 5:
                    btn.Background = Brushes.Orange;
                    break;

                case 6:
                    btn.Background = Brushes.Pink;
                    break;

                case 7:
                    btn.Background = Brushes.Cyan;
                    break;

                case 8:
                    btn.Background = Brushes.Brown;
                    break;

                case 9:
                    btn.Background = new SolidColorBrush(Color.FromRgb(255, 36, 0));
                    break;

                case 10:
                    btn.Background = Brushes.Teal;
                    break;

                case 11:
                    btn.Background = Brushes.Indigo;
                    break;

                default:
                    btn.Background = Brushes.Black;
                    break;
            }
        }

        private void ShowEllipseColour(Ellipse ell, int colour)
        {
            switch (colour)
            {
                case 0:
                    ell.Fill = Brushes.Red;
                    break;

                case 1:
                    ell.Fill = Brushes.Blue;
                    break;

                case 2:
                    ell.Fill = Brushes.Green;
                    break;

                case 3:
                    ell.Fill = Brushes.Yellow;
                    break;

                case 4:
                    ell.Fill = Brushes.Purple;
                    break;

                case 5:
                    ell.Fill = Brushes.Orange;
                    break;

                case 6:
                    ell.Fill = Brushes.Pink;
                    break;

                case 7:
                    ell.Fill = Brushes.Cyan;
                    break;

                case 8:
                    ell.Fill = Brushes.Brown;
                    break;

                case 9:
                    ell.Fill = new SolidColorBrush(Color.FromRgb(255, 36, 0));
                    break;

                case 10:
                    ell.Fill = Brushes.Teal;
                    break;

                case 11:
                    ell.Fill = Brushes.Indigo;
                    break;

                default:
                    ell.Fill = Brushes.Black;
                    break;
            }
        }

        // Generates a rectangle to represent each attempt's row
        private Rectangle generateRectangle(System.Windows.Point currPoint)
        {
            Rectangle rec = new Rectangle
            {
                //Name = $"{_currentAttempt}Rectangle",
                Width = _maxWidth,
                Height = _rectangleHeight,
                Fill = Brushes.Black,
                Visibility = Visibility.Visible
            };

            // Add the rectangle to the _gameCanvas and set position
            _gameCanvas.Children.Add(rec);
            Canvas.SetLeft(rec, currPoint.X);
            Canvas.SetTop(rec, currPoint.Y);

            return rec;
        }

        // Generates a button used for selecting colors

        private Button generateButton(int currentPos, System.Windows.Point currPoint)
        {

            Button btn = new Button
            {
                Width = _buttonSize,
                Height = _buttonSize,
                Background = Brushes.Red,
                BorderThickness = new Thickness(2),
                Content = "0",
                FontWeight = FontWeights.Bold,
                FontSize = _buttonSize / 3,
                Foreground = Brushes.White,
                Visibility = Visibility.Visible
            };

            // Associa l'evento per cambiare colore
            btn.MouseDown += ChangeColour;

            // Controlla se _gameCanvas contiene già il pulsante prima di aggiungerlo
            if (!_gameCanvas.Children.Contains(btn))
            {
                _gameCanvas.Children.Add(btn);
            }

            Canvas.SetLeft(btn, currPoint.X);
            Canvas.SetTop(btn, currPoint.Y);

            return btn;
        }

        private void Btn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ChangeColour(sender, e);
        }

        // Generates an ellipse for the attempt's color representation
        private Ellipse generateEllipse(System.Windows.Point currPoint, Button btnToCopy, int currentAttempt, int currentPos)
        {
            Ellipse ell = new Ellipse
            {
                Width = _buttonSize,
                Height = _buttonSize,
                Fill = btnToCopy.Background,
                Visibility = Visibility.Visible
            };

            // Controlla se _gameCanvas contiene già l'ellisse prima di aggiungerla
            if (!_gameCanvas.Children.Contains(ell))
            {
                _gameCanvas.Children.Add(ell);
            }
            double yOffset = _gameCanvas.Height - (_rectangleSpacing * (currentAttempt + 1));
            Canvas.SetLeft(ell, currPoint.X);
            Canvas.SetTop(ell, currPoint.Y);

            return ell;
        }

        // Generates an ellipse to represent the solution in the top of the game screen
        private Ellipse generateEllipse(System.Windows.Point currPoint, Colours colourToCopy)
        {
            Ellipse ell = new Ellipse
            {
                Width = solveCodeSize,
                Height = solveCodeSize,
                Visibility = Visibility.Visible
            };

            ShowEllipseColour(ell, (int)colourToCopy);

            // Add ellipse to the Canvas and set position
            _gameCanvas.Children.Add(ell);
            Canvas.SetLeft(ell, currPoint.X);
            Canvas.SetTop(ell, currPoint.Y);

            return ell;
        }

        private void btnMenù_Click(object sender, RoutedEventArgs e)
        {
            var a = new MainWindow();
            a.Show();
            this.Close();
        }

        private Button generateTips(int currentTip, System.Windows.Point currentPoint)
        {
            Button tipButton = new Button
            {
                //Name = $"{currentTip}Tip",
                Width = _tipsSize,
                Height = _rectangleHeight,
                Background = Brushes.LightGray,
                Foreground = Brushes.Black,
                Visibility = System.Windows.Visibility.Visible
            };

            // Set the tip content
            switch (currentTip)
            {
                case 0:
                    tipButton.Content = currentGame.RightPosition;
                    break;

                case 1:
                    tipButton.Content = currentGame.WrongPosition;
                    break;

                case 2:
                    tipButton.Content = currentGame.IsAllWrong;
                    break;
            }

            // Add tip button to the Canvas and set position
            _gameCanvas.Children.Add(tipButton);
            Canvas.SetLeft(tipButton, currentPoint.X);
            Canvas.SetTop(tipButton, currentPoint.Y);

            return tipButton;
        }

        // Resets all color selection buttons to their default state
        private void resetButton()
        {
            for (int i = 0; i < currentGame.CodeLength; i++)
            {
                selectColoursList[i].Content = "1"; // Reset to initial color
                ShowButtonColours(selectColoursList[i]); // Update UI
            }
        }
    }
}