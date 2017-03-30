﻿using System;
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
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Random random = new Random();
        DispatcherTimer enemyTimer = new DispatcherTimer();
        DispatcherTimer targetTimer = new DispatcherTimer();
        bool humanCaptured = false;

        public MainPage()
        {
            this.InitializeComponent();
            enemyTimer.Tick += EnemyTimer_Tick;
            enemyTimer.Interval = TimeSpan.FromSeconds(2);

            targetTimer.Tick += TargetTimer_Tick;
            targetTimer.Interval = TimeSpan.FromSeconds(.1);

            gameOverText.Visibility = Visibility.Collapsed;

        }

        private void TargetTimer_Tick(object sender, object e)
        {
            progressBar.Value += 1;
            if (progressBar.Value >= progressBar.Maximum)
            {
                String endReason = "time";
                EndTheGame(endReason);
            }
        }

        private void EndTheGame(string endReason)
        {
            if (!playArea.Children.Contains(gameOverText))
            {
                enemyTimer.Stop();
                targetTimer.Stop();
                humanCaptured = false;
                startButton.Visibility = Visibility.Visible;

                if (endReason == "enemy")
                {
                    gameOverText.Text = "The aliens have captured you! Game Over!";
                }
                else if (endReason == "leftscreen")
                {
                    gameOverText.Text = "Cannot run away! Game Over!";
                }
                else if (endReason == "time")
                {
                    gameOverText.Text = "Time expired! Game Over!";
                }

                gameOverText.Visibility = Visibility.Visible;
            }
        }

        private void EnemyTimer_Tick(object sender, object e)
        {
            AddEnemy();
        }

        private void AddEnemy()
        {
            ContentControl enemy = new ContentControl();
            enemy.Template = Resources["EnemyTemplate"] as ControlTemplate;
            AnimateEnemy(enemy, 0, playArea.ActualWidth - 100, "(Canvas.Left)");
            AnimateEnemy(enemy, random.Next((int)playArea.ActualHeight - 100), random.Next((int)playArea.ActualHeight - 100), "(Canvas.Top)");
            playArea.Children.Add(enemy);

            enemy.PointerEntered += Enemy_PointerEntered;
        }

        private void Enemy_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (humanCaptured)
            {
                string endReason = "enemy";
                EndTheGame(endReason);
            }
        }

        private void AnimateEnemy(ContentControl enemy, double from, double to, string propertyToAnimate)
        {
            Storyboard storyboard = new Storyboard() { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
            DoubleAnimation animation = new DoubleAnimation()
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromSeconds(random.Next(4, 6)))

            };
            Storyboard.SetTarget(animation, enemy);
            Storyboard.SetTargetProperty(animation, propertyToAnimate);
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            StartGame();
        }

        private void StartGame()
        {
            human.IsHitTestVisible = true;
            humanCaptured = false;
            progressBar.Value = 0;

            startButton.Visibility = Visibility.Collapsed;
            gameOverText.Visibility = Visibility.Collapsed;

            playArea.Children.Clear();

            playArea.Children.Add(target);
            playArea.Children.Add(human);

            enemyTimer.Start();
            targetTimer.Start();
        }

        private void human_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (enemyTimer.IsEnabled)
            {
                humanCaptured = true;
                human.IsHitTestVisible = false;
            }
        }

        private void target_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (targetTimer.IsEnabled == true && humanCaptured)
            {
                progressBar.Value = 0;
                Canvas.SetLeft(target, random.Next((int)playArea.ActualWidth / 10, (int)playArea.ActualWidth / 10 * 9));
                Canvas.SetTop(target, random.Next((int)playArea.ActualHeight / 10, (int)playArea.ActualHeight / 10 * 2));
                Canvas.SetLeft(human, random.Next((int)playArea.ActualWidth / 10, (int)playArea.ActualWidth / 10 * 9));
                Canvas.SetTop(human, random.Next((int)playArea.ActualHeight / 10 * 8, (int)playArea.ActualHeight / 10 * 9));
                humanCaptured = false;
                human.IsHitTestVisible = true;
            }
        }

        private void playArea_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (humanCaptured)
            {
                Point pointerPosition = e.GetCurrentPoint(null).Position;
                Point relativePosition = grid.TransformToVisual(playArea).TransformPoint(pointerPosition);

                if ((Math.Abs(relativePosition.X - Canvas.GetLeft(human)) > human.ActualWidth * 3) || (Math.Abs(relativePosition.Y - Canvas.GetTop(human)) > human.ActualHeight * 3))
                {
                    humanCaptured = false;
                    human.IsHitTestVisible = true;
                }
                else
                {
                    Canvas.SetLeft(human, relativePosition.X - human.ActualWidth / 2);
                    Canvas.SetTop(human, relativePosition.Y - human.ActualHeight / 2);
                }
            }
        }

        private void playArea_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (humanCaptured)
            {
                String endReason = "leftscreen";
                EndTheGame(endReason);
            }
        }
    }
}
