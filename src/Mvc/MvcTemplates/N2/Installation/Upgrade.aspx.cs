﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using N2.Edit.Installation;
using N2.Management.Installation;
using N2.Engine;

namespace N2.Edit.Install
{
	public partial class Upgrade : System.Web.UI.Page
	{
		protected IEngine Engine
		{
			get { return N2.Context.Current; }
		}

		protected InstallationManager Installer
		{
			get { return Engine.Resolve<InstallationManager>(); }
		}
		protected MigrationEngine Migrator
		{
			get { return Engine.Resolve<MigrationEngine>(); }
		}
		protected InstallationChecker Checker
		{
			get { return Engine.Resolve<InstallationChecker>(); }
		}

		protected override void OnInit(EventArgs e)
		{
			InstallationUtility.CheckInstallationAllowed(Context);

			base.OnInit(e);

			foreach(var m in Migrator.GetAllMigrations())
			{
				cblMigrations.Items.Add(new ListItem(m.Title, m.GetType().Name) { Selected = m.IsApplicable(Checker.Status) });
			}
		}

		protected void btnInstallAndMigrate_Click(object sender, EventArgs e)
		{
			ExecuteWithErrorHandling(() =>
				{
					ShowResults(Migrator.UpgradeAndMigrate());
				});
			Checker.Status = null;
			Installer.UpdateStatus(Checker.Status.Level);
		}

		protected void btnMigrate_Click(object sender, EventArgs e)
		{
			ExecuteWithErrorHandling(() =>
				{
					var results = Migrator.GetAllMigrations()
						.Where(m => cblMigrations.Items.FindByValue(m.GetType().Name).Selected)
						.Select(m => m.Migrate(Checker.Status))
						.ToList();
					ShowResults(results);
				});
			Checker.Status = null;
		}

		private void ShowResults(IEnumerable<MigrationResult> results)
		{
			StringBuilder errorText = new StringBuilder();
			lblResult.Text += "<ul>";
			foreach (var result in results)
			{
				string message = result.Migration.Title + " executed updating " + result.UpdatedItems + " items.";
				foreach(string error in result.Errors)
				{
					errorText.AppendFormat("{0} error: {1}<br/>", result.Migration.Title, error);
				}
				lblResult.Text += string.Format("<li class='{0}'>{1}</li>", result.Errors.Count > 0 ? "warning" : "ok", message);

				if (!string.IsNullOrEmpty(result.RedirectTo))
				{
					ClientScript.RegisterStartupScript(GetType(), "Redirect", string.Format("if (confirm('Continue with upgrade {0} on separate page?')) window.open('{1}');", result.Migration.Title, result.RedirectTo), addScriptTags: true);
				}
			}
			lblResult.Text += "</ul>";

			errorLabel.Text = errorText.ToString();
		}

		protected void btnInstall_Click(object sender, EventArgs e)
		{
			ExecuteWithErrorHandling(Installer.Upgrade);
			Checker.Status = null;
			Installer.UpdateStatus(Checker.Status.Level);
		}

		protected void btnExportSchema_Click(object sender, EventArgs e)
		{
			Response.ContentType = "application/octet-stream";
			Response.AddHeader("Content-Disposition", "attachment;filename=n2_upgrade.sql");

			InstallationManager im = Installer;
			Response.Write(im.ExportUpgradeSchema());
			Response.End();
		}



		protected Exception ExecuteWithErrorHandling(Action action)
		{
			try
			{
				action();
				return null;
			}
			catch (Exception ex)
			{
				errorLabel.Text = FormatException(ex);
				return ex;
			}
		}

		private static string FormatException(Exception ex)
		{
			if (ex == null)
				return "Unknown error";
			return "<b>" + ex.Message + "</b>" + ex.StackTrace;
		}
	}
}
