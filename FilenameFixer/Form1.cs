using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FilenameFixer
{
    public partial class Form1 : Form
    {
        private string _selectedPath{ get; set; }
        public CancellationTokenSource _tokenSource{ get; set; }

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click( object sender, EventArgs e )
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();

            if( result == DialogResult.OK )
            {
                _selectedPath = folderBrowserDialog1.SelectedPath;
                button2.Enabled = true;
            }
            else
            {
                _selectedPath = "";
                button2.Enabled = false;
                button3.Enabled = false;
            }
        }

        private void button2_Click( object sender, EventArgs e )
        {
            string errorMsg = "";
            if( String.IsNullOrEmpty( _selectedPath ) )
                errorMsg = "ERROR! Selected path is null or empty";
            if( String.IsNullOrEmpty( textBox1.Text ) )
                errorMsg = "ERROR! Unique filetypes is null or empty";
            if( String.IsNullOrEmpty( textBox2.Text ) )
                errorMsg = "ERROR! Foreach filetypes is null or empty";
            if( errorMsg != "" )
            {
                MessageBox.Show( errorMsg );
                return;
            }

            _tokenSource = new CancellationTokenSource();
            button3.BeginInvoke( new Action( () => button3.Enabled = true ) );
            button2.BeginInvoke( new Action( () => button2.Enabled = false ) );
            progressBar1.BeginInvoke( new Action( () => progressBar1.Value = 0 ) );
            Task t = Task.Run( () =>
            {
                string[] filePaths = Directory.GetFiles( _selectedPath );
                string[] uniqueFileTypes = textBox1.Text.Split( ',' );
                string[] requiredFileTypes = textBox2.Text.Split( ',' );

                var filePairs = new[] { new { Name = "", List = new List<String>() } }.ToList();
                int amountDone = 1;
                foreach( var uniqueFile in filePaths.Where( path => uniqueFileTypes.Any( path.Contains ) ) )
                {
                    var id = Regex.Match( uniqueFile, @"([sS]\d+([eE]0?\d+)+|[sS]?\d+[xX][eE]?\d+)" ).Value.ToLower();
                    var newFilePair = new { Name = amountDone.ToString(), List = new List<String>() };
                    foreach( var filePath in filePaths )
                    {
                        if( filePath.ToLower().Contains( id ) && uniqueFileTypes.Concat( requiredFileTypes ).ToArray().Any( filePath.Substring( filePath.LastIndexOf( '.' ) + 1 ).Equals ) )
                            newFilePair.List.Add( filePath );
                    }
                    if( newFilePair.List.Count != ( 1 + requiredFileTypes.Length ) )
                    {
                        MessageBox.Show( "ERROR! Couldn't find all required file types for: " + id );
                        return;
                    }
                    filePairs.Add( newFilePair );
                    amountDone++;
                }

                amountDone = 0;
                foreach( var filePair in filePairs )
                {
                    if( filePair.Name != "" )
                    {
                        foreach( var file in filePair.List )
                        {
                            try
                            {
                                string location = file.Substring( 0, file.LastIndexOf( "\\" ) ) + "\\" + filePair.Name + file.Substring( file.LastIndexOf( '.' ) );
                                File.Move( file, location );
                            }
                            catch( Exception exception )
                            {
                                MessageBox.Show( "ERROR! An exception occured: " + exception.Message );
                                return;
                            }
                            
                        }
                    }
                    progressBar1.BeginInvoke( new Action( () => progressBar1.Value = ( ++amountDone / filePairs.Count ) * 100 ) );
                }

                button3.BeginInvoke( new Action( () => button3.Enabled = false ) );
                button2.BeginInvoke( new Action( () => button2.Enabled = true ) );
                _tokenSource = null;
            }, _tokenSource.Token );
        }

        private void button3_Click(object sender, EventArgs e)
        {
            button3.Enabled = false;
            button2.Enabled = true;
            if( _tokenSource == null )
                return;

            _tokenSource.Cancel();
            _tokenSource = null;
        }
    }
}