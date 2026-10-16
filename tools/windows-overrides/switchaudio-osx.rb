class SwitchaudioOsx < Formula
  desc "Change the default audio device from the command-line"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "SwitchAudioSource", "SwitchAudioSource"
  end

  test do
    assert_match "device_name", shell_output("#{bin}/SwitchAudioSource -h")
    assert_match "invalid device type", shell_output("#{bin}/SwitchAudioSource -t bogus -a 2>&1", 2)
  end
end
