using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using Microsoft.Win32;

namespace SteamDetour
{
    internal static class SteamDetour
    {
        static Process game;
        static Process overlay;

        static void Error( string caption, params string[] message )
        {
            MessageBox.Show( string.Join( "\n", message ), $"SteamDetour : {caption ?? "Error"}", MessageBoxButtons.OK, MessageBoxIcon.Error );
            Environment.Exit( 1 );
        }

        [STAThread]
        static void Main( string[] args )
        {
            // Validate arguments
            if( args.Length != 2 )
                Error( "Usage", "SteamDetour.exe [exe path] %command%" );
            else if( !File.Exists( args[0] ) )
                Error( "File not found", args[0] );
            else if( !Path.IsPathRooted( args[0] ) )
                Error( "Invalid game path", "Full path to game executable required" );

            string gameExe = args[0];

            RegistryKey steamRegistry = Registry.LocalMachine.OpenSubKey( "SOFTWARE\\Wow6432Node\\Valve\\Steam" );
            if( steamRegistry == null )
                Error( "ERROR", "Cannot find Steam install" );
            string overlayExe = Path.Combine( (string)steamRegistry.GetValue( "InstallPath" ), "GameOverlayUI.exe" );
            if( !File.Exists( overlayExe ) )
                Error( "File not found", overlayExe );

            // Run the game
            Debug.WriteLine( $"Detour {args[1]} -> {gameExe}" );
            ProcessStartInfo gameInfo = new ProcessStartInfo();
            gameInfo.WorkingDirectory = Path.GetDirectoryName( gameExe );
            gameInfo.FileName = gameExe;
            game = Process.Start( gameInfo );

            // ...wait for it...
            game.WaitForExit( 1000 );

            bool hooked = false;
            while( !hooked )
            {
                // Run the overlay
                // based on https://github.com/SuiMachine/Steam-Overlay-Hooking-Helper
                ProcessStartInfo overlayInfo = new ProcessStartInfo();
                overlayInfo.WorkingDirectory = Path.GetDirectoryName( overlayExe );
                overlayInfo.FileName = overlayExe;
                overlayInfo.Arguments = $"-pid {game.Id} -manuallyclearframes 0";
                overlay = Process.Start( overlayInfo );
                Debug.WriteLine( $"GAME = {game.Id} OVERLAY = {overlay.Id}" );
                hooked = true;
            }

            game.WaitForExit();
            overlay.WaitForExit();
            Debug.WriteLine( "Goodbye." );
        }
    }
}
