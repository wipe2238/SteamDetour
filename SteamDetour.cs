using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using Microsoft.Win32;

// GameOverlayUI.exe -pid {game.Id} -manuallyclearframes 0
//    overlay, screenshot, playtime
// GameOverlayUI.exe -steampid {steam.Id} -pid {game.Id} -manuallyclearframes 0
//    overlay, screenshot, playtime, friends list, chat, browser
//    defunc (browser, friends list, chat): does not react to keypress

namespace SteamDetour
{
    internal static class SteamDetour
    {
        static readonly string[] steamRegistry = {
            @"SOFTWARE\Wow6432Node\Valve\Steam\InstallPath",
            @"SOFTWARE\Valve\Steam\InstallPath"
        };

        static Process steam;
        static Process game;
        static Process overlay;

        static void Error( string caption, params string[] message )
        {
            MessageBox.Show( string.Join( "\n", message ), $"SteamDetour : {(string.IsNullOrEmpty( caption ) ? "Error" : caption)}", MessageBoxButtons.OK, MessageBoxIcon.Error );
            Environment.Exit( 1 );
        }

        [STAThread]
        static void Main( string[] args )
        {
            // Validate arguments
            if( args.Length < 2 )
                Error( "Usage", @"C:\Path\SteamDetour.exe C:\Path\Game.exe %command%", "", "Full path to both executables is required." );
            else if( !File.Exists( args[0] ) )
                Error( "File not found", args[0] );
            else if( !Path.IsPathRooted( args[0] ) )
                Error( "Invalid path", "Full path to game executable required", args[0] );
            string gameExe = args[0];

            // Find the Steam installation directory
            // Required for base functionality
            string overlayExe = string.Empty;
            foreach( string registry in steamRegistry )
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey( Path.GetDirectoryName( registry ) );
                if( key == null )
                    continue;

                object value = key.GetValue( Path.GetFileName( registry ) );
                if( value != null )
                {
                    if( !Directory.Exists( (string)value ) )
                        continue;

                    overlayExe = Path.Combine( (string)value, "GameOverlayUI.exe" );
                    break;
                }
            }
            if( string.IsNullOrEmpty( overlayExe ) )
                Error( string.Empty, "Cannot find Steam installation directory" );
            else if( !File.Exists( overlayExe ) )
                Error( "File not found", overlayExe );
            Debug.WriteLine( $"OVERLAY = {overlayExe}" );

            // Find the Steam process
            // Required for browser
            Process[] proc = Process.GetProcessesByName( "Steam" );
            if( proc.Length == 0 )
                Error( "Process not found", "Cannot find Steam process" );
            else if( proc.Length != 1 )
                Error( "Process not found", "More than one Steam process running?" );
            steam = proc[0];

            // Run the game
            Debug.WriteLine( $"Detour {args[1]} -> {gameExe}" );
            ProcessStartInfo gameInfo = new ProcessStartInfo();
            gameInfo.WorkingDirectory = Path.GetDirectoryName( gameExe );
            gameInfo.FileName = gameExe;
            game = Process.Start( gameInfo );

            bool hooked = false;
            while( !hooked )
            {
                game.WaitForExit( 1000 );

                // Run the overlay
                // based on https://github.com/SuiMachine/Steam-Overlay-Hooking-Helper
                ProcessStartInfo overlayInfo = new ProcessStartInfo();
                overlayInfo.WorkingDirectory = Path.GetDirectoryName( overlayExe );
                overlayInfo.FileName = overlayExe;
                overlayInfo.Arguments = $"-steampid {steam.Id} -pid {game.Id} -manuallyclearframes 0";
                overlay = Process.Start( overlayInfo );
                Debug.WriteLine( $"STEAM = {steam.Id} GAME = {game.Id} OVERLAY = {overlay.Id}" );
                hooked = true;
            }

            game.WaitForExit();
            overlay.WaitForExit();
            Debug.WriteLine( "Goodbye." );
        }
    }
}
