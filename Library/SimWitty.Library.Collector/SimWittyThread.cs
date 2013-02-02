// <copyright file="SimWittyThread.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Collector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Threading abstraction layer for SimWitty Collector Services and related tools.
    /// </summary>
    public class SimWittyThread
    {
        /// <summary>
        /// The internal thread that executes the task.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed. Legacy design.")]
        public Thread InnerThread;
        
        /// <summary>
        /// A reference to the SimWitty collector service under which the thread will run.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed. Legacy design.")]
        protected SimWittyService parentService;

        /// <summary>
        /// Specifies the name of the thread for logging and debugging purposes.
        /// </summary>
        private string threadName = string.Empty;

        /// <summary>
        /// Is the thread running?
        /// </summary>
        private bool threadIsRunning = false;
        
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SimWittyThread"/> class.
        /// </summary>
        public SimWittyThread()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimWittyThread"/> class.
        /// </summary>
        /// <param name="parentService">Passes in a reference to the SimWitty collector service under which the thread will run.</param>
        public SimWittyThread(SimWitty.Library.Collector.SimWittyService parentService)
        {
            this.parentService = parentService;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of the thread used for reporting and logging. Defaults to the class name (GetType().ToString()).
        /// </summary>
        public string ThreadName
        {
            get
            {
                if (this.threadName.Length == 0) this.threadName = this.GetType().ToString();
                return this.threadName;
            }

            set
            {
                this.threadName = value;
            }
        }
                
        /// <summary>
        /// Gets or sets a value indicating whether the thread is running. Set executes RequestStart() or RequestStop().
        /// </summary>
        public bool Running
        {
            get
            {
                return this.threadIsRunning;
            }

            set
            {
                if (value) this.RequestStart();
                else this.RequestStop();
            }
        }

        /// <summary>
        /// Gets the Identifier for the thread (this.InnerThread.ManagedThreadId).
        /// </summary>
        public int ThreadId
        {
            get
            {
                return this.InnerThread.ManagedThreadId;
            }
        }

        #endregion

        #region Methods
        
        /// <summary>
        /// Call this method to start the thread. 
        /// </summary>
        public void RequestStart()
        {
            // The thread better not be running ...
            if (this.Running)
                return;

            if (this.InnerThread != null)
            {
                if (this.InnerThread.ThreadState == System.Threading.ThreadState.Running)
                    return;
            }

            // Start the internal thread
            try
            {
                // Start the thread
                this.InnerThread = new Thread(new ThreadStart(this.ExecuteTask));
                this.InnerThread.Start();

                // Log the thread startup
                this.parentService.Log(
                    string.Format(
                    "Starting {0} thread ({1}).", 
                    this.ThreadName, 
                    this.ThreadId.ToString()));

                // Mark it as started (cannot use this.Running without recalling the RequestStart method)
                this.threadIsRunning = true;
            }
            catch (Exception ex)
            {
                // Log the exception and mark it as not started
                string message = string.Format("Failed to start {0} thread ({1}). \n {2}", this.ThreadName, this.ThreadId.ToString(), ex.ToString());
                this.parentService.Log(System.Diagnostics.EventLogEntryType.Error, message);
                this.threadIsRunning = false;
            }
        }

        /// <summary>
        /// Call this method to request the thread stops. Note the actual stop will occur within this.ExecuteTask.
        /// </summary>
        public void RequestStop()
        {
            // Mark the thread as stopped
            this.threadIsRunning = false;

            // Log the thread stop
            string message = string.Format("Stopping {0} thread ({1}).", this.ThreadName, this.ThreadId.ToString());            
            this.parentService.Log(message);
        }

        /// <summary>
        /// Execute the task under the thread.
        /// </summary>
        protected virtual void ExecuteTask()
        {
            string message = string.Format("Error in {0} thread ({1}).\nThe child thread class is not properly implemented.\n Missing: protected override void ExecuteTask()", this.ThreadName, this.ThreadId.ToString());
            this.parentService.Log(System.Diagnostics.EventLogEntryType.Error, message);
        }

        /// <summary>
        /// Stop an active thread.
        /// </summary>
        protected void Stop()
        {
            // No need to stop a null thread ...
            if (this.InnerThread == null) return;

            // Handle stopping the thread based on the current state
            // Using switch in case we want to build more logic here
            switch (this.InnerThread.ThreadState)
            {
                case System.Threading.ThreadState.Aborted:
                case System.Threading.ThreadState.Stopped:
                case System.Threading.ThreadState.Unstarted:
                    // Already stopped?
                    break;

                case System.Threading.ThreadState.Suspended:
                    this.InnerThread.Abort();
                    break;

                case System.Threading.ThreadState.Running:
                    this.InnerThread.Abort();
                    break;

                default:
                    break;
            }
        }

        #endregion
    }
}
