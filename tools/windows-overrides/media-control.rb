class MediaControl < Formula
  desc "Control and observe media playback from the command-line"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "media-control", "media-control"
  end

  test do
    assert_match(/\d+\.\d+/, shell_output("#{bin}/media-control version"))
    # Nothing plays on CI
    assert_equal "null", shell_output("#{bin}/media-control get").strip
  end
end
