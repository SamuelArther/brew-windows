class WatchSim < Formula
  desc "Command-line Wear OS emulator app launcher"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "watch-sim", "watch-sim"
  end

  test do
    assert_match(/Usage: watch-sim/, shell_output("#{bin}/watch-sim 2>&1", 1))
    assert_match "Wear OS", shell_output("#{bin}/watch-sim --help")
    (testpath/"notes.txt").write "not an app"
    assert_match "not an APK", shell_output("#{bin}/watch-sim --info notes.txt 2>&1", 1)
    assert_match "No such file", shell_output("#{bin}/watch-sim missing.apk 2>&1", 1)
  end
end
