class SwitchaudioOsx < Formula
  desc "Change the default audio device from the command-line"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

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
