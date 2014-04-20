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
                var uniqueFiles = new List<string>();
                foreach( var uniqueFileType in uniqueFileTypes )
                {
                    uniqueFiles.AddRange( filePaths.Where( path => Regex.Match( path, String.Format( @"^.*?\.{0}$", uniqueFileType ) ).Success ) );
                }
                string[] requiredFileTypes = textBox2.Text.Split( ',' );
                foreach( var uniqueFile in uniqueFiles )
                {
                    foreach( var requiredFileType in requiredFileTypes )
                    {
                        bool found = false;
                        foreach( var filePath in filePaths )
                        {
                            int lastBackslashIndex = uniqueFile.LastIndexOf( "\\" );
                            int lastDotIndex = uniqueFile.LastIndexOf( '.' );
                            string uniqueFileName = uniqueFile.Substring( lastBackslashIndex + 1, lastDotIndex - lastBackslashIndex - 1 );
                            if( Regex.Match( filePath, String.Format( @"^.*?{0}.*?\.{1}$", uniqueFileName, requiredFileType ) ).Success )
                            {
                                found = true;
                                break;
                            }
                        }
                        if( !found )
                        {
                            MessageBox.Show( "ERROR! Couldn't find filetype: " + requiredFileType + " for " + uniqueFile );
                            return;
                        }
                    }
                }

                int amountDone = 0;
                foreach( var uniqueFile in uniqueFiles )
                {
                    foreach( var requiredFileType in requiredFileTypes )
                    {
                        foreach( var filePath in filePaths )
                        {
                            int lastBackslashIndex = uniqueFile.LastIndexOf( "\\" );
                            int lastDotIndex = uniqueFile.LastIndexOf( '.' );
                            string uniqueFileName = uniqueFile.Substring( lastBackslashIndex + 1, lastDotIndex - lastBackslashIndex - 1 );
                            try
                            {
                                if( Regex.Match( filePath, String.Format( @"^.*?{0}.*?\.{1}$", uniqueFileName, requiredFileType ) ).Success )
                                {
                                    File.Move( filePath, uniqueFile.Substring( 0, lastBackslashIndex + 1 ) + uniqueFileName + "." + requiredFileType );
                                    progressBar1.BeginInvoke( new Action( () => progressBar1.Value = ( ++amountDone / uniqueFiles.Count ) * 100 ) );
                                    break;
                                }
                            }
                            catch( Exception exception )
                            {
                                MessageBox.Show( "ERROR! An exception occured: " + exception.Message );
                                return;
                            }
                        }
                    }
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