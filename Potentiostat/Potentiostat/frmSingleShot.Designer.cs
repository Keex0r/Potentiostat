namespace Potentiostat
{
    partial class frmSingleShot
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.components = new System.ComponentModel.Container();
            this.graph = new jwGraph.jwGraph.jwGraph();
            this.SuspendLayout();
            // 
            // graph
            // 
            this.graph.AutoScaleBorder = 0.1D;
            this.graph.BottomRightColor = System.Drawing.Color.LightSteelBlue;
            this.graph.CenterImage = null;
            this.graph.CenterImageMaxSize = new System.Drawing.Size(0, 0);
            this.graph.CenterImageMinSize = new System.Drawing.Size(0, 0);
            this.graph.Cursor = System.Windows.Forms.Cursors.Cross;
            this.graph.Dock = System.Windows.Forms.DockStyle.Fill;
            this.graph.EnableAutoscaling = true;
            this.graph.EnableGraphObjects = true;
            this.graph.EnableLegend = true;
            this.graph.EnableMarkers = true;
            this.graph.EraserVisible = false;
            this.graph.FreeMarkerCount = 0;
            this.graph.GraphBackColor = System.Drawing.Color.GhostWhite;
            this.graph.GraphBorder = new System.Windows.Forms.Padding(100, 20, 100, 70);
            this.graph.HighQuality = true;
            this.graph.HorizontalMarkerCount = 0;
            this.graph.IncludeMarkersInScaling = true;
            this.graph.LeftMouseAction = jwGraph.jwGraph.jwGraph.enLeftMouseAction.ZoomIn;
            this.graph.LeftMouseFunctionalityEnabled = true;
            this.graph.LegendAlwaysVisible = false;
            this.graph.LegendPosition = jwGraph.jwGraph.jwGraph.enumLegendPosition.TopRight;
            this.graph.LegendTitle = null;
            this.graph.Location = new System.Drawing.Point(0, 0);
            this.graph.Message = "";
            this.graph.MessageColor = System.Drawing.Color.Black;
            this.graph.MiddleMouseFunctionalityEnabled = true;
            this.graph.MinimumSize = new System.Drawing.Size(160, 105);
            this.graph.MouseWheelZoomEnabled = true;
            this.graph.Name = "graph";
            this.graph.RightMouseFunctionalityEnabled = true;
            this.graph.ScaleProportional = false;
            this.graph.Size = new System.Drawing.Size(847, 614);
            this.graph.TabIndex = 1;
            this.graph.TopLeftColor = System.Drawing.Color.White;
            this.graph.VerticalMarkerCount = 0;
            // 
            // frmSingleShot
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(847, 614);
            this.Controls.Add(this.graph);
            this.Name = "frmSingleShot";
            this.Text = "Single Shot";
            this.ResumeLayout(false);

        }

        #endregion
        private jwGraph.jwGraph.jwGraph graph;
    }
}