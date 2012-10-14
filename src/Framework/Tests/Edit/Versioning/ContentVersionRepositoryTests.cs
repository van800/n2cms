﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using N2.Tests.Persistence;
using NUnit.Framework;
using N2.Edit.Versioning;
using Shouldly;
using N2.Persistence;
using N2.Definitions.Static;
using N2.Edit.Workflow;
using N2.Web;
using N2.Tests.Fakes;

namespace N2.Tests.Edit.Versioning
{
    [TestFixture]
    public class ContentVersionRepositoryTests : DatabasePreparingBase
    {
        ContentVersionRepository repository;
		IPersister persister;
		private DraftRepository drafts;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
			repository = TestSupport.CreateVersionRepository(ref persister, typeof(Items.NormalPage));
			drafts = new DraftRepository(repository, new FakeCacheWrapper());
        }

        [Test]
        public void MasterVersion_CanBeSavedAsVersion_AndRetrieved()
        {
            var master = CreateOneItem<Items.NormalPage>(0, "pageX", null);
            persister.Save(master);

            var draft = repository.Save(master);
            repository.Repository.Dispose();

            var savedDraft = repository.GetVersion(master);
            savedDraft.Published.ShouldBe(master.Published, TimeSpan.FromSeconds(1));
            savedDraft.PublishedBy.ShouldBe(master.SavedBy);
            savedDraft.Saved.ShouldBe(DateTime.Now, TimeSpan.FromSeconds(1));
            savedDraft.SavedBy.ShouldBe(draft.SavedBy);
            savedDraft.State.ShouldBe(master.State);
			savedDraft.VersionIndex.ShouldBe(master.VersionIndex);
			savedDraft.VersionDataXml.ShouldContain("pageX");
        }

        [Test]
        public void Version_CanBeSavedAsVersioin_AndRetrieved()
        {
            var page = CreateOneItem<Items.NormalPage>(0, "page", null);
            persister.Save(page);

			var version = page.Clone(true);
			version.VersionOf = page;

            var draft = repository.Save(version);
            repository.Repository.Dispose();

            var savedDraft = repository.GetVersion(page);
            savedDraft.Master.Value.ShouldBe(page);
        }

		[Test]
		public void VersionWithDraftStatus_CanBeRetrievedAsDraft()
		{
			var page = CreateOneItem<Items.NormalPage>(0, "page", null);
			persister.Save(page);

			var version = page.Clone(true);
			version.State = ContentState.Draft;
			version.VersionOf = page;
			version.VersionIndex++;

			var draft = repository.Save(version);

			drafts.FindDrafts(page).Single().ID.ShouldBe(draft.ID);
		}

		[TestCase(ContentState.Deleted)]
		[TestCase(ContentState.New)]
		[TestCase(ContentState.None)]
		[TestCase(ContentState.Published)]
		[TestCase(ContentState.Unpublished)]
		[TestCase(ContentState.Waiting)]
		public void OtherStates_ArntDrafts(ContentState state)
		{
			var page = CreateOneItem<Items.NormalPage>(0, "page", null);
			persister.Save(page);

			var version = page.Clone(true);
			version.State = state;
			version.VersionOf = page;

			var draft = repository.Save(version);

			drafts.HasDraft(page).ShouldBe(false);
			drafts.FindDrafts(page).ShouldBeEmpty();
		}

		[Test]
		public void MultipleDrafts_GreatestVersionIndex_IsFirst()
		{
			var page = CreateOneItem<Items.NormalPage>(0, "page", null);
			persister.Save(page);

			var version = page.Clone(true);
			version.VersionIndex = page.VersionIndex + 1;
			version.State = ContentState.Draft;
			version.VersionOf = page;
			var draft1 = repository.Save(version);

			var version2 = page.Clone(true);
			version2.VersionIndex = page.VersionIndex + 2;
			version2.State = ContentState.Draft;
			version2.VersionOf = page;
			var draft2 = repository.Save(version2);

			drafts.FindDrafts(page).First().ID.ShouldBe(draft2.ID);
		}

		[Test]
		public void VersionIndex_IsKeptWhenSavingVersion()
		{
			var page = CreateOneItem<Items.NormalPage>(0, "page", null);
			persister.Save(page);

			var versionItem = page.Clone(true);
			versionItem.VersionIndex = page.VersionIndex + 1;
			versionItem.State = ContentState.Draft;
			versionItem.VersionOf = page;
			
			var version = repository.Save(versionItem);

			repository.Repository.Dispose();
			var savedVersion = repository.GetVersion(page, versionItem.VersionIndex);

			savedVersion.VersionIndex.ShouldBe(versionItem.VersionIndex);
			savedVersion.Version.VersionIndex.ShouldBe(versionItem.VersionIndex);
		}

