using ExileCore;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace SimplePickIt
{
    public class SimplePickIt : BaseSettingsPlugin<SimplePickItSettings>
    {
        private Random Random { get; } = new Random();

        public override void Render()
        {
            if (!IsRunConditionMet()) return;

            var coroutineWorker = new Coroutine(PickItems(), this, "SimplePickIt.PickItems");
            Core.ParallelRunner.Run(coroutineWorker);
        }

        private bool IsRunConditionMet()
        {
            if (!Input.GetKeyState(Settings.PickUpKey.Value)) return false;
            if (!GameController.Window.IsForeground()) return false;
            if (GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible) return false;

            return true;
        }

        private List<LabelOnGround> GetItemToPick()
        {
            List<LabelOnGround> ItemToGet = new List<LabelOnGround>();

            if (GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels != null)
            {
                ItemToGet = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
                    ?.Where(label => label.Address != 0
                        && label.ItemOnGround?.Type != null
                        && label.ItemOnGround.Type == EntityType.WorldItem
                        && label.IsVisible)
                    .OrderBy(label => label.ItemOnGround.DistancePlayer)
                    .ToList();
            }

            if (ItemToGet.Any())
            {
                return ItemToGet;
            }
            else
            {
                return null;
            }
        }

        private IEnumerator PickItems()
        {
            var window = GameController.Window.GetWindowRectangle();
            Stopwatch waitingTime = new Stopwatch();
            int highlight = 0;
            int limit = 0;
            LabelOnGround nextItem = null;

            var itemList = GetItemToPick();
            if (itemList == null)
            {
                yield break;
            }

            do
            {
                if (GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible)
                {
                    yield break;
                }

                if (Settings.MinLoop.Value != 0)
                {
                    if (Settings.MaxLoop.Value < Settings.MinLoop.Value)
                    {
                        int temp = Settings.MaxLoop.Value;
                        Settings.MaxLoop.Value = Settings.MinLoop.Value;
                        Settings.MinLoop.Value = temp;
                    }
                    if (highlight == 0)
                    {
                        limit = Random.Next(Settings.MinLoop.Value, Settings.MaxLoop.Value + 1);
                    }
                    if (highlight == limit - 1)
                    {
                        Input.KeyDown(Settings.HighlightToggle.Value);
                        Thread.Sleep(Random.Next(20, 25));
                        Input.KeyUp(Settings.HighlightToggle.Value);
                        Thread.Sleep(Random.Next(20, 25));
                        Input.KeyDown(Settings.HighlightToggle.Value);
                        Thread.Sleep(Random.Next(20, 25));
                        Input.KeyUp(Settings.HighlightToggle.Value);
                        Thread.Sleep(Random.Next(20, 25));
                        highlight = -1;
                    }
                    highlight++;
                }

                if (itemList.Count() > 1)
                {
                    itemList = itemList.Where(label => label != null).Where(label => label.ItemOnGround != null).OrderBy(label => label.ItemOnGround.DistancePlayer).ToList();
                }

                nextItem = itemList[0];

                if (nextItem.ItemOnGround.DistancePlayer > Settings.Range.Value)
                {
                    yield break;
                }

                var centerOfLabel = nextItem?.Label?.GetClientRect().Center
                    + window.TopLeft
                    + new Vector2(Random.Next(0, 2), Random.Next(0, 2));
                if (!centerOfLabel.HasValue)
                {
                    yield break;
                }

                Input.SetCursorPos(centerOfLabel.Value);
                Thread.Sleep(Random.Next(15, 20));
                Input.Click(MouseButtons.Left);

                waitingTime.Start();
                while (nextItem.ItemOnGround.IsTargetable && waitingTime.ElapsedMilliseconds < Settings.MaxWaitTime.Value)
                {
                    ;
                }
                waitingTime.Reset();

                if(!nextItem.ItemOnGround.IsTargetable)
                {
                    itemList.RemoveAt(0);
                }
            } while (Input.GetKeyState(Settings.PickUpKey.Value) && itemList.Any());

            yield break;
        }
    }
}
