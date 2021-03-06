﻿using System;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;
using Splat;
using VpdbAgent.Application;

namespace VpdbAgent.ViewModels.Messages
{
	public class MessagesViewModel : ReactiveObject
	{
		// deps
		private readonly IMessageManager _messageManager;

		// props
		public IReactiveDerivedList<MessageItemViewModel> Messages { get; }
		public ReactiveCommand<Unit, Unit> ClearAll;
		
		// output props
		private readonly ObservableAsPropertyHelper<bool> _isEmpty;
		public bool IsEmpty => _isEmpty.Value;

		// privates
		private readonly Subject<Unit> _messagesRead = new Subject<Unit>();

		public MessagesViewModel(IDatabaseManager databaseManager, IMessageManager messageManager)
		{
			_messageManager = messageManager;

			Messages = databaseManager.GetMessages().CreateDerivedCollection(
				msg => new MessageItemViewModel(msg),
				x => true, 
				(x, y) => x.Message.CompareTo(y.Message),
				_messagesRead
			);

			databaseManager.GetMessages().CountChanged
				.StartWith(0)
				.Select(_ => Messages.Count == 0)
				.ToProperty(this, x => x.IsEmpty, out _isEmpty);

			ClearAll =  ReactiveCommand.Create(() => {
				_messageManager.ClearAll();
			});
		}

		public void OnViewUnselected()
		{
			_messageManager.MarkAllRead();
			_messagesRead.OnNext(Unit.Default); // refresh list
		}
	}
}
