﻿using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;

namespace BestPracticesCodeGenerator
{
    [Command(PackageIds.MyCommand)]
    internal sealed class MyCommand : BaseCommand<MyCommand>
    {
        private DTE2 _dte;

        public MyCommand()
        {
            _dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2;
        }

        public string GetSelectedFileName()
        {
            var items = (Array)_dte.ToolWindows.SolutionExplorer.SelectedItems;
            if (items != null && items.Length > 0)
            {
                UIHierarchyItem selItem = items.GetValue(0) as UIHierarchyItem;
                if (selItem.Object is ProjectItem)
                {
                    ProjectItem projItem = selItem.Object as ProjectItem;
                    return projItem.Properties.Item("FullPath").Value.ToString();
                }
            }
            return null;
        }
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var solution = VS.Solutions.GetCurrentSolutionAsync().Result;

            var window = this.Package.FindToolWindow(typeof(frmCodeGenerationOptions), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Não foi possível criar a janela de ferramentas.");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;

            var frm = ((frmCodeGenerationOptionsControl)window.Content);

            frm.Solution = solution;
            frm.OriginalFileName = GetSelectedFileName();// doc.FilePath;
            frm.FileContent = File.ReadAllText(frm.OriginalFileName);

            if (!frm.FileContent.Contains("BaseEntity"))
            {
                await VS.MessageBox.ShowWarningAsync("MyCommand", "Selected class must inherit from 'BaseEntity'");
                return;
            }

            frm.OriginalFilePath = Path.GetDirectoryName(frm.OriginalFileName);
            frm.FileName = Path.GetFileName(frm.OriginalFileName);

            frm.ClassProperties = frm.GetPropertiesInfo();

            frm.GRD_Properties.ItemsSource = frm.ClassProperties;
            frm.BTN_Generate.IsEnabled = true;
            frm.BTN_Reload.IsEnabled = true;
            frm.PNL_InstructionsToLoadClass.Visibility = System.Windows.Visibility.Hidden;
            frm.PNL_GenerateClasses.Visibility = System.Windows.Visibility.Visible;
            frm.LBL_ClassProperties.Text = "Properties from " + frm.FileName.Replace(":", "").Replace(".cs", "");

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
