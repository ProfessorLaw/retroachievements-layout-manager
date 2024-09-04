
using System;
using System.Windows.Forms;
using Retro_Achievement_Tracker.Properties;

namespace Retro_Achievement_Tracker.Forms
{
    partial class AchievementListWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Used to throttle resize settings auto-save
        /// </summary>
        private Timer resizeTimer = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AchievementListWindow));
            this.webView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
            ((System.ComponentModel.ISupportInitialize)(this.webView21)).BeginInit();
            this.SuspendLayout();
            // 
            // webView21
            // 
            this.webView21.AllowExternalDrop = true;
            this.webView21.CreationProperties = null;
            this.webView21.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webView21.Location = new System.Drawing.Point(0, 0);
            this.webView21.Name = "webView21";
            this.webView21.TabIndex = 0;
            this.webView21.ZoomFactor = 1D;
            this.webView21.NavigationCompleted += new System.EventHandler<Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs>(this.NavigationCompleted);
            this.webView21.Width = this.ClientSize.Width;
            this.webView21.Height = this.ClientSize.Height;
            // 
            // AchievementListWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(120, 1);
            this.Controls.Add(this.webView21);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "AchievementListWindow";
            this.Text = "RA Tracker - Achievement List";

            // Initialize the resize timer
            resizeTimer = new Timer
            {
                Interval = 5000 // Set the delay to half a second (adjust as needed)
            };
            resizeTimer.Tick += ResizeTimer_Tick;

            this.Resize += WebView_Resize;
            ((System.ComponentModel.ISupportInitialize)(this.webView21)).EndInit();
            this.ResumeLayout(false);

        }

        private void ResizeTimer_Tick(object sender, EventArgs e)
        {
            // Stop the timer
            resizeTimer.Stop();

            // Update WebView2 size to match the form's client area
            if (
                Settings.Default.achievement_list_window_width != this.ClientSize.Width ||
                Settings.Default.achievement_list_window_height != this.ClientSize.Height
            )
            {
                Settings.Default.achievement_list_window_width = this.ClientSize.Width;
                Settings.Default.achievement_list_window_height = this.ClientSize.Height;
                Settings.Default.Save();
            }
        }

        private void WebView_Resize(object sender, EventArgs e)
        {
            resizeTimer.Stop();
            resizeTimer.Start();
            // Update WebView2 size to match form's client area
            this.webView21.Width = this.ClientSize.Width;
            this.webView21.Height = this.ClientSize.Height;
        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 webView21;
    }
}