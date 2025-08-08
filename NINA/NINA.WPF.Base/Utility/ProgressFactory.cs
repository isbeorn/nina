using NINA.Core.Model;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace NINA.Core.Utility {
    public static class ProgressFactory {

        /// <summary>
        /// Instantiates an IProgress on the application dispatcher thread. Invoking this will work
        /// from any thread or task
        /// </summary>
        public static IProgress<ApplicationStatus> Create(IApplicationStatusMediator applicationStatusMediator, string sourceName) {
            var synchronizationContext = Application.Current?.Dispatcher != null
                ? new DispatcherSynchronizationContext(Application.Current.Dispatcher)
                : null;

            if (SynchronizationContext.Current == synchronizationContext) {
                return new Progress<ApplicationStatus>(p => {
                    p.Source = sourceName;
                    applicationStatusMediator.StatusUpdate(p);
                });
            } else {
                IProgress<ApplicationStatus> progressTemp = null;
                synchronizationContext.Send(_ => {
                    progressTemp = new Progress<ApplicationStatus>(p => {
                        p.Source = sourceName;
                        applicationStatusMediator.StatusUpdate(p);
                    });
                }, null);
                return progressTemp;
            }
        }
    }
}
