using System;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;

namespace GuiAndBackgroundWork {
    public partial class MainForm : Form {

        enum ReportType { Progress, Cancellation, Completion }
        private class Report
        {
            public ReportType type;
            public int percent;
            public object result;
        }

        //
        // Repport progress, cancellation or completion
        //

        private void ReportProgress(object sender, Report r)
        {
            if (r.type == ReportType.Progress) {
                progress.Value = r.percent;
            } else {
                result.AppendText((r.type == ReportType.Cancellation) ? "\r\nThe operation was canceled!" : "\r\n"+r.result.ToString());
                start.Enabled = true;
                cancel.Enabled = false;
            }
        }

        // The progress reporter object
        private readonly IProgress<Report> reporter;

        public MainForm() {
            InitializeComponent();

            // Create progress reporter and set the PorgressChanged event
            // The Report captures the current SynchronizationContext that is the
            // Windows Forms synchronization context.
            var progressReporter = new Progress<Report>();
            progressReporter.ProgressChanged += ReportProgress;
            reporter = progressReporter;
            // define initial buttom state
            start.Enabled = true;
            cancel.Enabled = false;

            directory.Text = "Enter here the directory path";
            stringToFind.Text = "Enter here the string";
        }
        
        private CancellationTokenSource tpCts;
        
        private void start_Click(object sender, EventArgs e) {
            tpCts = new CancellationTokenSource();
            start.Enabled = false;
            cancel.Enabled = true;
            result.Text = "";

            String dir = directory.Text;
            String strToFind = stringToFind.Text;
            try
            {
                string[] files = Directory.GetFiles(dir);
                result.Text = "Running...";

                Object _lock = new object();
                int nrAlreadyProcessed = 0;
                Parallel.ForEach(
                    files,
                    async (item, loopState, index) =>
                    {
                        if (ToCancel()) loopState.Break();
                        try
                        {
                            using (var stream = new FileStream(item, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
                            using (var reader = new StreamReader(stream, Encoding.UTF8))
                            {
                                string fileName = item.Split('\\').Last();
                                string line;
                                int nr = 0;
                                while ((line = await reader.ReadLineAsync()) != null)
                                {
                                    if (ToCancel()) loopState.Break();
                                    if (line.Contains(strToFind))
                                    {
                                        reporter.Report(
                                            new Report
                                            {
                                                type = ReportType.Completion,
                                                result = String.Format("{0} ({1}): \"{2}\"", fileName, nr, line)
                                            });
                                    }
                                    nr++;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            Complete("Something went wrong");
                            loopState.Break();
                        }
                        lock (_lock)
                        {
                            nrAlreadyProcessed++;
                            int percentage = ((nrAlreadyProcessed * 100) / files.Length);
                            reporter.Report(new Report { type = ReportType.Progress, percent = percentage });

                            if (files.Length == nrAlreadyProcessed)
                            {
                                Complete("Search ended.");
                            }
                        }
                    }
                );
            } catch(DirectoryNotFoundException)
            {
                Complete("Invalid directory");
            }
        }

        private void Complete(string result)
        {
            reporter.Report(new Report { type = ReportType.Completion, result = result });
            tpCts.Dispose();
        }
        private bool ToCancel()
        {
            bool toCancel;
            if ((toCancel = tpCts.Token.IsCancellationRequested))
            {
                reporter.Report(new Report { type = ReportType.Cancellation });
                tpCts.Dispose();
            }
            return toCancel;
        }

        private void cancel_Click(object sender, EventArgs e) {
            tpCts.Cancel();
        }
    }
}
