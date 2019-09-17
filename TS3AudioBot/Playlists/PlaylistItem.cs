// TS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  TS3AudioBot contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

namespace TS3AudioBot.Playlists
{
	using System;
	using Audio;
	using ResourceFactories;

	public class PlaylistItem
	{
		public MetaData Meta { get; }
		public AudioResource Resource { get; private set; }

		private PlaylistItem(MetaData meta) { Meta = meta ?? new MetaData(); }
		public PlaylistItem(AudioResource resource, MetaData meta = null) : this(meta) { Resource = resource; }

		public void Resolve(AudioResource resource)
		{
			Resource = resource ?? throw new ArgumentNullException(nameof(resource));
		}

		public override string ToString() => Resource.ResourceTitle ?? $"{Resource.AudioType}: {Resource.ResourceId}";
	}
}
