using Discord;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Tools;
using FezGame;
using FezGame.Services;
using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using MonoMod.RuntimeDetour;
using Common;

namespace RichFEZence
{
	public class RichFezence : DrawableGameComponent
	{
		const string MOD_NAME = "RichFEZence";

		enum CubeShardPieceDisplay
		{
			None,
			Separate,
			Decimal
		}

		enum SpecialPresenceState
		{
			None,
			IntroMenus,
			EldersGlitch,
			MenusGlitch,
			EndCutscene32,
			EndCutscene64
		}

		[ServiceDependency]
		public ILevelManager LevelManager { private get; set; }

		[ServiceDependency]
		public IGomezService GomezService { private get; set; }

		[ServiceDependency]
		public IGameStateManager GameState { private get; set; }

		[ServiceDependency]
		public IEngineStateManager EngineState { private get; set; }

		private Discord.Discord discord;
		private ActivityManager activityState;

		const long CLIENT_ID = 1228717137120591983;

		readonly TextInfo ti = new CultureInfo("en-US", false).TextInfo;

		public Hook introMenuHook;
		public Hook eldersGlitchHook;
		public Hook end32StartHook;
		public Hook end32EndHook;
		public Hook end64StartHook;
		public Hook end64EndHook;

		public Activity GetLoadingActivity(bool glitched = false)
		{
			return new Activity
			{
				Details = glitched ? PresenceUtils.GlitchString("Loading, please wait...") : "Loading, please wait...",
				State = "v" + Fez.Version,
				Assets =
				{
					LargeImage = glitched ? "start_menus_glitch" : "start_menus"
				}
			};
		}

		public RichFezence(Game game) : base(game)
		{
			try
			{
				discord = new Discord.Discord(CLIENT_ID, (ulong)CreateFlags.NoRequireDiscord);
				activityState = discord.GetActivityManager();
				activityState.UpdateActivity(GetLoadingActivity(), (result) => { });
			}
			catch (Exception ex)
			{
				// discord isn't open
				discord = null;
				throw ex;
			}
		}

		private void UpdateCurrentActivityState()
		{
			if (GameState.SaveData != null)
			{
				// all references in code are lowercase
				string current_level_name = LevelManager.Name.ToLower();
				
				string largeImageName = current_level_name;
				if(image_replacements.ContainsKey(largeImageName))
				{
					// some levels need to piggyback off another level's large image (mostly ones that are 2D versions with no map icon)
					largeImageName = image_replacements[largeImageName];
				}
				else if(!ASSET_NAMES.Contains(largeImageName))
				{
					// some levels won't have an image (modded levels?) so for now those use the generic FEZ logo icon
					largeImageName = "start_menus";
				}

				string largeImageText = current_level_name;
				if(direct_level_name_replacements.ContainsKey(largeImageText))
				{
					largeImageText = direct_level_name_replacements[largeImageText];
				} 
				else
				{
					// remove parts of the level name that are non-descriptive
					largeImageText = level_name_trims.Aggregate(largeImageText, (text, trim) => Regex.Replace(text, trim, ""));
					largeImageText = ti.ToTitleCase(largeImageText);
					// replace certain segments (irrespective of case) to expand abbreviated forms and correct capitalization
					largeImageText = segment_level_name_replacements.Aggregate(largeImageText, (text, replace) => Regex.Replace(text, replace.Key, replace.Value, RegexOptions.IgnoreCase));
					largeImageText = largeImageText.Replace("_", " ");
				}

				string details = "Exploring";
				if (EngineState.Paused)
				{
					details = "Paused";
				} 
				else if (details_replacements.ContainsKey(current_level_name))
				{
					details = details_replacements[current_level_name];
				}

				string state = $"🟨 {GameState.SaveData.CubeShards}{(GameState.SaveData.SecretCubes > 0 ? $" + 🟦 {GameState.SaveData.SecretCubes}" : "")}";
				if(special_state_levels.ContainsKey(current_level_name))
				{
					// certain levels can have Special State
					// right now it's just Temple of Love but still
					state = special_state_levels[current_level_name].Invoke(GameState);
				} 
				else if(state_disabled_levels.Contains(current_level_name))
				{
					state = "";
				}
				var activity = new Activity
				{
					Details = PresenceUtils.Utf16ToUtf8(details),
					State = PresenceUtils.Utf16ToUtf8(state),
					Assets =
					{
						LargeImage = largeImageName,
						LargeText = PresenceUtils.Utf16ToUtf8(largeImageText)
					}
				};
				activityState.UpdateActivity(activity, (result) => { });
			}
		}

