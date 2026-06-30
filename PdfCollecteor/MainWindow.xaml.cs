using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace PdfCollecteor
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<PdfFileInfo> _pdfFiles = new ObservableCollection<PdfFileInfo>();
        private List<PdfFileInfo> _allFoundFiles = new List<PdfFileInfo>();
        
        public MainWindow()
        {
            InitializeComponent();
            lstResults.ItemsSource = _pdfFiles;
        }
        
        private void BtnBrowseSource_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Wählen Sie den Quellordner aus";
                dialog.UseDescriptionForTitle = true;
                
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtSourceFolder.Text = dialog.SelectedPath;
                }
            }
        }
        
        private void BtnBrowseTarget_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Wählen Sie den Zielordner aus";
                dialog.UseDescriptionForTitle = true;
                
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtTargetFolder.Text = dialog.SelectedPath;
                }
            }
        }
        
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string sourceFolder = txtSourceFolder.Text;
            
            if (string.IsNullOrWhiteSpace(sourceFolder) || !Directory.Exists(sourceFolder))
            {
                MessageBox.Show("Bitte wählen Sie einen gültigen Quellordner aus.", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            _allFoundFiles.Clear();
            _pdfFiles.Clear();
            
            SearchPdfFiles(sourceFolder);
            ApplyFilters();
            
            txtFileCount.Text = _pdfFiles.Count.ToString();
            
            MessageBox.Show("Suche abgeschlossen. Gefunden: " + _allFoundFiles.Count + " Dateien, Anzeige: " + _pdfFiles.Count + " Dateien.",
                "Suche abgeschlossen", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void SearchPdfFiles(string rootPath)
        {
            try
            {
                var subDirectories = Directory.GetDirectories(rootPath);
                
                foreach (var subDir in subDirectories)
                {
                    try
                    {
                        var pdfFiles = Directory.GetFiles(subDir, "*.pdf");
                        
                        foreach (var pdfFile in pdfFiles)
                        {
                            string fileName = Path.GetFileName(pdfFile);
                            
                            if (IsMatchingPattern(fileName))
                            {
                                _allFoundFiles.Add(new PdfFileInfo
                                {
                                    FileName = fileName,
                                    FullPath = pdfFile,
                                    Directory = Path.GetDirectoryName(pdfFile)
                                });
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        continue;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error searching in " + subDir + ": " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler bei der Suche: " + ex.Message, 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private bool IsMatchingPattern(string fileName)
        {
            if (fileName.StartsWith("T", StringComparison.OrdinalIgnoreCase) && 
                fileName.IndexOf("Statik", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            
            if (fileName.StartsWith("H", StringComparison.OrdinalIgnoreCase) && 
                fileName.IndexOf("Statik", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            
            return false;
        }
        
        private void ApplyFilters()
        {
            _pdfFiles.Clear();
            
            bool filterDuplicates = chkFilterDuplicates.IsChecked ?? false;
            bool filterUnderscore = chkFilterUnderscore.IsChecked ?? false;
            
            var filteredFiles = _allFoundFiles.AsQueryable();
            
            if (filterDuplicates)
            {
                var uniqueFiles = _allFoundFiles
                    .GroupBy(f => f.FileName.ToLower())
                    .Select(g => g.First())
                    .ToList();
                filteredFiles = uniqueFiles.AsQueryable();
            }
            
            if (filterUnderscore)
            {
                filteredFiles = filteredFiles
                    .Where(f => !f.FileName.ToLower().EndsWith("_.pdf") && 
                               !f.FileName.ToLower().Contains("_."));
            }
            
            foreach (var file in filteredFiles.OrderBy(f => f.FileName))
            {
                _pdfFiles.Add(file);
            }
        }
        
        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            string targetFolder = txtTargetFolder.Text;
            
            if (string.IsNullOrWhiteSpace(targetFolder))
            {
                MessageBox.Show("Bitte wählen Sie einen Zielordner aus.", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (!Directory.Exists(targetFolder))
            {
                try
                {
                    Directory.CreateDirectory(targetFolder);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Zielordner konnte nicht erstellt werden: " + ex.Message, 
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            
            var filesToCopy = _pdfFiles.ToList();
            
            if (lstResults.SelectedItems.Count > 0)
            {
                filesToCopy = lstResults.SelectedItems.Cast<PdfFileInfo>().ToList();
            }
            
            if (filesToCopy.Count == 0)
            {
                MessageBox.Show("Keine Dateien zum Kopieren ausgewählt.", 
                    "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            int copiedCount = 0;
            int errorCount = 0;
            
            foreach (var fileInfo in filesToCopy)
            {
                try
                {
                    string targetPath = Path.Combine(targetFolder, fileInfo.FileName);
                    
                    if (File.Exists(targetPath))
                    {
                        int counter = 1;
                        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileInfo.FileName);
                        string extension = Path.GetExtension(fileInfo.FileName);
                        
                        do
                        {
                            targetPath = Path.Combine(targetFolder, fileNameWithoutExt + "_" + counter + extension);
                            counter++;
                        } while (File.Exists(targetPath));
                    }
                    
                    File.Copy(fileInfo.FullPath, targetPath, true);
                    copiedCount++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error copying " + fileInfo.FullPath + ": " + ex.Message);
                    errorCount++;
                }
            }
            
            MessageBox.Show("Kopiervorgang abgeschlossen.\n\n" +
                "Erfolgreich kopiert: " + copiedCount + "\n" +
                "Fehler: " + errorCount, 
                "Kopieren abgeschlossen", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void ChkFilter_Checked(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
            txtFileCount.Text = _pdfFiles.Count.ToString();
        }
    }
    
    public class PdfFileInfo
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string Directory { get; set; }
    }
}
