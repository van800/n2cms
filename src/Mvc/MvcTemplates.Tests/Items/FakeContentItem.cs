namespace N2.Templates.Mvc.Tests.Items
{
	[PageDefinition]
	public class FakeContentItem : ContentItem
	{
		public override string TemplateUrl
		{
			get { return "template.aspx"; }
		}
	}
}