		public override void Initialize()
		{
			base.Initialize();

			GomezService.CollectedSplitUpCube += UpdateCurrentActivityState;
			GomezService.CollectedAnti += UpdateCurrentActivityState;
			GomezService.CollectedShard += UpdateCurrentActivityState;
			GomezService.CollectedGlobalAnti += UpdateCurrentActivityState;
			EngineState.PauseStateChanged += UpdateCurrentActivityState;
			LevelManager.LevelChanged += UpdateCurrentActivityState;
		}

		public override void Update(GameTime gameTime)
		{
			if (discord != null)
			{
				try
				{
					discord.RunCallbacks();
				}
				catch (Exception)
				{
					// if RunCallbacks throws, it's because the user closed Discord
					// we don't care about handling this, aside from disabling anything trying to interact with Discord
					discord = null;
				}
			}

			base.Update(gameTime);
		}

		protected override void Dispose(bool disposing)
		{
			if (discord != null && activityState != null)
			{
				activityState.ClearActivity((result) => { });
				discord.Dispose();
			}
			GomezService.CollectedSplitUpCube -= UpdateCurrentActivityState;
			GomezService.CollectedAnti -= UpdateCurrentActivityState;
			GomezService.CollectedShard -= UpdateCurrentActivityState;
			GomezService.CollectedGlobalAnti -= UpdateCurrentActivityState;
			EngineState.PauseStateChanged -= UpdateCurrentActivityState;
			LevelManager.LevelChanged -= UpdateCurrentActivityState;
			base.Dispose(disposing);
		}

		readonly Dictionary<string, string> image_replacements = new Dictionary<string, string>() {
			["gomez_house_2d"] = "gomez_house",
			["villageville_2d"] = "villageville_3d",
			["geezer_house_2d"] = "geezer_house",
			["parlor_2d"] = "parlor",
			["school_2d"] = "school",
			["kitchen_2d"] = "kitchen",
			["villageville_3d_end_32"] = "descending",
			["villageville_3d_end_64"] = "transcending"
		};

		readonly Dictionary<string, string> details_replacements = new Dictionary<string, string>()
		{
			["elders"] = "Being enlightened",
			["pyramid"] = "Exploring...?",
			["hex_rebuild"] = "Rebuilding",
			["villageville_3d_end_32"] = "Descending",
			["villageville_3d_end_64"] = "Transcending"
		};

		readonly string[] state_disabled_levels = ["villageville", "elders", "villageville_3d_end_32", "villageville_3d_end_64"];

		readonly Dictionary<string, Func<IGameStateManager, string>> special_state_levels = new Dictionary<string, Func<IGameStateManager, string>>()
		{
			["temple_of_love"] = (gameState) => $"{(gameState.SaveData.HasDoneHeartReboot ? "" : $"[ {(gameState.SaveData.PiecesOfHeart > 0 ? "▀" : " ")}{(gameState.SaveData.PiecesOfHeart > 1 ? "▄" : " ")}{(gameState.SaveData.PiecesOfHeart > 2 ? "▀" : " ")} ]")}"
		};

		readonly string[] level_name_trims = ["_a$", "_b$", "_c$", "_2d$", "_3d$", "_one$", "_two$", "_2$", "_three$", "_alt$"];

		readonly Dictionary<string, string> segment_level_name_replacements = new Dictionary<string, string>()
		{
			["cmy"] = "CMYKave",
			["indust_"] = "Industrial",
			["_int$"] = " Interior",
			["qr"] = "QR"
		};

