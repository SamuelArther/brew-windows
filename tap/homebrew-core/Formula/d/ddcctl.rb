class Ddcctl < Formula
  desc "DDC monitor controls (brightness, contrast, volume, input) for the command-line"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "ddcctl", "ddcctl"
  end

  test do
    output = shell_output("#{bin}/ddcctl -d 100 -b 100", 1)
    assert_match(/found \d+ external display/, output)
    assert_match "brightness", shell_output("#{bin}/ddcctl -h")
  end
end
