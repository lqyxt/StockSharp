﻿namespace StockSharp.Diagram.Elements;

using StockSharp.Alerts;

/// <summary>
/// Notification element (sound, window etc.) for specific market events.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.NotificationKey,
	Description = LocalizedStrings.AlertElementKey,
	GroupName = LocalizedStrings.InformKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/notifying/notification.html")]
public sealed class AlertDiagramElement : DiagramElement
{
	/// <inheritdoc />
	public override Guid TypeId { get; } = "CE4790AA-00F5-4D99-A8AE-FBA174172DC1".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Bell";

	private readonly DiagramElementParam<AlertNotifications> _type;

	/// <summary>
	/// Alert type.
	/// </summary>
	public AlertNotifications Type
	{
		get => _type.Value;
		set => _type.Value = value;
	}

	private readonly DiagramElementParam<ITelegramChannel> _telegramChannel;

	/// <summary>
	/// Telegram channel.
	/// </summary>
	public ITelegramChannel TelegramChannel
	{
		get => _telegramChannel.Value;
		set => _telegramChannel.Value = value;
	}

	private readonly DiagramElementParam<string> _caption;

	/// <summary>
	/// Signal header.
	/// </summary>
	public string Caption
	{
		get => _caption.Value;
		set => _caption.Value = value;
	}

	private readonly DiagramElementParam<string> _message;

	/// <summary>
	/// Alert text.
	/// </summary>
	public string Message
	{
		get => _message.Value;
		set => _message.Value = value;
	}

	private readonly ISet<AlertNotifications> _alertAlerts;

	/// <summary>
	/// Initializes a new instance of the <see cref="AlertDiagramElement"/>.
	/// </summary>
	public AlertDiagramElement()
	{
		_alertAlerts = Scope<CompositionLoadingContext>.Current?.Value.AllowAlerts;

		AddInput(StaticSocketIds.Flag, LocalizedStrings.Flag, DiagramSocketType.Bool, OnProcess);

		_type = AddParam(nameof(Type), AlertNotifications.Popup)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Alerts, LocalizedStrings.Type, LocalizedStrings.SignalType, 10)
			.SetOnValueChangedHandler(value =>
			{
				SetElementName(value.GetDisplayName());
			});

		_telegramChannel = AddParam<ITelegramChannel>(nameof(TelegramChannel))
			.SetBasic(true)
			.SetSaveLoadHandlers(c =>
			{
				if (c is null)
					return null;

				return new SettingsStorage()
					.Set(nameof(c.Id), c.Id)
				;
			}, s =>
			{
				if (s.GetValue<long?>(nameof(ITelegramChannel.Id)) is not long channelId)
					return null;

				return channelId.TryFindChannel();
			})
			.SetDisplay(LocalizedStrings.Alerts, LocalizedStrings.Telegram, LocalizedStrings.TelegramChannel, 20)
			.SetEditor(new EditorAttribute(typeof(ITelegramChannelEditor), typeof(ITelegramChannelEditor)))
			;

		_caption = AddParam(nameof(Caption), string.Empty)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Alerts, LocalizedStrings.Header, LocalizedStrings.SignalHeader, 30);

		_message = AddParam(nameof(Message), string.Empty)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Alerts, LocalizedStrings.Message, LocalizedStrings.SignalText, 40);
	}

	private bool _canProcess;

	/// <inheritdoc />
	protected override void OnStart(DateTimeOffset time)
	{
		base.OnStart(time);

		if (Strategy.IsBacktesting)
			_canProcess = Type == AlertNotifications.Log;
		else
			_canProcess = _alertAlerts?.Contains(Type) != false;
	}

	private void OnProcess(DiagramSocketValue value)
	{
		if (!_canProcess || !value.GetValue<bool>())
			return;

		var svc = AlertServicesRegistry.TryNotificationService;

		if (svc != null)
			_ = svc.NotifyAsync(Type, TelegramChannel?.Id, LogLevel, Caption, Message, value.Time, default);
	}
}
