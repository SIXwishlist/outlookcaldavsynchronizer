﻿// This file is Part of CalDavSynchronizer (http://outlookcaldavsynchronizer.sourceforge.net/)
// Copyright (c) 2015 Gerhard Zehetbauer
// Copyright (c) 2015 Alexander Nimmervoll
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Linq;
using System.Threading.Tasks;
using CalDavSynchronizer.Implementation;
using CalDavSynchronizer.Implementation.ComWrappers;
using CalDavSynchronizer.Implementation.DistributionLists;
using CalDavSynchronizer.IntegrationTests.Infrastructure;
using CalDavSynchronizer.IntegrationTests.TestBase;
using DDay.iCal;
using GenSync.Logging;
using NUnit.Framework;
using Thought.vCards;

namespace CalDavSynchronizer.IntegrationTests
{

  public class ContactSynchronizerFixture : ContactSynchronizerFixtureBase
  {
    [Test]
    [Apartment(System.Threading.ApartmentState.STA)]
    public async Task Synchronize_AnyCase_SyncsSogoDistLists()
    {
      var options = GetOptions("IntegrationTests/Contacts/Sogo");
 

      await InitializeFor (options);
      await ClearEventRepositoriesAndCache ();

      await Server.CreateEntity(
        c =>
        {
          c.EmailAddresses.Add(new vCardEmailAddress("nihil@bla.com"));
          c.GivenName = "Nihil";
          c.FamilyName = "Baxter";
        });

      var masonId = await Server.CreateEntity (
       c =>
       {
         c.EmailAddresses.Add (new vCardEmailAddress ("mason@bla.com"));
         c.GivenName = "Agent";
         c.FamilyName = "Mason";
       });

      var steinbergId = await Server.CreateEntity (
       c =>
       {
         c.EmailAddresses.Add (new vCardEmailAddress ("steinberg@bla.com"));
         c.GivenName = "Agent";
         c.FamilyName = "Steinberg";
       });


      await ServerSogoDistListsOrNull.CreateEntity(
        l =>
        {
          l.Name = "Agents";
          l.Members.Add(new KnownDistributionListMember("mason@bla.com", "Mason", masonId.GetServerFileName()));
          l.Members.Add(new KnownDistributionListMember ("steinberg@bla.com", "Steinberg", steinbergId.GetServerFileName()));
        });
      
      await Synchronizer.Synchronize(NullSynchronizationLogger.Instance);

      var outlookNames = (await Outlook.GetAllEntities()).Select(c => c.Entity.Inner.LastName).ToArray();

      CollectionAssert.AreEquivalent(
        new[] {"Baxter", "Mason", "Steinberg"},
        outlookNames);

      using (var outlookDistList = (await OutlookDistListsOrNull.GetAllEntities()).SingleOrDefault()?.Entity)
      {
        Assert.That(outlookDistList, Is.Not.Null);

        Assert.That(outlookDistList.Inner.DLName, Is.EqualTo("Agents"));

        var outlookMembers = Enumerable
          .Range(1, outlookDistList.Inner.MemberCount)
          .Select(i => outlookDistList.Inner.GetMember(i))
          .ToSafeEnumerable()
          .Select(d => d.Address)
          .ToArray();

        CollectionAssert.AreEquivalent(
          new[] {"mason@bla.com", "steinberg@bla.com"},
          outlookMembers);

        outlookDistList.Inner.DLName = "All";
        var recipient = Application.Session.CreateRecipient("Baxter");
        Assert.That(recipient.Resolve(), Is.True);
        outlookDistList.Inner.AddMember(recipient);
        outlookDistList.Inner.Save();
      }

      await Synchronizer.Synchronize (NullSynchronizationLogger.Instance);

      var serverDistList = (await ServerSogoDistListsOrNull.GetAllEntities ()).SingleOrDefault ()?.Entity;
      Assert.That (serverDistList, Is.Not.Null);

      Assert.That (serverDistList.Name, Is.EqualTo ("All"));

      CollectionAssert.AreEquivalent (
       new[] { "mason@bla.com", "steinberg@bla.com", "nihil@bla.com" },
       serverDistList.Members.Select(m => m.EmailAddress));

    }

