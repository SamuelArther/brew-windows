class SwitchaudioOsx < Formula
  desc "Change the default audio device from the command-line"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
