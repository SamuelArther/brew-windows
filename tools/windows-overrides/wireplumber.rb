class Wireplumber < Formula
  desc "Audio session control for Windows Core Audio (wpctl)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "wpctl", "wpctl"
    windows_exe "libexec/wintools.exe", "wireplumber", "wireplumber"
  end

  def caveats
    <<~EOS
      wpctl works on the Windows audio endpoints: status lists adapters, sinks and sources with
      their ids, and get-volume/set-volume/set-mute/set-default act on an id or on
      @DEFAULT_AUDIO_SINK@ / @DEFAULT_AUDIO_SOURCE@. The session manager itself is the Windows
      Audio Endpoint Builder service; there are no Lua scripts (wpexec).
    EOS
  end

  test do
    assert_match "Compiled with libwireplumber", shell_output("#{bin}/wireplumber --version")
    status = shell_output("#{bin}/wpctl status")
    assert_match "Audio", status
    assert_match "Sinks:", status
    assert_match "set-volume ID VOL", shell_output("#{bin}/wpctl --help")
    assert_match "not found", shell_output("#{bin}/wpctl get-volume 99999 2>&1", 1)
    shell_output("#{bin}/wpctl bogus 2>&1", 2)
  end
end