    [Test]
    [Apartment (System.Threading.ApartmentState.STA)]
    public async Task SynchronizeTwoWay_LocalContactChanges_IsSyncedToServerAndPreservesExtendedPropertiesAndUid ()
    {
      var options = GetOptions ("IntegrationTests/Contacts/Sogo");
      await InitializeFor (options);
      await ClearEventRepositoriesAndCache ();

      string initialUid = null;

      await Server.CreateEntity (
      c =>
      {
        c.EmailAddresses.Add (new vCardEmailAddress ("nihil@bla.com"));
        c.GivenName = "Nihil";
        c.FamilyName = "Baxter";
        c.OtherProperties.Add (new vCardProperty ("X-CALDAVSYNCHRONIZER-INTEGRATIONTEST", "TheValueBlaBLubb"));
        initialUid = c.UniqueId;
      });

      await Synchronizer.Synchronize (NullSynchronizationLogger.Instance);

      using (var outlookEvent = (await Outlook.GetAllEntities ()).Single ().Entity)
      {
        outlookEvent.Inner.FirstName = "TheNewNihil";
        outlookEvent.Inner.Save ();
      }

      await Synchronizer.Synchronize (NullSynchronizationLogger.Instance);

      var serverContact = (await Server.GetAllEntities ()).Single ().Entity;

      Assert.That (serverContact.GivenName, Is.EqualTo ("TheNewNihil"));
      Assert.That (serverContact.UniqueId, Is.EqualTo (initialUid));

      Assert.That (
        serverContact.OtherProperties.SingleOrDefault (p => p.Name == "X-CALDAVSYNCHRONIZER-INTEGRATIONTEST")?.Value,
        Is.EqualTo ("TheValueBlaBLubb"));
    }


    [Test]
    [Apartment(System.Threading.ApartmentState.STA)]
    public async Task Synchronize_AnyCase_SyncsVCardGroupDistLists()
    {
      var options = GetOptions("IntegrationTests/Contacts/Sogo");
      ((Contracts.ContactMappingConfiguration)options.MappingConfiguration).DistributionListType = Contracts.DistributionListType.VCardGroup;

      await InitializeFor(options);
      await ClearEventRepositoriesAndCache();

      await Server.CreateEntity(
        c =>
        {
          c.EmailAddresses.Add(new vCardEmailAddress("nihil@bla.com"));
          c.GivenName = "Nihil";
          c.FamilyName = "Baxter";
        });

      var masonId = await Server.CreateEntity(
        c =>
        {
          c.EmailAddresses.Add(new vCardEmailAddress("mason@bla.com"));
          c.GivenName = "Agent";
          c.FamilyName = "Mason";
        });

      var steinbergId = await Server.CreateEntity(
        c =>
        {
          c.EmailAddresses.Add(new vCardEmailAddress("steinberg@bla.com"));
          c.GivenName = "Agent";
          c.FamilyName = "Steinberg";
        });


      await ServerVCardGroupsOrNull.CreateEntity(
        l =>
        {
          l.FormattedName = "Agents";
          l.FamilyName = "Agents";

          var member = new vCardMember();
          member.EmailAddress = "mason@bla.com";
          member.DisplayName = "Mason";
          l.Members.Add(member);

          member = new vCardMember();
          member.EmailAddress = "steinberg@bla.com";
          member.DisplayName = "Steinberg";
          l.Members.Add(member);
        });

      await Synchronizer.Synchronize(NullSynchronizationLogger.Instance);

      var outlookNames = (await Outlook.GetAllEntities()).Select(c => c.Entity.Inner.LastName).ToArray();

      CollectionAssert.AreEquivalent(
        new[] { "Baxter", "Mason", "Steinberg" },
        outlookNames);

      using (var outlookDistList = (await OutlookDistListsOrNull.GetAllEntities()).SingleOrDefault()?.Entity)
      {
        Assert.That(outlookDistList, Is.Not.Null);

        Assert.That(outlookDistList.Inner.DLName, Is.EqualTo("Agents"));

        var outlookMembers = Enumerable
          .Range(1, outlookDistList.Inner.MemberCount)
          .Select(i => outlookDistList.Inner.GetMember(i))
          .ToSafeEnumerable()
          .Select(d => d.Address)
          .ToArray();

        CollectionAssert.AreEquivalent(
          new[] { "mason@bla.com", "steinberg@bla.com" },
          outlookMembers);

        outlookDistList.Inner.DLName = "All";
        var recipient = Application.Session.CreateRecipient("Baxter");
        Assert.That(recipient.Resolve(), Is.True);
        outlookDistList.Inner.AddMember(recipient);
        outlookDistList.Inner.Save();
      }

      await Synchronizer.Synchronize(NullSynchronizationLogger.Instance);

      var serverDistList = (await ServerVCardGroupsOrNull.GetAllEntities()).SingleOrDefault()?.Entity;
      Assert.That(serverDistList, Is.Not.Null);

      Assert.That(serverDistList.FormattedName, Is.EqualTo("All"));

      CollectionAssert.AreEquivalent(
        new[] { "mason@bla.com", "steinberg@bla.com", "nihil@bla.com" },
        serverDistList.Members.Select(m => m.EmailAddress));

    }



  }


}
