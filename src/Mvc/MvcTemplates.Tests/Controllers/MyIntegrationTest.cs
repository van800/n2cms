using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using N2.Engine;
using N2.Persistence.NH;
using N2.Tests.Fakes;

namespace N2.Templates.Mvc.Tests.Controllers
{
	[TestFixture]
	public class MyIntegrationTest
	{
		[Test]
		public void Test()
		{
			var engine = new ContentEngine(new N2.Engine.Castle.WindsorServiceContainer(), new N2.Web.EventBroker(), new ContainerConfigurer());
			var schemaCreator = new NHibernate.Tool.hbm2ddl.SchemaExport(engine.Resolve<IConfigurationBuilder>().BuildConfiguration());
			schemaCreator.Execute(false, true, false, engine.Resolve<ISessionProvider>().OpenSession.Session.Connection, null);

			var actions = engine.Container.ResolveAll<N2.Plugin.Scheduling.ScheduledAction>();
			Assert.AreNotEqual(0, actions.Length);

			Assert.IsInstanceOf<FakeSessionProvider>(engine.Container.Resolve<ISessionProvider>());

			using (engine.Persister)
			{
				engine.Persister.Save(new Items.FakeContentItem { Name = "Hello" });
			}
			Assert.AreEqual("Hello", engine.Persister.Get(1).Name);
		}
	}
}
