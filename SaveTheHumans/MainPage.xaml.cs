using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SaveTheHumans
{
    /// <summary>
    /// This is the C# Code that consists of methods and event handlers for the main page
    /// </summary>
    public sealed partial class MainPage : Page
    {
        
        #region Field Variables

        /// <summary>
        /// Field Variable used to deploy and store a random number
        /// </summary>
        private Random _random = new Random();

        /// <summary>
        /// Field Variable used to create a new instance of a timer for the enemies
        /// </summary>
        private DispatcherTimer _tmEnemyTimer = new DispatcherTimer();

        /// <summary>
        /// Field Variable used to create a new instance of a timer for the portal
        /// </summary>
        private DispatcherTimer _tmTargetTimer = new DispatcherTimer();

        /// <summary>
        /// Field Variable used to create a new instance of a timer for the humans
        /// </summary>
        private DispatcherTimer _tmHumanTimer = new DispatcherTimer();

        /// <summary>
        /// Field Variable used to store a boolean that keeps track of whether a human is currently being saved
        /// </summary>
        private bool _humanRescueInProgress = false;

        /// <summary>
        /// Field Variable used to keep track of the number of humans that have been saved
        /// </summary>
        private int _savedHumanCounter;

        /// <summary>
        /// Field Variable used to keep track of the number of humans on the screen
        /// </summary>
        private int _enemyCount;

        /// <summary>
        /// Field Variable used to keep track of the number of humans throughout the game
        /// </summary>
        private int _humanCount;

        /// <summary>
        /// Field Variable that creates a control will be used to store any human that gets selected
        /// </summary>
        private ContentControl _selectedHuman;

        #endregion

        #region Constructor
        
        /// <summary>
        /// Constructor used to initialize the main page and the field variables
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();

            // Initiate timer values for the enemy spawn timer
            _tmEnemyTimer.Tick += OnEnemyTimerTick;
            _tmEnemyTimer.Interval = TimeSpan.FromSeconds(2);

            // Initiate timer values for the game timer
            _tmTargetTimer.Tick += OnTargetTimerTick;
            _tmTargetTimer.Interval = TimeSpan.FromSeconds(.05);

            // Initiate the timer values for the human spawn timer
            _tmHumanTimer.Tick += OnHumanTimerTick;
            _tmHumanTimer.Interval = TimeSpan.FromSeconds(4);

            // Ensure certain text blocks are hidden when the page is loaded
            _txtGameOverText.Visibility = Visibility.Collapsed;
            _pnlLivesSaved.Visibility = Visibility.Collapsed;
            _pnlStats.Visibility = Visibility.Collapsed;

            // Initiate the entity counters to 0
            _savedHumanCounter = 0;
            _humanCount = 0;
            _enemyCount = 0;

            // Initialize the selected human object
            _selectedHuman = null;

            // Initialize the canvas to be blank
            _pnlPlayArea.Children.Clear();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Method used to initialize the position of the portal when first starting the game
        /// </summary>
        private void InitializePortalPosition()
        {
            // Set the location of the first portal
            Canvas.SetLeft(_uiTarget, _random.Next((int)_pnlPlayArea.ActualWidth / 10, (int)_pnlPlayArea.ActualWidth / 10 * 9));
            Canvas.SetTop(_uiTarget, _random.Next((int)_pnlPlayArea.ActualHeight / 10, (int)_pnlPlayArea.ActualHeight / 10 * 2));

            // Add the portal to the canvas
            _pnlPlayArea.Children.Add(_uiTarget);
        }

        /// <summary>
        /// Method used to initialize the position of the human when first starting the game
        /// </summary>
        private void InitializeHumanPosition()
        {
            // Add a human
            AddHuman();         
        }

        /// <summary>
        /// Method used to add humans to the screen
        /// </summary>
        private void AddHuman()
        {
            // Create a new human control object and apply the human template to it
            ContentControl human = new ContentControl();
            human.Template = Resources["HumanTemplate"] as ControlTemplate;

            // Set the location of the first human
            Canvas.SetLeft(human, _random.Next((int)_pnlPlayArea.ActualWidth / 10, (int)_pnlPlayArea.ActualWidth / 10 * 9));
            Canvas.SetTop(human, _random.Next((int)_pnlPlayArea.ActualHeight / 10 * 8, (int)_pnlPlayArea.ActualHeight / 10 * 9));

            // Add the human to the canvas
            _pnlPlayArea.Children.Add(human);

            // Allow the human to be clicked on
            human.IsHitTestVisible = true;

            // Set the PointerPressed event handler for the human
            human.PointerPressed += OnHumanUiPressed;

            // Add one to the human counter
            _humanCount++;
        }

        /// <summary>
        /// Method used to add enemies to the screen
        /// </summary>
        private void AddEnemy()
        {
            // Create a new enemy control object and apply the enemy template to it
            ContentControl enemy = new ContentControl();
            enemy.Template = Resources["EnemyTemplate"] as ControlTemplate;

            // Allow the enemy to move around the screen
            AnimateEnemy(enemy, 0, _pnlPlayArea.ActualWidth - 100, "(Canvas.Left)");
            AnimateEnemy(enemy, _random.Next((int)_pnlPlayArea.ActualHeight - 100), _random.Next((int)_pnlPlayArea.ActualHeight - 100), "(Canvas.Top)");

            // Add the enemy to the main canvas
            _pnlPlayArea.Children.Add(enemy);

            // Set the PointerEntered event handler for the enemy
            enemy.PointerEntered += OnEnemyPointerEntered;
        }

        /// <summary>
        /// Method used to allow enemy movement around the screen
        /// </summary>
        /// <param name="enemy">takes an enemy as a control</param>
        /// <param name="from">takes the source location</param>
        /// <param name="to">takes the destination location</param>
        /// <param name="propertyToAnimate">takes a string for the canvas property to animate</param>
        private void AnimateEnemy(ContentControl enemy, double from, double to, string propertyToAnimate)
        {
            // Create new Storyboard and DoubleAnimation objects
            Storyboard storyboard = new Storyboard() { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
            DoubleAnimation animation = new DoubleAnimation()
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromSeconds(_random.Next(4, 6)))

            };

            // Determine animation properties
            Storyboard.SetTarget(animation, enemy);
            Storyboard.SetTargetProperty(animation, propertyToAnimate);

            // Execute the animation
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        /// <summary>
        /// Method used to start the game
        /// </summary>
        private void StartGame()
        {
            // Reset boolean rescue-in-progress variable
            _humanRescueInProgress = false;

            // Reset counter variables
            _uiProgressBar.Value = 0;

            _savedHumanCounter = 0;
            _txtDisplayHumansSaved.Text = "0";

            _humanCount = 0;
            _enemyCount = 0;

            // Reset control visibilities
            _btnStartButton.Visibility = Visibility.Collapsed;
            _txtGameOverText.Visibility = Visibility.Collapsed;
            _pnlLivesSaved.Visibility = Visibility.Visible;
            _pnlStats.Visibility = Visibility.Collapsed;

            // Reset the canvas, then add the initial human and portal
            _pnlPlayArea.Children.Clear();
            InitializePortalPosition();
            InitializeHumanPosition();

            // Start the timers
            _tmEnemyTimer.Start();
            _tmTargetTimer.Start();
            _tmHumanTimer.Start();

            // Ensure no human is stored in the selectedHuman object
            _selectedHuman = null;
        }

        /// <summary>
        /// Method used to end the game
        /// </summary>
        /// <param name="endReason">takes a string variable that stores the reason for ending the game</param>
        private void EndGame(string endReason)
        {
            // If the game over message is not being displayed
            if (!_pnlPlayArea.Children.Contains(_txtGameOverText))
            {
                // Stop the timers
                _tmEnemyTimer.Stop();
                _tmTargetTimer.Stop();
                _tmHumanTimer.Stop();

                // The user cannot rescue humans
                _humanRescueInProgress = false;

                // Clear the canvas
                _pnlPlayArea.Children.Clear();

                // Allow the user to see the start button. but not see the lives saved stack panel at the top of the screen
                _btnStartButton.Visibility = Visibility.Visible;
                _pnlLivesSaved.Visibility = Visibility.Collapsed;

                // If the game ended because a human collided with an enemy
                if (endReason == "enemy")
                {
                    // Display a message for that scenario
                    _txtGameOverText.Text = "The aliens have captured you! Game Over!";
                }

                // If the game ended because the user left the screen while attempting to rescue a human
                else if (endReason == "leftscreen")
                {
                    // Display a message for that scenario
                    _txtGameOverText.Text = "Cannot run away! Game Over!";
                }

                // If the game ended because time has run out
                else if (endReason == "time")
                {
                    // Display a message for that scenario
                    _txtGameOverText.Text = "Time expired! Game Over!";
                }

                // Whatever the cause may be, display the game over message
                _txtGameOverText.Visibility = Visibility.Visible;

                // If the user has saved at least one human, display their stats
                if (_savedHumanCounter >= 1)
                {
                    // Display the total count of humans and lives saved
                    _txtTotalHumans.Text = _humanCount.ToString();
                    _txtHumansSaved.Text = _savedHumanCounter.ToString();

                    // Use a temporary variable to determine the save percentage, then display it
                    double savePercentage = Math.Round(((double)_savedHumanCounter / _humanCount) * 100);
                    _txtSavePercantage.Text = savePercentage.ToString() + "%";

                    // Display the total count of enemies that were on the screen
                    _txtEnemyCount.Text = _enemyCount.ToString();

                    // Make the entire Stats stack panel visible on the screen
                    _pnlStats.Visibility = Visibility.Visible;
                }
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Event Handler that runs when the user presses the start button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnStartGame(object sender, RoutedEventArgs e)
        {
            // Call the method to start the game
            StartGame();
        }

        /// <summary>
        /// Event Handler that runs for every tick in the main game timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTargetTimerTick(object sender, object e)
        {
            // Add 1 to the progress bar
            _uiProgressBar.Value += 1;

            // If the progress bar is full
            if (_uiProgressBar.Value >= _uiProgressBar.Maximum)
            {
                // Call the end game method, passing it the reason why the game has ended
                String endReason = "time";
                EndGame(endReason);
            }
        }

        /// <summary>
        /// Event Handler that runs for every tick in the enemy timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEnemyTimerTick(object sender, object e)
        {
            // Call the method to add an enemy to the game
            AddEnemy();

            // Keep track of the number of enemies
            _enemyCount++;
        }

        /// <summary>
        /// Event Handler that runs for every tick in the human timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHumanTimerTick(object sender, object e)
        {
            // Add a human
            AddHuman();
        }

        /// <summary>
        /// Event Handler that runs when the user clicks on a human
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHumanUiPressed(object sender, PointerRoutedEventArgs e)
        {
            // If the game is in progress and a human is not currently selected
            if (_tmHumanTimer.IsEnabled && _humanRescueInProgress == false)
            {
                // A rescue is in progress
                _humanRescueInProgress = true;

                // Set the selected human to the human that was clicked on
                _selectedHuman = (ContentControl)sender;
            }
        }

        /// <summary>
        /// Event Handler that runs when the user moves their cursor around the canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPlayAreaPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            // If the user has already clicked on a human
            if (_humanRescueInProgress)
            {
                // Use instances of Point to get the current cursor location
                Point pointerPosition = e.GetCurrentPoint(null).Position;
                Point relativePosition = _pnlGrid.TransformToVisual(_pnlPlayArea).TransformPoint(pointerPosition);

                // If the user moves the cursor too fast
                if ((Math.Abs(relativePosition.X - Canvas.GetLeft(_selectedHuman)) > _selectedHuman.ActualWidth * 3) || (Math.Abs(relativePosition.Y - Canvas.GetTop(_selectedHuman)) > _selectedHuman.ActualHeight * 3))
                {
                    // Unselect the human, allow it to be clicked again
                    _humanRescueInProgress = false;
                    _selectedHuman = null;
                }

                // If the user does not move their cursor too fast
                else
                {
                    // Have the human stay in the center of the cursor and follow it as it moves around the screen
                    Canvas.SetLeft(_selectedHuman, relativePosition.X - _selectedHuman.ActualWidth / 10);
                    Canvas.SetTop(_selectedHuman, relativePosition.Y - _selectedHuman.ActualHeight / 10);
                }
            }
        }

        /// <summary>
        /// Event Handler that runs when a human is dragged into a portal
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPortalUiEntered(object sender, PointerRoutedEventArgs e)
        {
            // If the game is in progress and a human is currently being rescued
            if (_humanRescueInProgress)
            {                
                // Reset the game timer
                _uiProgressBar.Value = 0;

                // Create another portal and reset the human in random locations at the top (portal) and bottom (human)
                Canvas.SetLeft(_uiTarget, _random.Next((int)_pnlPlayArea.ActualWidth / 10, (int)_pnlPlayArea.ActualWidth / 10 * 9));
                Canvas.SetTop(_uiTarget, _random.Next((int)_pnlPlayArea.ActualHeight / 10, (int)_pnlPlayArea.ActualHeight / 10 * 2));

                Canvas.SetLeft(_selectedHuman, _random.Next((int)_pnlPlayArea.ActualWidth / 10, (int)_pnlPlayArea.ActualWidth / 10 * 9));
                Canvas.SetTop(_selectedHuman, _random.Next((int)_pnlPlayArea.ActualHeight / 10 * 8, (int)_pnlPlayArea.ActualHeight / 10 * 9));

                // Unselect the human
                _selectedHuman = null;

                // Add one to the counter that keeps track of the number of saved humans
                _savedHumanCounter ++;

                // Add one to the counter that keeps track of the number of total humans
                _humanCount ++;

                // Update the label that displays the counter to the user
                _txtDisplayHumansSaved.Text = _savedHumanCounter.ToString();

                // Allow the user to save and click on another human
                _humanRescueInProgress = false;

               
            }
        }

        /// <summary>
        /// Event Handler that runs when a human collides with an enemy
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEnemyPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // If the human the enemy collides with is currently clicked on by the user
            if (_humanRescueInProgress)
            {
                // Call the end game method, passing it the reason why the game has ended
                string endReason = "enemy";
                EndGame(endReason);
            }
        }

        /// <summary>
        /// Event Handler that runs when the users cursor exits the canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPlayAreaPointerExited(object sender, PointerRoutedEventArgs e)
        {
            // If the user moves their cursor outside of the canvas while they have a human selected
            if (_humanRescueInProgress)
            {
                // Call the end game method, passing it the reason why the game has ended
                String endReason = "leftscreen";
                EndGame(endReason);
            }
        }

        #endregion
    }
}
