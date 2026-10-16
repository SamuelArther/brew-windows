class LmSensors < Formula
  desc "Tools for monitoring the temperatures and fans"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "sensors", "sensors"
    windows_exe "libexec/wintools.exe", "sensors-detect", "sensors-detect"
  end

  test do
    assert_match "Usage", shell_output("#{bin}/sensors --help")
    assert_match "sensors-detect version", shell_output("#{bin}/sensors-detect")
    assert_match "sensors version", shell_output("#{bin}/sensors -v")
  end
end
