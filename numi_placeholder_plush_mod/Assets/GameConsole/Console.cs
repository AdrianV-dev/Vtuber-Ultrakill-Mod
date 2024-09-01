using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameConsole.Commands;
using GameConsole.CommandTree;
using plog;
using plog.Handlers;
using plog.Models;
using plog.unity.Handlers;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameConsole
{

	[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
	public class Console : MonoSingleton<Console>, plog.Handlers.ILogHandler
	{
		[Serializable]
		public class AutocompletePanel
		{
			public TMP_Text text;

			public Image background;
		}

		public static readonly plog.Logger Log = new plog.Logger("Console");

		public bool pinned;

		public bool consoleOpen;

		public List<ConsoleLog> logs = new List<ConsoleLog>();

		public readonly HashSet<Level> logLevelFilter = new HashSet<Level>(Enum.GetValues(typeof(Level)).Cast<Level>());

		private int logLevelCount = Enum.GetValues(typeof(Level)).Length;

		public int errorCount;

		public int warningCount;

		public int infoCount;

		private readonly List<LogLine> logLinePool = new List<LogLine>();

		[SerializeField]
		private GameObject consoleContainer;

		[SerializeField]
		private CanvasGroup consoleBlocker;

		[SerializeField]
		private TMP_InputField consoleInput;

		[Space]
		[SerializeField]
		private LogLine logLine;

		[SerializeField]
		private GameObject logContainer;

		[Space]
		[SerializeField]
		private GameObject scroller;

		[SerializeField]
		private TMP_Text scrollText;

		[SerializeField]
		private TMP_Text openBindText;

		[SerializeField]
		private AutocompletePanel[] autocompletePanels;

		[Space]
		public ErrorBadge errorBadge;

		[Space]
		[SerializeField]
		private GameObject[] hideOnPin;

		[SerializeField]
		private GameObject[] hideOnPinNoReopen;

		[SerializeField]
		private Image[] backgrounds;

		[SerializeField]
		private CanvasGroup masterGroup;

		[Space]
		public ConsoleWindow consoleWindow;

		private const int MaxLogLines = 20;

		private bool openedDuringPause;

		private OptionsManager rememberedOptionsManager;

		public readonly Dictionary<string, ICommand> recognizedCommands = new Dictionary<string, ICommand>();

		public readonly HashSet<Type> registeredCommandTypes = new HashSet<Type>();

		private bool logsDirty;

		private int scrollState;

		private UnscaledTimeSince timeSincePgHeld;

		private UnscaledTimeSince timeSinceScrollTick;

		private List<string> commandHistory = new List<string>();

		private int commandHistoryIndex = -1;

		public Action onError;

		public Binds binds;

		private List<string> suggestions = new List<string>();

		private int selectedSuggestionIndex;

		private int suggestionStartIndex;

		private PconAdapter pconAdapter = new PconAdapter();

		private DebugLogProxy plogHandler = new DebugLogProxy();

		public static bool IsOpen
		{
			get
			{
				if (MonoSingleton<Console>.Instance != null && MonoSingleton<Console>.Instance.consoleContainer != null)
				{
					return MonoSingleton<Console>.Instance.consoleContainer.activeSelf;
				}
				return false;
			}
		}

		public bool ExtractStackTraces { get; private set; }

		private List<ConsoleLog> filteredLogs => logs.Where((ConsoleLog l) => logLevelFilter.Contains(l.log.Level)).ToList();

		protected override void Awake()
		{
			if (MonoSingleton<Console>.Instance != this)
			{
				UnityEngine.Object.Destroy(base.gameObject);
				return;
			}
			base.Awake();
			binds = new Binds();
			binds.Initialize();
			AutocompletePanel[] array = autocompletePanels;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].background.transform.parent.gameObject.SetActive(value: true);
			}
			SelectSuggestion(0);
			if (binds.registeredBinds != null && binds.registeredBinds.ContainsKey("open") && binds.registeredBinds["open"].Action != null)
			{
				openBindText.text = binds.registeredBinds["open"].Action.GetBindingDisplayString();
			}
			RegisterCommands(new ICommand[4]
			{
			new Help(),
			new Clear(),
			new Echo(),
			new Exit()
			});
			RegisterCommands(new ICommand[9]
			{
			new Prefs(this),
			new Scenes(),
			new Scene(),
			new ConsoleCmd(this),
			new Style(this),
			new Buffs(this),
			new MapVar(this),
			new InputCommands(this),
			new Rumble(this)
			});
			if (UnityEngine.Debug.isDebugBuild)
			{
				RegisterCommand(new GameConsole.Commands.Debug(this));
				RegisterCommand(new Pcon(this));
			}
			for (int j = 0; j < 20; j++)
			{
				LogLine logLine = UnityEngine.Object.Instantiate(this.logLine, logContainer.transform, worldPositionStays: false);
				logLine.Wipe();
				logLine.gameObject.SetActive(value: false);
				logLinePool.Add(logLine);
			}
			this.logLine.gameObject.SetActive(value: false);
			Application.logMessageReceived += HandleUnityLog;
			RegisterPLogHandler();
			if (!Consts.CONSOLE_ERROR_BADGE)
			{
				errorBadge.SetEnabled(enabled: false);
			}
			if (UnityEngine.Debug.isDebugBuild && MonoSingleton<PrefsManager>.Instance.GetBoolLocal("pcon.autostart"))
			{
				StartPCon();
			}
			consoleInput.onValueChanged.AddListener(FindSuggestions);
		}

		private void Start()
		{
			ExtractStackTraces = UnityEngine.Debug.isDebugBuild || MonoSingleton<PrefsManager>.Instance.GetBoolLocal("forceStackTraceExtraction");
		}

		public void RegisterPLogHandler()
		{
			plog.Logger.Root.AddHandler(plogHandler);
			plog.Logger.Root.AddHandler(this);
		}

		public void StartPCon()
		{
			if (pconAdapter == null)
			{
				return;
			}
			if (!pconAdapter.PConLibraryExists())
			{
				pconAdapter = null;
				return;
			}
			pconAdapter.StartPConClient(ProcessInput, delegate
			{
				MonoSingleton<CheatsController>.Instance.ActivateCheats();
			});
		}

		public void UpdateDisplayString()
		{
			openBindText.text = binds.registeredBinds["open"].Action.GetBindingDisplayString();
		}

		public bool CheatBlocker()
		{
			if (MonoSingleton<CheatsController>.Instance == null && CheatsManager.KeepCheatsEnabled)
			{
				return false;
			}
			if (MonoSingleton<CheatsController>.Instance == null || !MonoSingleton<CheatsController>.Instance.cheatsEnabled)
			{
				Log.Error("Cheats aren't enabled!");
				return true;
			}
			return false;
		}

		public void RegisterCommands(IEnumerable<ICommand> commands)
		{
			foreach (ICommand command in commands)
			{
				RegisterCommand(command);
			}
		}

		public void RegisterCommand(ICommand command)
		{
			if (registeredCommandTypes.Contains(command.GetType()))
			{
				Log.Warning("Command " + command.GetType().Name + " already registered!");
				return;
			}
			recognizedCommands.Add(command.Command.ToLower(), command);
			registeredCommandTypes.Add(command.GetType());
			if (command is IConsoleLogger consoleLogger)
			{
				consoleLogger.Log.NotifyParent = false;
				consoleLogger.Log.AddHandler(this);
				consoleLogger.Log.AddHandler(plogHandler);
			}
		}

		public void Clear()
		{
			scrollState = 0;
			errorCount = 0;
			warningCount = 0;
			infoCount = 0;
			logs.Clear();
			RepopulateLogs();
			UpdateScroller();
			errorBadge.SetEnabled(enabled: false, hide: false);
		}

		private void IncrementCounters(Level type)
		{
			if (scrollState > 0)
			{
				scrollState++;
				UpdateScroller();
			}
			switch (type)
			{
				case Level.Error:
					errorCount++;
					break;
				case Level.Warning:
					warningCount++;
					break;
				default:
					infoCount++;
					break;
				case Level.CLI:
					break;
			}
		}

		public void UpdateFilters(bool showErrors, bool showWarnings, bool showLogs)
		{
			if (showErrors)
			{
				logLevelFilter.Add(Level.Error);
			}
			else
			{
				logLevelFilter.Remove(Level.Error);
			}
			if (showWarnings)
			{
				logLevelFilter.Add(Level.Warning);
			}
			else
			{
				logLevelFilter.Remove(Level.Warning);
			}
			if (showLogs)
			{
				logLevelFilter.Add(Level.Info);
				logLevelFilter.Add(Level.Fine);
				logLevelFilter.Add(Level.Finer);
				logLevelFilter.Add(Level.Finest);
			}
			else
			{
				logLevelFilter.Remove(Level.Info);
				logLevelFilter.Remove(Level.Fine);
				logLevelFilter.Remove(Level.Finer);
				logLevelFilter.Remove(Level.Finest);
			}
			RepopulateLogs();
		}

		public void SetForceStackTraceExtraction(bool value)
		{
			ExtractStackTraces = value;
			MonoSingleton<PrefsManager>.Instance.SetBoolLocal("forceStackTraceExtraction", value);
		}

		public string[] Parse(string text)
		{
			return text.Split(' ');
		}

		private void ProcessUserInput(string text)
		{
			if (!string.IsNullOrEmpty(text))
			{
				ProcessInput(text);
			}
		}

		public void ProcessInput(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			string[] array = Parse(text);
			if (array.Length == 0)
			{
				return;
			}
			string text2 = array[0];
			text2 = text2.ToLower();
			Log.Log("> " + text, Level.CLI);
			if (text.ToLower() == "sv_cheats 1")
			{
				Log.Log("To enable cheats, you must enter the Konami code in-game.", Level.Warning);
				return;
			}
			if (recognizedCommands.TryGetValue(text2, out var value))
			{
				try
				{
					value.Execute(this, array.Skip(1).ToArray());
					return;
				}
				catch (Exception ex)
				{
					Log.Log("Command <b>'" + text2 + "'</b> failed.\n" + ex.Message, Level.Error, null, ex.StackTrace);
					return;
				}
			}
			Log.Log("Unknown command: '" + text2 + "'", Level.Warning);
		}

		private void ScrollUp()
		{
			timeSinceScrollTick = 0f;
			scrollState++;
			if (scrollState > logs.Count - 1)
			{
				scrollState = logs.Count - 1;
			}
			if (logs.Count == 0)
			{
				scrollState = 0;
			}
			UpdateScroller();
			RepopulateLogs();
		}

		private void ScrollDown()
		{
			timeSinceScrollTick = 0f;
			scrollState--;
			if (scrollState < 0)
			{
				scrollState = 0;
			}
			UpdateScroller();
			RepopulateLogs();
		}

		private void DefaultDevConsoleOff()
		{
		}

		private void OnGUI()
		{
			DefaultDevConsoleOff();
		}

		private void LateUpdate()
		{
			DefaultDevConsoleOff();
		}

		private void Update()
		{
			DefaultDevConsoleOff();
			bool activeSelf = consoleContainer.activeSelf;
			if (binds.OpenPressed || (consoleOpen && Input.GetKeyDown(KeyCode.Escape)))
			{
				consoleOpen = !consoleOpen;
				if (consoleOpen)
				{
					GameStateManager.Instance.RegisterState(new GameState("console", hideOnPin)
					{
						cursorLock = LockMode.Unlock,
						playerInputLock = LockMode.Lock,
						cameraInputLock = LockMode.Lock,
						priority = 100
					});
					if (logsDirty)
					{
						RepopulateLogs();
					}
				}
				else
				{
					GameStateManager.Instance.PopState("console");
				}
				if (pinned)
				{
					GameObject[] array = hideOnPin;
					for (int i = 0; i < array.Length; i++)
					{
						array[i].SetActive(consoleOpen);
					}
					if (!consoleOpen)
					{
						array = hideOnPinNoReopen;
						for (int i = 0; i < array.Length; i++)
						{
							array[i].SetActive(value: false);
						}
					}
					Image[] array2 = backgrounds;
					for (int i = 0; i < array2.Length; i++)
					{
						array2[i].enabled = consoleOpen;
					}
				}
				else
				{
					consoleContainer.SetActive(consoleOpen);
				}
				masterGroup.interactable = consoleOpen;
				bool flag = activeSelf;
				if (pinned)
				{
					flag = !consoleOpen;
				}
				if (flag)
				{
					if ((bool)MonoSingleton<OptionsManager>.Instance && binds.OpenPressed && !openedDuringPause && MonoSingleton<OptionsManager>.Instance == rememberedOptionsManager && SceneHelper.CurrentScene != "Main Menu")
					{
						MonoSingleton<OptionsManager>.Instance.UnPause();
					}
					StopAllCoroutines();
				}
				else
				{
					if ((bool)MonoSingleton<OptionsManager>.Instance && SceneHelper.CurrentScene != "Main Menu")
					{
						openedDuringPause = MonoSingleton<OptionsManager>.Instance.paused;
						rememberedOptionsManager = MonoSingleton<OptionsManager>.Instance;
						MonoSingleton<OptionsManager>.Instance.Pause();
					}
					consoleBlocker.alpha = 0f;
					StartCoroutine(FadeBlockerIn());
					consoleInput.ActivateInputField();
					errorBadge.Dismiss();
				}
			}
			if (!consoleOpen)
			{
				return;
			}
			if (binds.ScrollUpPressed || Input.mouseScrollDelta.y > 0f)
			{
				timeSincePgHeld = 0f;
				ScrollUp();
			}
			if (binds.ScrollDownPressed || Input.mouseScrollDelta.y < 0f)
			{
				timeSincePgHeld = 0f;
				ScrollDown();
			}
			if ((binds.ScrollUpHeld || binds.ScrollDownHeld) && (float)timeSincePgHeld > 0.5f)
			{
				bool scrollUpHeld = binds.ScrollUpHeld;
				if ((float)timeSinceScrollTick > 0.05f)
				{
					if (scrollUpHeld)
					{
						ScrollUp();
					}
					else
					{
						ScrollDown();
					}
				}
			}
			if (binds.ScrollToTopPressed)
			{
				scrollState = logs.Count - 1;
				UpdateScroller();
				RepopulateLogs();
			}
			if (binds.ScrollToBottomPressed)
			{
				scrollState = 0;
				UpdateScroller();
				RepopulateLogs();
			}
			if (suggestions.Count > 0)
			{
				if (binds.AutocompletePressed || binds.SubmitPressed)
				{
					consoleInput.text = suggestions[selectedSuggestionIndex];
					consoleInput.caretPosition = consoleInput.text.Length;
					consoleInput.ActivateInputField();
				}
				if (binds.CommandHistoryUpPressed)
				{
					SelectSuggestion(selectedSuggestionIndex + 1, wrap: true);
					consoleInput.caretPosition = consoleInput.text.Length;
				}
				if (binds.CommandHistoryDownPressed)
				{
					SelectSuggestion(selectedSuggestionIndex - 1, wrap: true);
					consoleInput.caretPosition = consoleInput.text.Length;
				}
				return;
			}
			if (binds.CommandHistoryUpPressed)
			{
				commandHistoryIndex++;
				if (commandHistoryIndex > commandHistory.Count - 1)
				{
					commandHistoryIndex = commandHistory.Count - 1;
				}
				consoleInput.text = ((commandHistoryIndex == -1) ? "" : commandHistory[commandHistoryIndex]);
				consoleInput.caretPosition = consoleInput.text.Length;
			}
			if (binds.CommandHistoryDownPressed)
			{
				commandHistoryIndex--;
				if (commandHistoryIndex < -1)
				{
					commandHistoryIndex = -1;
				}
				consoleInput.text = ((commandHistoryIndex == -1) ? "" : commandHistory[commandHistoryIndex]);
				consoleInput.caretPosition = consoleInput.text.Length;
			}
			if (binds.SubmitPressed)
			{
				consoleInput.ActivateInputField();
				if (!string.IsNullOrEmpty(consoleInput.text))
				{
					ProcessUserInput(consoleInput.text);
					commandHistory = commandHistory.Prepend(consoleInput.text).ToList();
					commandHistoryIndex = -1;
					consoleInput.text = string.Empty;
				}
			}
		}

		private void UpdateScroller()
		{
			if (scrollState == 0)
			{
				scroller.SetActive(value: false);
				return;
			}
			scroller.SetActive(value: true);
			scrollText.text = $"{scrollState} lines below";
		}

		private IEnumerator FadeBlockerIn()
		{
			consoleBlocker.alpha = 0f;
			while (consoleBlocker.alpha < 1f)
			{
				consoleBlocker.alpha += 0.2f;
				yield return new WaitForSecondsRealtime(0.03f);
			}
			consoleBlocker.alpha = 1f;
		}

		private void SelectSuggestion(int newIndex, bool wrap = false)
		{
			if (suggestions.Count == 0)
			{
				AutocompletePanel[] array = autocompletePanels;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].background.gameObject.SetActive(value: false);
				}
				return;
			}
			int num = Mathf.Max(0, suggestions.Count);
			int num2 = newIndex;
			if (wrap)
			{
				int num3 = num2 % num;
				if (num3 < 0)
				{
					num3 += num;
				}
				num2 = num3;
			}
			else
			{
				num2 = Mathf.Clamp(num2, 0, num - 1);
			}
			selectedSuggestionIndex = num2;
			ShowSuggestions(num2);
		}

		private void ShowSuggestions(int selected)
		{
			int num = suggestionStartIndex + Mathf.Min(suggestions.Count, autocompletePanels.Length - 1);
			if (selected < suggestionStartIndex)
			{
				suggestionStartIndex = selected;
			}
			if (selected > num)
			{
				int num2 = selected - num;
				suggestionStartIndex += num2;
			}
			int num3 = suggestionStartIndex;
			suggestions.Skip(num3 - 1).Take(autocompletePanels.Length);
			for (int i = 0; i < autocompletePanels.Length; i++)
			{
				AutocompletePanel autocompletePanel = autocompletePanels[i];
				int num4 = num3 + i;
				if (num4 >= suggestions.Count)
				{
					autocompletePanel.background.gameObject.SetActive(value: false);
					continue;
				}
				autocompletePanel.text.text = "> " + suggestions[num4];
				if (num4 == selectedSuggestionIndex)
				{
					autocompletePanel.background.color = Color.gray;
				}
				else
				{
					autocompletePanel.background.color = Color.black;
				}
				autocompletePanel.background.gameObject.SetActive(value: true);
			}
		}

		private void FindSuggestions(string value)
		{
			suggestions.Clear();
			if (value == "")
			{
				SelectSuggestion(0);
				return;
			}
			string[] array = Parse(value);
			Queue<string> queue = new Queue<string>(array.Skip(1));
			if (recognizedCommands.TryGetValue(array[0], out var value2))
			{
				if (value2 is CommandRoot commandRoot)
				{
					(string, Branch) tuple = commandRoot.FindLongestMatchingBranch(commandRoot.Root, queue);
					string soFar = tuple.Item1;
					IEnumerable<Branch> source = tuple.Item2.children.Where((Node n) => n is Branch).Cast<Branch>();
					if (queue.Count > 0)
					{
						string next = queue.Peek();
						source = source.Where((Branch n) => n.name.StartsWith(next));
						suggestions.AddRange(source.Select((Branch n) => soFar + " " + n.name));
					}
				}
			}
			else
			{
				foreach (KeyValuePair<string, ICommand> recognizedCommand in recognizedCommands)
				{
					if (recognizedCommand.Key.StartsWith(array[0]))
					{
						suggestions.Add(recognizedCommand.Value.Command ?? "");
					}
				}
			}
			SelectSuggestion(0);
		}

		private void OnDisable()
		{
			Application.logMessageReceived -= HandleUnityLog;
		}

		private void InsertLog(Log log)
		{
			IncrementCounters(log.Level);
			if (IsOpen)
			{
				RepopulateLogs();
			}
			else
			{
				logsDirty = true;
			}
		}

		private void RepopulateLogs()
		{
			List<ConsoleLog> list = ((logLevelFilter.Count == logLevelCount) ? logs : filteredLogs);
			for (int i = 0; i < logLinePool.Count; i++)
			{
				if (list.Count - i - 1 - scrollState < 0)
				{
					logLinePool[logLinePool.Count - i - 1].gameObject.SetActive(value: false);
				}
				else if (logLinePool.Count - i - 1 >= 0)
				{
					logLinePool[logLinePool.Count - i - 1].gameObject.SetActive(value: true);
					logLinePool[logLinePool.Count - i - 1].PopulateLine(list[list.Count - i - 1 - scrollState]);
				}
			}
		}

		private void HandleUnityLog(string message, string stacktrace, LogType type)
		{
			DefaultDevConsoleOff();
			if (!plogHandler.SuppressingNativeLogs)
			{
				plogHandler.HandleNativeUnityLog(message, stacktrace, type);
			}
		}

		public void Handle(plog.Logger source, Log log)
		{
			if (log.StackTrace == null && (ExtractStackTraces || log.Level == Level.Error))
			{
				log.StackTrace = StackTraceUtility.ExtractStackTrace();
			}
			logs.Add(new ConsoleLog(log, source));
			InsertLog(log);
			if (log.Level == Level.Error)
			{
				onError?.Invoke();
			}
		}
	}
}