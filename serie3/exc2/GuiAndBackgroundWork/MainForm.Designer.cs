namespace GuiAndBackgroundWork {
    partial class MainForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.start = new System.Windows.Forms.Button();
            this.cancel = new System.Windows.Forms.Button();
            this.progress = new System.Windows.Forms.ProgressBar();
            this.result = new System.Windows.Forms.TextBox();
            this.directory = new System.Windows.Forms.TextBox();
            this.stringToFind = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // start
            // 
            this.start.Location = new System.Drawing.Point(6, 81);
            this.start.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.start.Name = "start";
            this.start.Size = new System.Drawing.Size(80, 19);
            this.start.TabIndex = 0;
            this.start.Text = "Start";
            this.start.UseVisualStyleBackColor = true;
            this.start.Click += new System.EventHandler(this.start_Click);
            // 
            // cancel
            // 
            this.cancel.Location = new System.Drawing.Point(101, 81);
            this.cancel.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(83, 19);
            this.cancel.TabIndex = 1;
            this.cancel.Text = "Cancel";
            this.cancel.UseVisualStyleBackColor = true;
            this.cancel.Click += new System.EventHandler(this.cancel_Click);
            // 
            // progress
            // 
            this.progress.Location = new System.Drawing.Point(6, 106);
            this.progress.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(513, 19);
            this.progress.TabIndex = 2;
            // 
            // result
            // 
            this.result.Location = new System.Drawing.Point(17, 142);
            this.result.Multiline = true;
            this.result.Name = "result";
            this.result.Size = new System.Drawing.Size(512, 269);
            this.result.TabIndex = 3;
            // 
            // directory
            // 
            this.directory.Location = new System.Drawing.Point(5, 29);
            this.directory.Name = "directory";
            this.directory.Size = new System.Drawing.Size(512, 20);
            this.directory.TabIndex = 4;
            // 
            // stringToFind
            // 
            this.stringToFind.Location = new System.Drawing.Point(6, 55);
            this.stringToFind.Name = "stringToFind";
            this.stringToFind.Size = new System.Drawing.Size(512, 20);
            this.stringToFind.TabIndex = 5;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.stringToFind);
            this.groupBox1.Controls.Add(this.start);
            this.groupBox1.Controls.Add(this.progress);
            this.groupBox1.Controls.Add(this.cancel);
            this.groupBox1.Controls.Add(this.directory);
            this.groupBox1.Location = new System.Drawing.Point(11, 11);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox1.Size = new System.Drawing.Size(527, 413);
            this.groupBox1.TabIndex = 16;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Get lines from files in directory where a certain string appears";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(549, 435);
            this.Controls.Add(this.result);
            this.Controls.Add(this.groupBox1);
            this.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Name = "MainForm";
            this.Text = "GUI and Background Work";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button start;
        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.ProgressBar progress;
        private System.Windows.Forms.TextBox result;
        private System.Windows.Forms.TextBox directory;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox stringToFind;
    }
}

