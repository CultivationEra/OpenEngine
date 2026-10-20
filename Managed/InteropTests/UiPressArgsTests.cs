using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GameEngine.Scripting;
using NUnit.Framework;

namespace GameEngine.ManagedInteropTests
{
    /// <summary>
    /// The pointer press lifecycle as a script sees it: which button, where, and the difference between
    /// a release and a cancelled press.
    /// <para>
    /// Driven against the native shim's element double, like the wrapper tests beside this file, because a
    /// dotnet-test host has no window and no mounted document. What the double cannot speak for — whether the
    /// real tree bubbles a press through ancestors, whether a real manager fills UIEvent::Button from the
    /// platform — is the engine's own UITests' business, and the event ids here are the ones that engine
    /// dispatches.
    /// </para>
    /// </summary>
    public class UiPressArgsTests
    {
        private const string kLib = "GameEngine.Native";

        private const string kMouseDown = "UI.MouseDown";
        private const string kMouseUp = "UI.MouseUp";
        private const string kMouseCancel = "UI.MouseCancel";

        [DllImport(kLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GE_TestUI_AddElement(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string elementId,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string tagLower, out ulong outInstanceId);

        [DllImport(kLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GE_TestUI_Reset();

        [DllImport(kLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GE_TestUI_SubscriptionCount(ulong instanceId,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string? eventName, out int outCount);

        [DllImport(kLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GE_TestUI_DispatchPress(ulong instanceId,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string eventName, float x, float y, int button, int mods,
            out int outInvoked);

        /// <summary>Each test builds its own elements, so the double starts empty.</summary>
        [SetUp]
        public void Reset() => Assert.That(GE_TestUI_Reset(), Is.EqualTo(0));

        private static ulong AddElement(string elementId, string tagLower)
        {
            Assert.That(GE_TestUI_AddElement(elementId, tagLower, out ulong instanceId), Is.EqualTo(0));
            Assert.That(instanceId, Is.Not.Zero);
            return instanceId;
        }

        private static int DispatchPress(ulong instanceId, string eventName, float x, float y, int button,
                                         int mods = 0)
        {
            Assert.That(GE_TestUI_DispatchPress(instanceId, eventName, x, y, button, mods, out int invoked),
                Is.EqualTo(0));
            return invoked;
        }

        private static int SubscriptionCount(ulong instanceId, string eventName)
        {
            Assert.That(GE_TestUI_SubscriptionCount(instanceId, eventName, out int count), Is.EqualTo(0));
            return count;
        }

        /// <summary>
        /// The press carries the button, and the button is the RIGHT one — the reason this surface exists.
        /// <para>
        /// A red control for this: revert the dispatch case in EventSubscription.Invoke and the listener is
        /// never called, because a PressArgs handler then matches no arm of the switch; revert PressArgs.Button
        /// and the assertion on 1 fails while the listener still runs. Both failures name the field rather than
        /// the test.
        /// </para>
        /// </summary>
        [Test]
        public void Pressed_ReportsWhichButtonAndWhere()
        {
            ulong instanceId = AddElement("grid-cell", "button");

            Ui.PressArgs? seen = null;

            Ui.Element? cell = Ui.FindElement("grid-cell");

            Assert.That(cell, Is.Not.Null);
            cell!.Pressed += args => seen = args;

            Assert.That(SubscriptionCount(instanceId, kMouseDown), Is.EqualTo(1),
                "the subscription did not reach the native table");
            Assert.That(DispatchPress(instanceId, kMouseDown, 41.5f, 12.25f, Ui.PointerButton.Right),
                Is.EqualTo(1), "the press reached no listener");

            Assert.That(seen, Is.Not.Null, "the listener was never called");
            Assert.That(seen!.Value.Button, Is.EqualTo(Ui.PointerButton.Right));
            Assert.That(seen.Value.IsRight, Is.True, "IsRight disagreed with the button it was given");
            Assert.That(seen.Value.IsLeft, Is.False);
            Assert.That(seen.Value.X, Is.EqualTo(41.5f).Within(0.0001f));
            Assert.That(seen.Value.Y, Is.EqualTo(12.25f).Within(0.0001f));
        }

        /// <summary>
        /// A release and a cancelled press are DIFFERENT events, and a listener told the difference can
        /// disarm without activating. Folding them together would make "began here, ended elsewhere" a
        /// position check every author has to remember.
        /// </summary>
        [Test]
        public void Released_And_PressCancelled_AreDistinct()
        {
            ulong instanceId = AddElement("grid-cell", "button");

            var heard = new List<string>();

            Ui.Element? cell = Ui.FindElement("grid-cell");

            Assert.That(cell, Is.Not.Null);
            cell!.Released += _ => heard.Add("released");
            cell.PressCancelled += _ => heard.Add("cancelled");

            DispatchPress(instanceId, kMouseUp, 0f, 0f, Ui.PointerButton.Left);
            DispatchPress(instanceId, kMouseCancel, -1f, -1f, Ui.PointerButton.Left);

            Assert.That(heard, Is.EqualTo(new[] { "released", "cancelled" }),
                "a cancelled press was delivered as a release, or not at all");
        }

        /// <summary>
        /// Modifiers still arrive, so a listener can ask "was shift held at the press" without a second
        /// subscription — the same bitmask the click and pointer events carry.
        /// </summary>
        [Test]
        public void Pressed_CarriesTheModifierMask()
        {
            ulong instanceId = AddElement("grid-cell", "button");

            int mods = -1;

            Ui.Element? cell = Ui.FindElement("grid-cell");

            Assert.That(cell, Is.Not.Null);
            cell!.Pressed += args => mods = args.Mods;

            const int kShift = 0x0001;

            DispatchPress(instanceId, kMouseDown, 0f, 0f, Ui.PointerButton.Left, kShift);

            Assert.That(mods, Is.EqualTo(kShift), "the modifier mask did not survive the dispatch");
        }
    }
}
