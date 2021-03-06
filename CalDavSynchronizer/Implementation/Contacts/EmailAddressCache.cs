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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalDavSynchronizer.DataAccess;
using Thought.vCards;

namespace CalDavSynchronizer.Implementation.Contacts
{
  public class EmailAddressCache : ICardDavRepositoryLogger
  {
    private Dictionary<WebResourceName, string[]> _addressesByEntityId = new Dictionary<WebResourceName, string[]>(WebResourceName.Comparer);

    public void LogEntitiesExists(IEnumerable<WebResourceName> allEntities)
    {
      foreach (var id in allEntities)
      {
        if (!_addressesByEntityId.ContainsKey(id))
          _addressesByEntityId.Add(id, null);
      }
    }

    public void LogEntityExists(WebResourceName entityId, vCard vCard)
    {
      _addressesByEntityId[entityId] = vCard.EmailAddresses.Select(a => a.Address).ToArray();
    }

    public void LogEntityDeleted(WebResourceName entityId)
    {
      _addressesByEntityId.Remove(entityId);
    }

    public CacheItem[] Items
    {
      get { return _addressesByEntityId.Select(kv => new CacheItem(kv.Key, kv.Value)).ToArray(); }
      set { _addressesByEntityId = value.ToDictionary(e => e.Id, e => e.EmailAddresses, WebResourceName.Comparer); }
    }

    public WebResourceName[] GetIdsOfEntriesWithEmptyEmailAddress()
    {
      return _addressesByEntityId.Where(e => e.Value == null).Select(e => e.Key).ToArray();
    }
  }
}
