class Mole < Formula
  desc "Deep clean and inspect disk usage, with a system dashboard"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "mole", "mole"
    windows_exe "libexec/wintools.exe", "mo", "mo"
  end

  test do
    assert_match(/\d+\.\d+/, shell_output("#{bin}/mole --version"))
    assert_match "Health", shell_output("#{bin}/mole status")
    assert_match "reclaimable", shell_output("#{bin}/mole clean --dry-run")
    (testpath/"proj/node_modules").mkpath
    assert_match "build artifact", shell_output("#{bin}/mo purge #{testpath} --dry-run")
  end
end
