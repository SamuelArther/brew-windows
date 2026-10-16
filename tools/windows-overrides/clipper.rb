class Clipper < Formula
  desc "Share the Windows clipboard with tmux and other local and remote apps"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "clipper", "clipper"
  end

  test do
    assert_match "clipper", shell_output("#{bin}/clipper -v")
    assert_match "--port", shell_output("#{bin}/clipper --help")
    assert_match "flag provided but not defined", shell_output("#{bin}/clipper --bogus 2>&1", 2)
  end
end