		[Test]
		public void Parts_AreSerialized()
		{
			var page = CreateOneItem<Items.NormalPage>(0, "page", null);
			persister.Save(page);
			var part = CreateOneItem<Items.NormalPage>(0, "part", page);
			part.ZoneName = "TheZone";
			persister.Save(part);

			var version = page.CloneForVersioningRecursive(engine.Resolve<StateChanger>());
			version.VersionIndex = page.VersionIndex + 1;
			version.VersionOf = page;

			repository.Save(version);

			repository.Repository.Dispose();
			var savedVersion = repository.GetVersion(page, version.VersionIndex);

			var deserializedPage = savedVersion.Version;
			var deserializedPart = deserializedPage.Children.Single();

			deserializedPart.Title.ShouldBe("part");
			deserializedPart.VersionOf.ID.ShouldBe(part.ID);
			deserializedPart.ZoneName.ShouldBe(part.ZoneName);
		}

		[Test]
		public void Details_AreSerialized()
		{
			var page = CreateOneItem<Items.NormalPage>(0, "page", null);
			page["Hello"] = "world";
			page.GetDetailCollection("Stuffs", true).Add("Hello");
			persister.Save(page);

			var version = page.Clone(true);
			version.VersionIndex = page.VersionIndex + 1;
			version.VersionOf = page;

			repository.Save(version);

			repository.Repository.Dispose();
			var savedVersion = repository.GetVersion(page, version.VersionIndex);

			var deserializedPage = savedVersion.Version;
			deserializedPage["Hello"].ShouldBe("world");
			deserializedPage.GetDetailCollection("Stuffs", false)[0].ShouldBe("Hello");
		}

		[Test]
		public void Details_OnParts_AreSerialized()
		{
			var page = CreateOneItem<Items.NormalPage>(0, "page", null);
			persister.Save(page);
			var part = CreateOneItem<Items.NormalPage>(0, "part", page);
			part["Hello"] = "world";
			part.GetDetailCollection("Stuffs", true).Add("Hello");
			part.ZoneName = "TheZone";
			persister.Save(part);

			var version = page.CloneForVersioningRecursive(engine.Resolve<StateChanger>());
			version.VersionIndex = page.VersionIndex + 1;

			repository.Save(version);

			repository.Repository.Dispose();
			var savedVersion = repository.GetVersion(page, version.VersionIndex);

			var deserializedPage = savedVersion.Version;
			var deserializedPart = deserializedPage.Children.Single();
			deserializedPart["Hello"].ShouldBe("world");
			deserializedPart.GetDetailCollection("Stuffs", false)[0].ShouldBe("Hello");
		}

		[Test]
		public void NotSavedAsPrevious_IsFoundAsDraft()
		{
			var master = CreateOneItem<Items.NormalPage>(0, "pageX", null);
			persister.Save(master);

			var manager = new VersionManager(repository, persister.Repository, new StateChanger(), new N2.Configuration.EditSection());
			manager.AddVersion(master, asPreviousVersion: false);

			drafts.FindDrafts().Single().Master.ID.ShouldBe(master.ID);
		}

		[Test]
		public void SavedAsPrevious_IsNotFoundAsDraft()
		{
			var master = CreateOneItem<Items.NormalPage>(0, "pageX", null);
			persister.Save(master);

			var manager = new VersionManager(repository, persister.Repository, new StateChanger(), new N2.Configuration.EditSection());
			manager.AddVersion(master, asPreviousVersion: true);

			drafts.FindDrafts().Any().ShouldBe(false);
		}

		[Test]
		public void DraftsCanBeFound_ForSingleItem()
		{
			var master = CreateOneItem<Items.NormalPage>(0, "pageX", null);
			persister.Save(master);

			var manager = new VersionManager(repository, persister.Repository, new StateChanger(), new N2.Configuration.EditSection());
			manager.AddVersion(master, asPreviousVersion: false);

			drafts.FindDrafts().Single().Master.ID.ShouldBe(master.ID);
		}

		[Test]
		public void DefaultValues_AreMaintained()
		{
			
		}
    }
}