using FluentAssertions;
using GalaSoft.MvvmLight.Helpers;
using MvvmRelayCommand = GalaSoft.MvvmLight.Command.RelayCommand;
using MvvmRelayCommandGeneric = GalaSoft.MvvmLight.Command.RelayCommand<int>;

namespace NINA.Test.Utility {

    [TestFixture]
    public class MvvmLightCommandTest {

        /// <summary>
        /// Verifies that WeakAction executes instance methods while alive and stops invoking them after deletion.
        /// This protects legacy command helpers from retaining or invoking targets after an explicit cleanup.
        /// </summary>
        [Test]
        public void WeakAction_InstanceAction_ExecutesUntilMarkedForDeletion() {
            ActionTarget target = new ActionTarget();
            WeakAction weakAction = new WeakAction(target.Increment);

            weakAction.IsAlive.Should().BeTrue();
            weakAction.Target.Should().BeSameAs(target);
            weakAction.MethodName.Should().Be(nameof(ActionTarget.Increment));

            weakAction.Execute();
            weakAction.MarkForDeletion();
            weakAction.Execute();

            target.Count.Should().Be(1);
            weakAction.IsAlive.Should().BeFalse();
            weakAction.Target.Should().BeNull();
        }

        /// <summary>
        /// Verifies that WeakAction&lt;T&gt; casts object parameters and forwards the typed value.
        /// This covers command storage paths that keep weak actions behind the non-generic object execution interface.
        /// </summary>
        [Test]
        public void WeakActionGeneric_ExecuteWithObject_CastsAndForwardsParameter() {
            ActionTarget target = new ActionTarget();
            WeakAction<int> weakAction = new WeakAction<int>(target.Add);

            weakAction.ExecuteWithObject(12);

            target.Count.Should().Be(12);
            weakAction.MethodName.Should().Be(nameof(ActionTarget.Add));
        }

        /// <summary>
        /// Verifies that WeakFunc returns values while alive and default values after deletion.
        /// This documents the null-safe behavior used by RelayCommand CanExecute delegates.
        /// </summary>
        [Test]
        public void WeakFunc_ExecuteAfterDeletion_ReturnsDefaultValue() {
            FuncTarget target = new FuncTarget();
            WeakFunc<bool> weakFunc = new WeakFunc<bool>(target.CanExecute);

            weakFunc.Execute().Should().BeTrue();
            weakFunc.MarkForDeletion();

            weakFunc.IsAlive.Should().BeFalse();
            weakFunc.Execute().Should().BeFalse();
        }

        /// <summary>
        /// Verifies that RelayCommand refuses a null execute delegate.
        /// This catches invalid command construction early instead of failing later during user interaction.
        /// </summary>
        [Test]
        public void RelayCommand_WhenExecuteIsNull_ThrowsArgumentNullException() {
            Action createCommand = () => _ = new MvvmRelayCommand(null);

            createCommand.Should().Throw<ArgumentNullException>().WithParameterName("execute");
        }

        /// <summary>
        /// Verifies that RelayCommand respects CanExecute before invoking Execute.
        /// This protects command bindings that disable UI actions through a delegate rather than through button state alone.
        /// </summary>
        [Test]
        public void RelayCommand_WhenCanExecuteChanges_OnlyExecutesWhenAllowed() {
            int executionCount = 0;
            bool canExecute = false;
            MvvmRelayCommand command = new MvvmRelayCommand(() => executionCount++, () => canExecute);

            command.CanExecute(null).Should().BeFalse();
            command.Execute(null);
            executionCount.Should().Be(0);

            canExecute = true;
            command.CanExecute(null).Should().BeTrue();
            command.Execute(null);
            executionCount.Should().Be(1);
        }

        /// <summary>
        /// Verifies that RelayCommand&lt;T&gt; converts convertible command parameters before execution.
        /// This covers WPF bindings that pass text-box or XAML string values into numeric command handlers.
        /// </summary>
        [Test]
        public void RelayCommandGeneric_ExecuteWithConvertibleParameter_ConvertsBeforeExecuting() {
            int observedValue = 0;
            MvvmRelayCommandGeneric command = new MvvmRelayCommandGeneric(value => observedValue = value);

            command.Execute("42");

            observedValue.Should().Be(42);
        }

        /// <summary>
        /// Verifies that RelayCommand&lt;T&gt; uses default(T) for null value-type parameters in CanExecute and Execute.
        /// This protects value-type command handlers from invalid casts when WPF invokes them with a null parameter.
        /// </summary>
        [Test]
        public void RelayCommandGeneric_NullValueTypeParameter_UsesDefaultValue() {
            int observedCanExecuteValue = -1;
            int observedExecuteValue = -1;
            MvvmRelayCommandGeneric command = new MvvmRelayCommandGeneric(
                value => observedExecuteValue = value,
                value => {
                    observedCanExecuteValue = value;
                    return true;
                });

            command.CanExecute(null).Should().BeTrue();
            command.Execute(null);

            observedCanExecuteValue.Should().Be(0);
            observedExecuteValue.Should().Be(0);
        }

        private sealed class ActionTarget {
            public int Count { get; private set; }

            public void Increment() {
                Count++;
            }

            public void Add(int value) {
                Count += value;
            }
        }

        private sealed class FuncTarget {
            public bool CanExecute() {
                return true;
            }
        }
    }
}