		readonly Dictionary<string, string> direct_level_name_replacements = new Dictionary<string, string>()
		{
			["elders"] = "???",
			["boileroom"] = "Boiler Room",
			["hex_rebuild"] = "???",
			["villageville_3d_end_32"] = "???",
			["villageville_3d_end_64"] = "???"
		};

		readonly string[] ASSET_NAMES = ["abandoned_a",
											"abandoned_b",
											"abandoned_c",
											"ancient_walls",
											"arch",
											"bell_tower",
											"big_owl",
											"big_tower",
											"boileroom",
											"cabin_interior_b",
											"cabin_nature",
											"clock",
											"cmy",
											"cmy_b",
											"cmy_fork",
											"code_machine",
											"crypt",
											"descending",
											"elders",
											"elders_glitch",
											"extractor_a",
											"five_towers",
											"five_towers_cave",
											"fox",
											"fractal",
											"geezer_house",
											"globe",
											"globe_int",
											"gomez_house",
											"graveyard_a",
											"graveyard_gate",
											"graveyard_treasure_a",
											"grave_cabin",
											"grave_ghost",
											"grave_lesser_gate",
											"grave_treasure_a",
											"industrial_city",
											"industrial_hub",
											"industrial_superspin",
											"indust_abandoned_a",
											"kitchen",
											"lava",
											"lava_fork",
											"lava_skull",
											"library_interior",
											"lighthouse",
											"lighthouse_house_a",
											"lighthouse_spin",
											"mausoleum",
											"memory_core",
											"mine_a",
											"mine_bomb_pillar",
											"mine_wrap",
											"nature_hub",
											"nuzu_abandoned_a",
											"nuzu_abandoned_b",
											"nuzu_boilerroom",
											"nuzu_dorm",
											"nuzu_school",
											"observatory",
											"oldschool",
											"oldschool_ruins",
											"oldscool",
											"orrery",
											"orrery_b",
											"owl",
											"parlor",
											"pivot_one",
											"pivot_three",
											"pivot_three_cave",
											"pivot_two",
											"pivot_watertower",
											"purple_lodge",
											"purple_lodge_ruin",
											"quantum",
											"rails",
											"ritual",
											"school",
											"sewer_fork",
											"sewer_geyser",
											"sewer_hub",
											"sewer_lesser_gate_b",
											"sewer_pillars",
											"sewer_pivot",
											"sewer_qr",
											"sewer_qr_sony",
											"sewer_start",
											"sewer_to_lava",
											"sewer_treasure_one",
											"sewer_treasure_two",
											"showers",
											"skull",
											"skull_b",
											"spinning_plates",
											"spire_alt",
											"stargate",
											"stargate_ruins",
											"start_menus",
											"superspin_cave",
											"telescope",
											"temple_of_love",
											"throne",
											"towers_a",
											"towers_b",
											"towers_c",
											"transcending",
											"tree",
											"tree_crumble",
											"tree_of_death",
											"tree_roots",
											"tree_sky",
											"triple_pivot_cave",
											"two_walls",
											"villageville_3d",
											"village_exit",
											"visitor",
											"wall_a",
											"wall_b",
											"wall_hole",
											"wall_interior_a",
											"wall_interior_b",
											"wall_interior_hole",
											"wall_kitchen",
											"wall_school",
											"wall_village",
											"waterfall",
											"watertower_secret",
											"water_pyramid",
											"water_tower",
											"water_wheel",
											"water_wheel_b",
											"weightswitch_temple",
											"well_2",
											"windmill_cave",
											"windmill_int",
											"zu_4_side",
											"zu_bridge",
											"zu_city",
											"zu_city_ruins",
											"zu_code_loop",
											"zu_fork",
											"zu_heads",
											"zu_house_empty",
											"zu_house_empty_b",
											"zu_house_qr",
											"zu_house_qr_sony",
											"zu_house_ruin_gate",
											"zu_house_ruin_visitors",
											"zu_house_scaffolding",
											"zu_library",
											"zu_switch",
											"zu_switch_b",
											"zu_tetris",
											"zu_throne_ruins",
											"zu_unfold",
											"zu_zuish"];
	}
}
